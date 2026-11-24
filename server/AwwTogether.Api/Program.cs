using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using AwwTogether.Api.Data;
using AwwTogether.Api.Models;
using AwwTogether.Api.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Cloud Run tells the container which port to listen on via $PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<ContentStore>();

// Inquiry storage: SQLite locally (no cloud credentials needed), Firestore on Google Cloud Run
// (Cloud Run's disk is wiped on every restart, so a local file would lose inquiries).
var useFirestore = string.Equals(builder.Configuration["Storage:Provider"], "Firestore", StringComparison.OrdinalIgnoreCase);
if (useFirestore)
{
    // On Cloud Run the project and credentials come from the service account automatically.
    // Firestore:ProjectId only needs setting when running elsewhere.
    var projectId = builder.Configuration["Firestore:ProjectId"];
    builder.Services.AddSingleton(_ => string.IsNullOrEmpty(projectId) ? FirestoreDb.Create() : FirestoreDb.Create(projectId));
    builder.Services.AddSingleton<IInquiryStore, FirestoreInquiryStore>();
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=App_Data/awwtogether.db";
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));
    builder.Services.AddScoped<IInquiryStore, SqliteInquiryStore>();
}

// Email each new inquiry to the duo (Gmail SMTP; password from the Email__Password secret).
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IInquiryNotifier, SmtpInquiryNotifier>();

// Stop the contact form from being spammed: 5 submissions per IP per 10 minutes.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("inquiries", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) }));
});

// Most hosts (Azure, Render, Fly, nginx) sit behind a proxy; trust its X-Forwarded-* headers
// so rate limiting and HTTPS redirects see the real client.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Create the SQLite file and tables on first run.
if (!useFirestore)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
    if (Path.GetDirectoryName(dataSource) is { Length: > 0 } dir) Directory.CreateDirectory(dir);
    db.Database.EnsureCreated();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // HTTPS is terminated (and http→https redirected) by Cloud Run / the host in front of us.
    app.UseHsts();
}

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data: https:; media-src 'self' https:; " +
        "style-src 'self' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; " +
        "connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? "";
        // Vite's /assets files have content hashes in their names, so they can be cached for good.
        // Videos and images change rarely; cache them for a week. index.html is never cached so deploys show up at once.
        ctx.Context.Response.Headers.CacheControl =
            path.StartsWith("/assets/") ? "public, max-age=31536000, immutable"
            : path.StartsWith("/videos/") || path.StartsWith("/images/") ? "public, max-age=604800"
            : "no-cache";
    },
});
app.UseRateLimiter();

var api = app.MapGroup("/api");

api.MapGet("/content", (ContentStore store) => store.Current)
    .WithName("GetContent");

api.MapPost("/inquiries", async (InquiryRequest req, IInquiryStore store, IInquiryNotifier notifier, ILogger<Program> log) =>
{
    // Honeypot filled in → almost certainly a bot. Pretend it worked so it moves on.
    if (!string.IsNullOrWhiteSpace(req.Website))
        return Results.Accepted();

    var inquiry = new Inquiry
    {
        Name = req.Name.Trim(),
        Email = req.Email.Trim(),
        Brand = string.IsNullOrWhiteSpace(req.Brand) ? null : req.Brand.Trim(),
        Budget = string.IsNullOrWhiteSpace(req.Budget) ? null : req.Budget.Trim(),
        Message = req.Message.Trim(),
        CreatedAt = DateTimeOffset.UtcNow,
    };
    var id = await store.AddAsync(inquiry);
    log.LogInformation("New inquiry {Id} from {Brand}", id, inquiry.Brand ?? "(no brand)");

    // Saved first, so a mail problem never loses an inquiry. Sent before responding because
    // Cloud Run pauses the CPU once the response is out.
    try
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await notifier.NotifyAsync(inquiry, timeout.Token);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Inquiry {Id} saved, but the notification email failed", id);
    }
    return Results.Accepted();
})
.RequireRateLimiting("inquiries")
.WithName("CreateInquiry");

// Private: lets the duo read submissions. Send the key from Admin:ApiKey in an X-Admin-Key header.
// Disabled (404) until a key is configured.
api.MapGet("/inquiries", async (HttpRequest http, IInquiryStore store, IConfiguration config) =>
{
    var key = config["Admin:ApiKey"];
    if (string.IsNullOrEmpty(key)) return Results.NotFound();
    var given = http.Headers["X-Admin-Key"].ToString();
    if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(key)))
        return Results.Unauthorized();

    return Results.Ok(await store.ListRecentAsync(200));
})
.WithName("ListInquiries");

api.MapFallback(() => Results.NotFound());

app.MapHealthChecks("/health");

// Everything else is the React app (built into wwwroot by `npm run build`).
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;

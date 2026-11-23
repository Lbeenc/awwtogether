using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using AwwTogether.Api.Data;
using AwwTogether.Api.Models;
using AwwTogether.Api.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<ContentStore>();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=App_Data/awwtogether.db";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

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
using (var scope = app.Services.CreateScope())
{
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
    app.UseHsts();
    app.UseHttpsRedirection();
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
app.UseStaticFiles();
app.UseRateLimiter();

var api = app.MapGroup("/api");

api.MapGet("/content", (ContentStore store) => store.Current)
    .WithName("GetContent");

api.MapPost("/inquiries", async (InquiryRequest req, AppDbContext db, ILogger<Program> log) =>
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
    db.Inquiries.Add(inquiry);
    await db.SaveChangesAsync();
    log.LogInformation("New inquiry {Id} from {Brand}", inquiry.Id, inquiry.Brand ?? "(no brand)");
    return Results.Accepted();
})
.RequireRateLimiting("inquiries")
.WithName("CreateInquiry");

// Private: lets the duo read submissions. Send the key from Admin:ApiKey in an X-Admin-Key header.
// Disabled (404) until a key is configured.
api.MapGet("/inquiries", async (HttpRequest http, AppDbContext db, IConfiguration config) =>
{
    var key = config["Admin:ApiKey"];
    if (string.IsNullOrEmpty(key)) return Results.NotFound();
    var given = http.Headers["X-Admin-Key"].ToString();
    if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(key)))
        return Results.Unauthorized();

    var list = await db.Inquiries.OrderByDescending(i => i.CreatedAt).Take(200).ToListAsync();
    return Results.Ok(list.Select(InquiryResponse.From));
})
.WithName("ListInquiries");

api.MapFallback(() => Results.NotFound());

app.MapHealthChecks("/health");

// Everything else is the React app (built into wwwroot by `npm run build`).
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;

using System.Net;
using System.Net.Http.Json;
using AwwTogether.Api.Models;
using AwwTogether.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AwwTogether.Api.Tests;

/// <summary>Spins up the real API against a throwaway SQLite file.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"aww-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath};Pooling=False");
        builder.UseSetting("Admin:ApiKey", "test-key");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_dbPath); } catch (IOException) { }
    }
}

public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static object ValidInquiry(string? website = null) => new
    {
        name = "Jamie Brand",
        email = "jamie@example.com",
        brand = "Glow Co",
        budget = "$500 – $1,500",
        message = "We'd love two TikToks for our spring launch.",
        website,
    };

    [Fact]
    public async Task Content_is_served()
    {
        var json = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/content");
        Assert.Equal("AwwTogether", json.GetProperty("brandName").GetString());
        Assert.True(json.GetProperty("services").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Valid_inquiry_is_saved_and_listed_for_admin()
    {
        var post = await _client.PostAsJsonAsync("/api/inquiries", ValidInquiry());
        Assert.Equal(HttpStatusCode.Accepted, post.StatusCode);

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/inquiries");
        req.Headers.Add("X-Admin-Key", "test-key");
        var list = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("Glow Co", await list.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Invalid_inquiry_returns_field_errors()
    {
        var res = await _client.PostAsJsonAsync("/api/inquiries", new { name = "J", email = "not-an-email", message = "hi" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Email", body);
        Assert.Contains("Message", body);
    }

    [Fact]
    public async Task Inquiries_list_requires_admin_key()
    {
        var res = await _client.GetAsync("/api/inquiries");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Valid_inquiry_triggers_email_notification()
    {
        var fake = new FakeNotifier();
        using var client = factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IInquiryNotifier>(fake))).CreateClient();

        var res = await client.PostAsJsonAsync("/api/inquiries", ValidInquiry());

        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);
        var sent = Assert.Single(fake.Sent);
        Assert.Equal("jamie@example.com", sent.Email);
        Assert.Equal("Glow Co", sent.Brand);
    }

    [Fact]
    public async Task Inquiry_is_still_accepted_when_email_fails()
    {
        using var client = factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IInquiryNotifier>(new FakeNotifier { Fail = true }))).CreateClient();

        var res = await client.PostAsJsonAsync("/api/inquiries", ValidInquiry());

        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);
    }

    [Fact]
    public async Task Honeypot_submission_sends_no_email()
    {
        var fake = new FakeNotifier();
        using var client = factory.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IInquiryNotifier>(fake))).CreateClient();

        await client.PostAsJsonAsync("/api/inquiries", ValidInquiry(website: "http://spam.example"));

        Assert.Empty(fake.Sent);
    }

    private sealed class FakeNotifier : IInquiryNotifier
    {
        public bool Fail { get; init; }
        public List<Inquiry> Sent { get; } = [];

        public Task NotifyAsync(Inquiry inquiry, CancellationToken ct = default)
        {
            if (Fail) throw new InvalidOperationException("SMTP down");
            Sent.Add(inquiry);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Unknown_api_route_is_404_not_the_spa()
    {
        var res = await _client.GetAsync("/api/nope");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}

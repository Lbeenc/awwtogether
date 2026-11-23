using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

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
    public async Task Unknown_api_route_is_404_not_the_spa()
    {
        var res = await _client.GetAsync("/api/nope");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}

using System.ComponentModel.DataAnnotations;

namespace AwwTogether.Api.Models;

/// <summary>A brand inquiry submitted through the contact form, stored in SQLite.</summary>
public sealed class Inquiry
{
    public int Id { get; set; }
    [MaxLength(100)] public required string Name { get; set; }
    [MaxLength(254)] public required string Email { get; set; }
    [MaxLength(100)] public string? Brand { get; set; }
    [MaxLength(50)] public string? Budget { get; set; }
    [MaxLength(4000)] public required string Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>What the contact form posts. Validated automatically by the endpoint.</summary>
public sealed record InquiryRequest(
    [property: Required, StringLength(100, MinimumLength = 2)] string Name,
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: StringLength(100)] string? Brand,
    [property: StringLength(50)] string? Budget,
    [property: Required, StringLength(4000, MinimumLength = 10)] string Message,
    // Honeypot: hidden on the form, so real visitors leave it empty. Bots fill it.
    string? Website);

public sealed record InquiryResponse(string Id, string Name, string Email, string? Brand, string? Budget, string Message, DateTimeOffset CreatedAt)
{
    public static InquiryResponse From(string id, Inquiry i) => new(id, i.Name, i.Email, i.Brand, i.Budget, i.Message, i.CreatedAt);
}

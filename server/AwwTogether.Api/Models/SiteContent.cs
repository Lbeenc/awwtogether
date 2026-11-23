namespace AwwTogether.Api.Models;

/// <summary>All editable copy for the site. Loaded from Data/content.json.</summary>
public sealed record SiteContent(
    string BrandName,
    string Tagline,
    IReadOnlyList<Creator> Creators,
    string About,
    string WhatIsUgc,
    IReadOnlyList<Stat> Stats,
    IReadOnlyList<PortfolioItem> Photos,
    IReadOnlyList<PortfolioItem> Videos,
    IReadOnlyList<Testimonial> Testimonials,
    IReadOnlyList<Service> Services,
    ContactInfo Contact);

public sealed record Creator(string Name, string Specialty, string? PhotoUrl);

public sealed record Stat(string Value, string Label);

public sealed record PortfolioItem(string Title, string Brand, string Description, string? MediaUrl, string? Link);

public sealed record Testimonial(string Quote, string Name, string Role);

public sealed record Service(string Title, string Description, IReadOnlyList<PriceTier> Tiers);

public sealed record PriceTier(string Label, string Price);

public sealed record ContactInfo(string Email, string Website, string Instagram, string TikTok, string ResponseTime);

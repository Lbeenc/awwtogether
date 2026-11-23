using AwwTogether.Api.Data;
using AwwTogether.Api.Models;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace AwwTogether.Api.Services;

/// <summary>Where contact-form inquiries are kept. Chosen by the Storage:Provider setting.</summary>
public interface IInquiryStore
{
    Task<string> AddAsync(Inquiry inquiry, CancellationToken ct = default);
    Task<IReadOnlyList<InquiryResponse>> ListRecentAsync(int max, CancellationToken ct = default);
}

/// <summary>Local development and tests: a SQLite file via EF Core.</summary>
public sealed class SqliteInquiryStore(AppDbContext db) : IInquiryStore
{
    public async Task<string> AddAsync(Inquiry inquiry, CancellationToken ct = default)
    {
        db.Inquiries.Add(inquiry);
        await db.SaveChangesAsync(ct);
        return inquiry.Id.ToString();
    }

    public async Task<IReadOnlyList<InquiryResponse>> ListRecentAsync(int max, CancellationToken ct = default)
    {
        var rows = await db.Inquiries.OrderByDescending(i => i.CreatedAt).Take(max).ToListAsync(ct);
        return rows.Select(i => InquiryResponse.From(i.Id.ToString(), i)).ToList();
    }
}

/// <summary>Production on Google Cloud Run: the "inquiries" collection in Firestore.</summary>
public sealed class FirestoreInquiryStore(FirestoreDb db) : IInquiryStore
{
    private CollectionReference Collection => db.Collection("inquiries");

    public async Task<string> AddAsync(Inquiry inquiry, CancellationToken ct = default)
    {
        var doc = await Collection.AddAsync(new Dictionary<string, object?>
        {
            ["name"] = inquiry.Name,
            ["email"] = inquiry.Email,
            ["brand"] = inquiry.Brand,
            ["budget"] = inquiry.Budget,
            ["message"] = inquiry.Message,
            ["createdAt"] = Timestamp.FromDateTimeOffset(inquiry.CreatedAt),
        }, ct);
        return doc.Id;
    }

    public async Task<IReadOnlyList<InquiryResponse>> ListRecentAsync(int max, CancellationToken ct = default)
    {
        var snap = await Collection.OrderByDescending("createdAt").Limit(max).GetSnapshotAsync(ct);
        return snap.Documents.Select(d =>
        {
            var f = d.ToDictionary();
            string? Str(string k) => f.TryGetValue(k, out var v) ? v as string : null;
            return new InquiryResponse(
                d.Id,
                Str("name") ?? "",
                Str("email") ?? "",
                Str("brand"),
                Str("budget"),
                Str("message") ?? "",
                f.TryGetValue("createdAt", out var t) && t is Timestamp ts ? ts.ToDateTimeOffset() : DateTimeOffset.MinValue);
        }).ToList();
    }
}

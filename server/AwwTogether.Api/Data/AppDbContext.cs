using AwwTogether.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AwwTogether.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite can't sort DateTimeOffset natively; store it as unix ms.
        modelBuilder.Entity<Inquiry>()
            .Property(i => i.CreatedAt)
            .HasConversion(v => v.ToUnixTimeMilliseconds(), v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        modelBuilder.Entity<Inquiry>().HasIndex(i => i.CreatedAt);
    }
}

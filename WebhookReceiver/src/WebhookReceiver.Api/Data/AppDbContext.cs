using Microsoft.EntityFrameworkCore;
using WebhookReceiver.Api.Models;

namespace WebhookReceiver.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.Property(e => e.Status)
                .HasConversion<int>();
            entity.HasIndex(e => new { e.Status, e.ReceivedAt });
        });
    }
}

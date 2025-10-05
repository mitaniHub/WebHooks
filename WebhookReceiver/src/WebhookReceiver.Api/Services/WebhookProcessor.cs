using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebhookReceiver.Api.Data;
using WebhookReceiver.Api.Models;

namespace WebhookReceiver.Api.Services;

public class WebhookProcessor(IServiceProvider serviceProvider, ILogger<WebhookProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Simple polling loop; in production consider channels or queue triggers
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var handler = scope.ServiceProvider.GetRequiredService<IWebhookHandler>();

                // Fetch a small batch of pending events
                var pending = await db.WebhookEvents
                    .Where(e => e.Status == WebhookStatus.Pending)
                    .OrderBy(e => e.ReceivedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                if (pending.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                foreach (var evt in pending)
                {
                    evt.Status = WebhookStatus.Processing;
                }
                await db.SaveChangesAsync(stoppingToken);

                foreach (var evt in pending)
                {
                    try
                    {
                        await handler.HandleAsync(evt, stoppingToken);
                        evt.Status = WebhookStatus.Succeeded;
                        evt.ProcessedAt = DateTimeOffset.UtcNow;
                        evt.Error = null;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed processing webhook {Id}", evt.Id);
                        evt.Status = WebhookStatus.Failed;
                        evt.Error = ex.Message;
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in WebhookProcessor");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

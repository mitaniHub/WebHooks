using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebhookReceiver.Api.Models;

namespace WebhookReceiver.Api.Services;

public class LoggingWebhookHandler(ILogger<LoggingWebhookHandler> logger) : IWebhookHandler
{
    public Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var headers = JsonSerializer.Deserialize<Dictionary<string, string[]>>(webhookEvent.HeadersJson) ?? [];
        logger.LogInformation("Processing webhook {Id} type={Type} source={Source} bodyLength={Length}",
            webhookEvent.Id,
            webhookEvent.EventType,
            webhookEvent.Source,
            webhookEvent.Body?.Length ?? 0);
        foreach (var kvp in headers)
        {
            logger.LogDebug("Header {Key}={Value}", kvp.Key, string.Join(",", kvp.Value));
        }

        return Task.CompletedTask;
    }
}

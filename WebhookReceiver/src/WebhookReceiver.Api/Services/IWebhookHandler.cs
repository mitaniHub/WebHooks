using System.Threading.Tasks;
using WebhookReceiver.Api.Models;

namespace WebhookReceiver.Api.Services;

public interface IWebhookHandler
{
    Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
}

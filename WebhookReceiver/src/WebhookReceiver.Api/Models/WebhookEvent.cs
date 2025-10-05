using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebhookReceiver.Api.Models;

public enum WebhookStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3
}

public class WebhookEvent
{
    [Key]
    public long Id { get; set; }

    [MaxLength(200)]
    public string? EventType { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    [Required]
    public string HeadersJson { get; set; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    public WebhookStatus Status { get; set; } = WebhookStatus.Pending;

    [MaxLength(2000)]
    public string? Error { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
}

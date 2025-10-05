using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebhookReceiver.Api.Data;
using WebhookReceiver.Api.Models;
using WebhookReceiver.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure EF Core (SQLite)
var defaultDbPath = Path.Combine(builder.Environment.ContentRootPath, "app.db");
var connectionString = builder.Configuration.GetConnectionString("Default")
                      ?? $"Data Source={defaultDbPath}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Background processing
builder.Services.AddScoped<IWebhookHandler, LoggingWebhookHandler>();
builder.Services.AddHostedService<WebhookProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Receive webhooks
app.MapPost("/webhooks", async (HttpRequest request, AppDbContext db) =>
{
    request.EnableBuffering();
    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    request.Body.Position = 0;

    var headers = request.Headers.ToDictionary(
        h => h.Key,
        h => h.Value.ToArray(),
        StringComparer.OrdinalIgnoreCase);
    var headersJson = JsonSerializer.Serialize(headers);

    string? GetHeader(params string[] names)
    {
        foreach (var name in names)
        {
            if (headers.TryGetValue(name, out var values) && values.Length > 0)
            {
                return values[0];
            }
        }
        return null;
    }

    var evt = new WebhookEvent
    {
        Body = body,
        HeadersJson = headersJson,
        EventType = GetHeader("X-Event-Type", "X-GitHub-Event", "ce-type", "Stripe-Event-Type"),
        Source = GetHeader("ce-source", "User-Agent", "X-Source")
    };

    db.WebhookEvents.Add(evt);
    await db.SaveChangesAsync();

    return Results.Accepted($"/webhooks/{evt.Id}", new { evt.Id });
})
.WithName("ReceiveWebhook");

// Inspect stored webhook
app.MapGet("/webhooks/{id:long}", async (long id, AppDbContext db) =>
{
    var evt = await db.WebhookEvents.FindAsync(id);
    return evt is not null ? Results.Ok(evt) : Results.NotFound();
})
.WithName("GetWebhook");

app.Run();

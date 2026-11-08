using System.Text;
using System.Text.Json;
using InfraHostHealthcheck.Configuration;
using Microsoft.Extensions.Options;

namespace InfraHostHealthcheck.Features.Webhook;

public class DiscordClient
{
    private static readonly string ServiceName = typeof(DiscordClient).Assembly.GetName().Name ?? "";

    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private readonly ILogger<DiscordClient> _logger;

    public DiscordClient(
        HttpClient httpClient,
        IOptions<AppSettings> settings,
        ILogger<DiscordClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendNotification(string host, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.HealthCheckWebhook))
        {
            _logger.LogWarning("Discord webhook is not configured, skipping notification for {Host}", host);
            return;
        }

        var payload = new
        {
            content = $"[{ServiceName}] Health check failed for {host}"
        };

        string json = JsonSerializer.Serialize(payload);
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.PostAsync(_settings.HealthCheckWebhook, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Discord webhook returned {StatusCode}", response.StatusCode);
        }
    }
}

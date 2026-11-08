using InfraHostHealthcheck.Configuration;
using InfraHostHealthcheck.Features.Webhook;
using Microsoft.Extensions.Options;

namespace InfraHostHealthcheck.Features;

public class HealthCheckService : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly DiscordClient _discord;
    private readonly AppSettings _settings;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public HealthCheckService(
        HttpClient httpClient,
        DiscordClient discord,
        IOptions<AppSettings> settings,
        ILogger<HealthCheckService> logger,
        IHostApplicationLifetime lifetime)
    {
        _httpClient = httpClient;
        _discord = discord;
        _settings = settings.Value;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await WaitForApplicationStartedAsync(stoppingToken))
            return;

        _logger.LogInformation(
            "Health check started - polling {Count} endpoints every {Interval}m",
            _settings.HealthEndpoints.Length,
            _settings.RunEveryXMinutes);

        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.RunEveryXMinutes));
        do
        {
            await Task.WhenAll(_settings.HealthEndpoints.Select(endpoint => CheckEndpointAsync(endpoint, stoppingToken)));
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task<bool> WaitForApplicationStartedAsync(CancellationToken stoppingToken)
    {
        TaskCompletionSource started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration startedRegistration = _lifetime.ApplicationStarted.Register(() => started.TrySetResult());
        using CancellationTokenRegistration stoppingRegistration = stoppingToken.Register(() => started.TrySetCanceled(stoppingToken));

        try
        {
            await started.Task;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task CheckEndpointAsync(string endpoint, CancellationToken stoppingToken)
    {
        string? failure = null;
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(endpoint, stoppingToken);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                failure = $"{(int)response.StatusCode} {response.StatusCode}";
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            failure = exception.Message;
        }

        if (failure is null)
            return;

        _logger.LogWarning("Health check failed for {Endpoint}: {Failure}", endpoint, failure);

        try
        {
            await _discord.SendNotification(endpoint, stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Failed to send Discord notification for {Endpoint}", endpoint);
        }
    }
}

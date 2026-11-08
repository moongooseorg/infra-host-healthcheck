using InfraHostHealthcheck.Configuration;
using InfraHostHealthcheck.Features;
using InfraHostHealthcheck.Features.Webhook;
using OpenBaoHelper;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

try
{
    builder.AddOpenBaoConfiguration();
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Fatal: configuration could not be loaded from OpenBao. {exception}");
    return 1;
}

builder.Services.AddHttpClient<HealthCheckService>(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddHttpClient<DiscordClient>();

builder.Services.AddHostedService<HealthCheckService>();

WebApplication app = builder.Build();

app.MapGet("/health", () => Results.Text("up"));

app.Run();

return 0;

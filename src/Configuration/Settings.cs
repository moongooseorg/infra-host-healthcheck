using OpenBaoHelper;

namespace InfraHostHealthcheck.Configuration;

[OpenBaoSection("AppSettings")]
public class AppSettings
{
    public int RunEveryXMinutes { get; set; }
    public string[] HealthEndpoints { get; set; } = [];
    public string HealthCheckWebhook { get; set; } = "";
}

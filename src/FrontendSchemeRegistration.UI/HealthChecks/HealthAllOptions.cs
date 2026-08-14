namespace FrontendSchemeRegistration.UI.HealthChecks;

public class HealthAllOptions
{
    public const string ConfigSection = "Health:All";

    public string HeaderName { get; set; } = "X-Health-Check-Token";

    public string Token { get; set; } = string.Empty;

    public int DownstreamTimeoutSeconds { get; set; } = 10;

    public int MaximumResponseBodyBytes { get; set; } = 65_536;

    public int MaximumDeepHealthHops { get; set; } = 2;
}

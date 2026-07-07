using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.Options;

[ExcludeFromCodeCoverage]
public class RegistrationFeeSnapshotPollingOptions
{
    public const string ConfigSection = "RegistrationFeeSnapshotPolling";

    public int TimeoutSeconds { get; set; } = 60;

    public int IntervalSeconds { get; set; } = 3;
}

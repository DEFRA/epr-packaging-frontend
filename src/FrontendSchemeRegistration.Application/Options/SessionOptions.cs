namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SessionOptions
{
    public const string ConfigSection = "Session";

    public int IdleTimeoutMinutes { get; set; }
}

namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

public class SessionOptions
{
    public const string ConfigSection = "Session";

    public int IdleTimeoutMinutes { get; set; }
}

namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RedisOptions
{
    public const string ConfigSection = "Redis";

    public string ConnectionString { get; set; }

    public string InstanceName { get; set; }
}

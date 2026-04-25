namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

public class RedisOptions
{
    public const string ConfigSection = "Redis";

    public string ConnectionString { get; set; }

    public string InstanceName { get; set; }
}

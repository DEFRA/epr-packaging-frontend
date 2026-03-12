namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

[ExcludeFromCodeCoverage]
public class FibreOptions
{
    public const string ConfigSection = "Fibre";
    
    public string? LaunchDate { get; set; }

    public DateTime LaunchDateUtc =>
        DateTime.TryParse(LaunchDate, CultureInfo.InvariantCulture, out var result)
            ? DateTime.SpecifyKind(result, DateTimeKind.Utc)
            : DateTime.MaxValue;
}
namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CsocOptions
{
    public const string ConfigSection = "Csoc";
    
    public string? UnderstandingObligationsEndpoint { get; set; }
}
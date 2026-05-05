namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CsocOptions
{
    public const string ConfigSection = "Csoc";
    
    public string? WasteObligationsBaseAddress { get; set; }
}
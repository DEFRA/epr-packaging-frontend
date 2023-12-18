namespace FrontendSchemeRegistration.Application.Options;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FrontEndAccountCreationOptions
{
    public const string ConfigSection = "FrontEndAccountCreation";

    [Required]
    public string BaseUrl { get; set; }
}
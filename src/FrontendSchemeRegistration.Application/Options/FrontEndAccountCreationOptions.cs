namespace FrontendSchemeRegistration.Application.Options;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

public class FrontEndAccountCreationOptions
{
    public const string ConfigSection = "FrontEndAccountCreation";

    [Required]
    public string BaseUrl { get; set; }
}
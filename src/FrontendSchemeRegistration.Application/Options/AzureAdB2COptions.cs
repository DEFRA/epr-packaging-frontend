namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

public class AzureAdB2COptions
{
    public const string ConfigSection = "AzureAdB2C";

    public string SignedOutCallbackPath { get; set; }
}
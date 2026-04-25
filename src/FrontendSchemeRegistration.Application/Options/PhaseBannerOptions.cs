namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

public class PhaseBannerOptions
{
    public const string Section = "PhaseBanner";

    public string ApplicationStatus { get; set; } = string.Empty;

    public string SurveyUrl { get; set; } = string.Empty;

    public bool Enabled { get; set; }
}
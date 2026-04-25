namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

public class ValidationOptions
{
    public const string ConfigSection = "Validation";

    public int MaxIssuesToProcess { get; set; }

    public string MaxIssueReportSize { get; set; }

    public int ClosedLoopRegistrationFromYear { get; set; }
}
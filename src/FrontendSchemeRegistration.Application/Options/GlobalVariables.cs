namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;
using DTOs.Submission;

[ExcludeFromCodeCoverage]
public class GlobalVariables
{
    public int SchemeYear { get; set; }

    public string BasePath { get; set; }

    public int FileUploadLimitInBytes { get; set; }

    public List<SubmissionPeriod> SubmissionPeriods { get; set; }

    public bool UseLocalSession { get; set; }
}

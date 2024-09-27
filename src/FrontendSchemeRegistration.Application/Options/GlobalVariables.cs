namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;
using DTOs.Submission;

[ExcludeFromCodeCoverage]
public class GlobalVariables
{
    public string BasePath { get; set; }

    public int FileUploadLimitInBytes { get; set; }

    public List<SubmissionPeriod> SubmissionPeriods { get; set; }

    public bool UseLocalSession { get; set; }
    public int SubsidiaryFileUploadLimitInBytes { get; set; }
}

using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubsidiarySubmission : AbstractSubmission
{
    public override SubmissionType Type => SubmissionType.Subsidiary;

    public string SubsidiaryFileName { get; set; }

    public DateTime? SubsidiaryFileUploadDateTime { get; set; }

    public bool SubsidiaryDataComplete { get; set; }

    public int RecordsAdded { get; set; }
}

namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadCompanyDetailsViewModel : ViewModelWithOrganisationRole
{
    public DateTime SubmissionDeadline { get; set; }
    
    public bool IsResubmission { get; set; }

    public string PageHeading =>
        IsResubmission ? "upload_organisation_details" :
        IsComplianceScheme ? "report_your_member_organisation_detail" : "report_your_organisation_detail";
}
namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class FileUploadCompanyDetailsViewModel : ViewModelWithOrganisationRole
{
    public DateTime SubmissionDeadline { get; set; }
    
    public bool IsResubmission { get; set; }

    public string PageHeading
    {
        get
        {
            if (IsResubmission)
            {
                return "upload_organisation_details";
            }
            else if (IsComplianceScheme)
            {
                return "report_your_member_organisation_detail";
            }
            else
            {
                return "report_your_organisation_detail";
            }
        }
    }
}
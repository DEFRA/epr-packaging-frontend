namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.Enums;

[ExcludeFromCodeCoverage]
public class FileUploadCompanyDetailsViewModel : ViewModelWithOrganisationRole
{
    public DateTime SubmissionDeadline { get; set; }
    public bool IsResubmission { get; set; }
    public int? RegistrationYear { get; set; }
    public RegistrationJourney? RegistrationJounrey { get; set; }
    public string PageHeadingCso = "register_company_size";
    public string OrganisationName { get; set; }

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
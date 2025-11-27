namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Sessions;

[ExcludeFromCodeCoverage]
public class FileUploadCompanyDetailsViewModel : ViewModelWithOrganisationRole
{
    public DateTime SubmissionDeadline { get; set; }
    public bool IsResubmission { get; set; }
    public int? RegistrationYear { get; set; }
    public ProducerSize? ProducerSize { get; set; }
    public string PageHeadingCso = "register_company_size";
    public string OrganisationName { get; set; }
    public string PageHeading = "report_your_organisation_detail";
}
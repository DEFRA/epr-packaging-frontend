namespace FrontendSchemeRegistration.UI.ViewModels;

using Newtonsoft.Json;

public class FileUploadCompanyDetailsSuccessViewModel : ViewModelWithOrganisationRole
{
    public Guid SubmissionId { get; set; }

    [JsonProperty(PropertyName = "CompanyDetailsFileName")]
    public string FileName { get; set; }

    public bool RequiresBrandsFile { get; set; }

    public bool RequiresPartnershipsFile { get; set; }

    public DateTime SubmissionDeadline { get; set; }

    public bool IsApprovedUser { get; set; }

    public int? OrganisationMemberCount { get; set; }
}
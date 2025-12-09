namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;
using Application.Enums;
using Newtonsoft.Json;

[ExcludeFromCodeCoverage]
public class FileUploadCompanyDetailsSuccessViewModel : ViewModelWithOrganisationRole
{
    public Guid SubmissionId { get; set; }

    [JsonProperty(PropertyName = "CompanyDetailsFileName")]
    public string FileName { get; set; }

    public bool RequiresBrandsFile { get; set; }

    public bool RequiresPartnershipsFile { get; set; }

    public DateTime SubmissionDeadline { get; set; }

    public bool IsApprovedUser { get; set; }

    public bool IsCso { get; set; }
    
    public bool IsResubmission { get; set; }

    public int? RegistrationYear { get; set; }
    
    public RegistrationJourney? RegistrationJourney { get; set; }
    
    public string OrganisationName { get; set; }
}
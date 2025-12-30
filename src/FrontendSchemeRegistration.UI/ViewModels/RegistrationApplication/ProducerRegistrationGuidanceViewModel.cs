using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

[ExcludeFromCodeCoverage]
public class ProducerRegistrationGuidanceViewModel : OrganisationNationViewModel
{
    public bool IsComplianceScheme { get; set; } = false;

    public string ComplianceScheme { get; set; }

    public string OrganisationName { get; set; }

    public string OrganisationNumber { get; set; }
    
    public int RegistrationYear { get; set; }
    
    public RegistrationJourney? RegistrationJourney { get; set; }

    public bool ShowRegistrationCaption { get; set; } = false;
}
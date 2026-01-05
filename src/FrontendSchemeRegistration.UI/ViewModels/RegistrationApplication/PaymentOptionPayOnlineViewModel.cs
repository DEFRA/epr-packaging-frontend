using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

using Application.Enums;

[ExcludeFromCodeCoverage]
public class PaymentOptionPayOnlineViewModel
{
    public int TotalAmountOutstanding { get; set; }
    public string TotalAmount => (TotalAmountOutstanding / 100).ToString("#,##0.00");

    public string ApplicationReferenceNumber { get; set; }

    public string PaymentLink { get; set; }
    
    public bool IsComplianceScheme { get; set; }

    public int RegistrationYear { get; set; }
    public RegistrationJourney? RegistrationJourney { get; set; }
    public bool ShowRegistrationCaption => RegistrationJourney != null;
}
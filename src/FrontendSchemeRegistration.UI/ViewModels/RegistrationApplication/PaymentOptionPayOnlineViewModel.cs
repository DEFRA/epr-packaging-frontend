using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

[ExcludeFromCodeCoverage]
public class PaymentOptionPayOnlineViewModel
{
    public int TotalAmountOutstanding { get; set; }

    public string ApplicationReferenceNumber { get; set; }

    public string PaymentLink { get; set; }
    
    public bool IsComplianceScheme { get; set; }

    public int RegistrationYear { get; set; }

}
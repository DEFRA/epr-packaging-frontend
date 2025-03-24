using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

[ExcludeFromCodeCoverage]
public class PaymentOptionPayByPhoneViewModel
{
    public int TotalAmountOutstanding { get; set; }

    public string ApplicationReferenceNumber { get; set; }
    
    public bool IsComplianceScheme { get; set; }
}
using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

[ExcludeFromCodeCoverage]
public class PaymentOptionPayByPhoneViewModel
{
    public int TotalAmountOutstanding { get; set; }

    public string TotalAmount => (TotalAmountOutstanding / 100).ToString("#,##0.00");

    public string ApplicationReferenceNumber { get; set; }
    
    public bool IsComplianceScheme { get; set; }

    public int RegistrationYear { get; set; }

    public RegistrationJourney? RegistrationJourney { get; set; }
}
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class PaymentOptionPayOnlineViewModel
{
    public int TotalAmountOutstanding { get; set; }

    public string ApplicationReferenceNumber { get; set; }

    public string PaymentLink { get; set; }
    
    public bool IsComplianceScheme { get; set; }
}
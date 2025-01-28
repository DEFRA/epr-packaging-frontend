using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class PaymentOptionPayByPhoneViewModel
{
    public int TotalAmountOutstanding { get; set; }

    public string ApplicationReferenceNumber { get; set; }
}
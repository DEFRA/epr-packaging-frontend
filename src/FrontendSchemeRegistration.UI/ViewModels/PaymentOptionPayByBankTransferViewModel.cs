using FrontendSchemeRegistration.Application.Enums;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class PaymentOptionPayByBankTransferViewModel : OrganisationNationViewModel
    {
        public bool IsComplianceScheme { get; set; } = false;

        public string ApplicationReferenceNumber { get; set; }

        public int TotalAmountOutstanding { get; set; }

        public bool IsEngland => NationName == Nation.England.ToString();

        public bool IsNorthernIreland => NationName == Nation.NorthernIreland.ToString();
    }
}

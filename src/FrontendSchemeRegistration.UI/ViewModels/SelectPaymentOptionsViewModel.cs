using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.UI.Resources;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class SelectPaymentOptionsViewModel
    {
        [Required(ErrorMessageResourceName = "please_select_payment_method", ErrorMessageResourceType = typeof(ErrorMessages))]
        public int? PaymentOption { get; set; }

        public string RegulatorNation { get; set; } = string.Empty;

        public int TotalAmountOutstanding { get; set; }

        // full nation name
        public string NationName => NationExtensions.GetNationName(this.RegulatorNation);

        public bool IsEngland => NationName == Nation.England.ToString();
    }
}

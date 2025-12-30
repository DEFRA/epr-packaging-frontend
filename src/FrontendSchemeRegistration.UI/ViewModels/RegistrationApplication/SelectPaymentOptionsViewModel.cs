using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Extensions;
using FrontendSchemeRegistration.UI.Resources;

namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication
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

        public int RegistrationYear { get; set; }

        public bool ShowRegistrationCaption { get; set; } = false;

        public RegistrationJourney? RegistrationJourney { get; set; }

        public bool IsComplianceScheme { get; set; }

        public string OrganisationName { get; set; } = string.Empty;
    }
}

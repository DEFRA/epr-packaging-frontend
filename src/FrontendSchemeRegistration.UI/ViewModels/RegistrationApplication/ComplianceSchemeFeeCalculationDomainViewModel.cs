namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication
{
    public class ComplianceSchemeFeeCalculationDomainViewModel : ComplianceSchemeFeeCalculationBreakdownViewModel
    {
        public bool ShowPaymentButton => !RegistrationApplicationSubmitted && TotalAmountOutstanding > 0;
    }
}

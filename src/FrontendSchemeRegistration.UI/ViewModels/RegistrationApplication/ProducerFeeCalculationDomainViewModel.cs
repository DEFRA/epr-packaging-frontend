namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

public class ProducerFeeCalculationDomainViewModel : FeeCalculationBreakdownViewModel
{
    public bool ShowPaymentButton => !RegistrationApplicationSubmitted && TotalAmountOutstanding > 0;
}

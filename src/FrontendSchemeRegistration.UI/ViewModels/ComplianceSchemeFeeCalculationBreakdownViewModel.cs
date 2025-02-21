namespace FrontendSchemeRegistration.UI.ViewModels
{
    public class ComplianceSchemeFeeCalculationBreakdownViewModel
    {
        public int TotalAmountOutstanding { get; set; }
        public bool IsRegistrationFeePaid { get; set; }
        public int TotalPreviousPayments { get; set; }
        public int TotalFeeAmount { get; set; }
        public int RegistrationFee { get; set; }
        public int OnlineMarketplaceCount { get; set; }
        public int SmallProducersFee { get; set; }
        public int SmallProducersCount { get; set; }
        public int LargeProducersFee { get; set; }
        public int LargeProducersCount { get; set; }

        public int OnlineMarketplaceFee { get; set; }
        public int SubsidiaryCompanyFee { get; set; }
        public int SubsidiaryCompanyCount { get; set; }

        public int LateProducerFee { get; set; }
        public int LateProducersCount { get; set; }
        public bool RegistrationApplicationSubmitted { get; set; }
    }
}

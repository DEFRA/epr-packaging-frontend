namespace FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication
{
    using Application.Enums;

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
        public int ClosedLoopRecyclingFee { get; set; }
        public int ClosedLoopRecyclingCount { get; set; }
        public int SubsidiaryCompanyFee { get; set; } // band-only total (sum of FeeBreakdowns[*].TotalPrice)
        public int SubsidiaryCompanyCount { get; set; }
        public int TotalSubsidiaryOnlineMarketplaceFee { get; set; }
        public int NumberOfSubsidiariesBeingOnlineMarketplace { get; set; }
        public int TotalSubsidiaryClosedLoopRecyclingFee { get; set; }
        public int NumberOfSubsidiariesBeingClosedLoopRecycling { get; set; }
        public int TotalSubsidiaryLateFee { get; set; }
        public int CountOfLateSubsidiaries { get; set; }

        public int LateProducerFee { get; set; }
        public int LateProducersCount { get; set; }
        public bool RegistrationApplicationSubmitted { get; set; }

        public int RegistrationYear { get; set; }
        public RegistrationJourney? RegistrationJourney { get; set; }
        public bool ShowRegistrationCaption => RegistrationJourney != null;
    }
}

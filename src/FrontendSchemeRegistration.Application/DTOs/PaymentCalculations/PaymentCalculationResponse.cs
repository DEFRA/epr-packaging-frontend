using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.PaymentCalculations
{
    using System.Text.Json.Serialization;

    [ExcludeFromCodeCoverage]
    public class PaymentCalculationResponse
    {
        public int ProducerRegistrationFee { get; set; }
        public int ProducerOnlineMarketPlaceFee { get; set; }
        public int ProducerLateRegistrationFee { get; set; }
        public int SubsidiariesFee { get; set; }
        public int TotalFee { get; set; }
        public int PreviousPayment { get; set; }
        public int OutstandingPayment { get; set; }
        public int SubsidiariesLateRegistrationFee { get; set; }
        public SubsidiariesFeeBreakdown SubsidiariesFeeBreakdown { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class SubsidiariesFeeBreakdown
    {
        [JsonPropertyName("totalSubsidiariesOMPFees")]
        public int TotalSubsidiariesOnlineMarketplaceFee { get; set; }

        [JsonPropertyName("countOfOMPSubsidiaries")]
        public int CountOfOnlineMarketplaceSubsidiaries { get; set; }

        [JsonPropertyName("unitOMPFees")]
        public int UnitOnlineMarketplaceFee { get; set; }

        public FeeBreakdown[] FeeBreakdowns { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class FeeBreakdown
    {
        public int BandNumber { get; set; }
        public int UnitCount { get; set; }
        public int UnitPrice { get; set; }
        public int TotalPrice { get; set; }
    }
}

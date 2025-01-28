using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.PaymentCalculations
{
    [ExcludeFromCodeCoverage]
    public class PaymentCalculationRequest
    {
        public string ProducerType { get; set; }

        public int NumberOfSubsidiaries { get; set; }

        public string Regulator { get; set; }

        public int NoOfSubsidiariesOnlineMarketplace { get; set; }

        public bool IsProducerOnlineMarketplace { get; set; }

        public bool IsLateFeeApplicable { get; set; }

        public string ApplicationReferenceNumber { get; set; }

        public DateTime SubmissionDate { get; set; }
    }
}

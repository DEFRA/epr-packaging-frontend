using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.PaymentCalculations
{
    [ExcludeFromCodeCoverage]
    public class ProducerPaymentCalculationV2Request : PaymentCalculationRequest
    {
        public Guid FileId { get; set; }
        public Guid ExternalId { get; set; }
        public DateTimeOffset InvoicePeriod { get; set; }
        public int PayerTypeId { get; set; }
        public int PayerId { get; set; }
    }
}

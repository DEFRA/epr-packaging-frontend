
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels
{
    public class PackagingDataResubmissionFeePaymentEvent   {

        public Guid? FileId { get; set; }

        public string? ReferenceNumber { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public string PaidAmount { get; set; }
    }
}

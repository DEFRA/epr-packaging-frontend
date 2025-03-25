using System.Text.Json.Serialization;

namespace FrontendSchemeRegistration.Application.DTOs.PaymentCalculations
{
	public class PackagingPaymentResponse
	{
		[JsonPropertyName("previousPayments")]
		public decimal PreviousPaymentsReceived { get; set; }

		[JsonPropertyName("totalResubmissionFee")]
		public decimal ResubmissionFee { get; set; }

		[JsonPropertyName("outstandingPayment")]
		public decimal TotalOutstanding { get; set; }

		[JsonPropertyName("memberCount")]
		public int MemberCount { get; set; }
	}
}
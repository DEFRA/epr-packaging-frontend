namespace FrontendSchemeRegistration.Application.DTOs.PaymentCalculations
{
	public class PackagingPaymentRequest
	{
		public string ReferenceNumber { get; set; }

		public string Regulator { get; set; }

		public DateTime? ResubmissionDate { get; set; }

		public int MemberCount { get; set; }
	}
}
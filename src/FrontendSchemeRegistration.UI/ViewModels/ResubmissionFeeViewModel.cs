namespace FrontendSchemeRegistration.UI.ViewModels
{
    public class ResubmissionFeeViewModel
    {
        public bool IsComplianceScheme { get; set; }
        public int MemberCount { get; set; }
        public decimal ResubmissionFee { get; set; } = 0;
        public decimal TotalChargeableItems { get; set; } = 0;
        public decimal PreviousPaymentsReceived { get; set; } = 0;
        public decimal TotalOutstanding { get; set; } = 0;
    }
}

namespace FrontendSchemeRegistration.UI.Sessions
{
    public class FeeBreakdownDetails
    {
        public decimal TotalAmountOutstanding { get; set; }

        public bool IsLateFeeApplicable { get; set; }

        public decimal ResubmissionFee { get; set; }

        public int MemberCount { get; set; }

        public decimal PreviousPaymentsReceived { get; set; }        
    }
}
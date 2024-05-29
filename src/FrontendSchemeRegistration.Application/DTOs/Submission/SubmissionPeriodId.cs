using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission
{
    [ExcludeFromCodeCoverage]
    public class SubmissionPeriodId
    {
        public Guid SubmissionId { get; set; }

        public string SubmissionPeriod { get; set; }

        public string DatePeriodStartMonth { get; set; }

        public string DatePeriodEndMonth { get; set; }

        public int Year { get; set; }
    }
}

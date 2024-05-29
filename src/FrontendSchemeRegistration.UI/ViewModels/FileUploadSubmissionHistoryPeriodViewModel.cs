using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.Application.DTOs.Submission;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class FileUploadSubmissionHistoryPeriodViewModel
    {
        public string SubmissionPeriod { get; set; }

        public string DatePeriodStartMonth { get; set; }

        public string DatePeriodEndMonth { get; set; }

        public string DatePeriodYear { get; set; }

        public List<SubmissionHistory> SubmissionHistory { get; set; }
    }
}

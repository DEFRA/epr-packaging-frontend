using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    public class FileUploadCompanyDetailsSubmissionHistoryViewModel
    {
        public bool PreviousSubmissionHistoryExists { get; set; }

        public List<FileUploadCompanyDetailsSubmissionHistoryPeriodViewModel> SubmissionPeriods { get; set; }
    }
}

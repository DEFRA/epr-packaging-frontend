using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class FileUploadSubmissionHistoryViewModel
    {
        public bool PreviousSubmissionHistoryExists { get; set; }

        public List<FileUploadSubmissionHistoryPeriodViewModel> SubmissionPeriods { get; set; }
    }
}

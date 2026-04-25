using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
        public class FileUploadHistoryPackagingDataFilesViewModel
    {
        public int Year { get; set; }

        public List<FileUploadSubmissionHistoryPeriodViewModel> SubmissionPeriods { get; set; }
    }
}

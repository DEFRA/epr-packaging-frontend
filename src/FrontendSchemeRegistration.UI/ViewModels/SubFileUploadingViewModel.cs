using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class SubFileUploadingViewModel
    {
        public string? SubmissionId { get; set; }

        public bool IsFileUploadTakingLong { get; set; }
    }
}

using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class FileUploadHistoryPreviousSubmissionsViewModel
    {
        public List<int> Years { get; set; }

        public PagingDetail PagingDetail { get; set; } = new PagingDetail();
    }
}

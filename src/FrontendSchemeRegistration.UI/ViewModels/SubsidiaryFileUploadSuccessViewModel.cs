using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class SubsidiaryFileUploadSuccessViewModel
    {
        public int RecordsAdded { get; set; }

        public int TotalSubsidiariesCount { get; set; }

        public bool ShowTotalSubsidiariesCount { get; set; }
    }
}

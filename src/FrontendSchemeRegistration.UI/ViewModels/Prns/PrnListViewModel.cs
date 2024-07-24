using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    /// <summary>
    /// Collection of PRNs and PERNs, typically associated with a single
    /// Packaging Producer or Compliance Scheme.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PrnListViewModel
    {
        public List<PrnViewModel> Prns { get; set; }
    }
}
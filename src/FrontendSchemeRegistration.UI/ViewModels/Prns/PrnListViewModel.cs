namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    /// <summary>
    /// Collection of PRNs and PERNs, typically associated with a single
    /// Packaging Producer or Compliance Scheme.
    /// </summary>
    public class PrnListViewModel
    {
        public List<PrnViewModel> Prns { get; set; }
    }
}
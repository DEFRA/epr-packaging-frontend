using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    /// <summary>
    /// Subset of PrnViewModel. Restrict properties to searchable fields
    /// or those displayed in search results summary table
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PrnSearchResultViewModel : BasePrnViewModel
    {
        /// <summary>
        ///     The note's EPR Next Gen PRN page when ShowPrnsOnCdp is on; null keeps the packaging PRN page.
        /// </summary>
        public string? PrnHref { get; set; }
    }
}
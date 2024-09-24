namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
	using System.Diagnostics.CodeAnalysis;

	[ExcludeFromCodeCoverage]
    public class PrnSearchResultListViewModel
    {
        public string? SearchString { get; set; }

        public List<string> SearchValues() => TypeAhead;

        public List<PrnSearchResultViewModel> ActivePageOfResults { get; set; }

        public List<string> TypeAhead { get; set; } = [];
        
        public PagingDetail PagingDetail { get; set; } = new();      
    }
}
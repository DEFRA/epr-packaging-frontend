using FrontendSchemeRegistration.Application.DTOs;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    [ExcludeFromCodeCoverage]
    public class SearchPrnsViewModel
    {
        public int Page
        {
            get => _page < 1 ? 1 : _page;
            set => _page = value;
        }
        
        private int _page = 1;

        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }

        public string? FilterBy { get; set; }

        public string? SortBy { get; set; }

		public string? Source { get; set; }

		public static implicit operator PaginatedRequest(SearchPrnsViewModel viewModel)
		{
			return new PaginatedRequest
			{
				Page = viewModel.Page,
				PageSize = viewModel.PageSize,
				Search = viewModel.Search,
				FilterBy = viewModel.FilterBy,
				SortBy = viewModel.SortBy
			};
		}
	}
}

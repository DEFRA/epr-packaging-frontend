namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
	using Microsoft.AspNetCore.Mvc.Rendering;
	using System.Diagnostics.CodeAnalysis;

	[ExcludeFromCodeCoverage]
    public class PrnSearchResultListViewModel
    {
        public string? SearchString { get; set; }

        public List<string> SearchValues() => TypeAhead;

        public List<PrnSearchResultViewModel> ActivePageOfResults { get; set; }

        public List<string> TypeAhead { get; set; } = [];

        public PagingDetail PagingDetail { get; set; } = new();

        public string? SelectedFilter { get; set; }
        public string? SelectedSort { get; set; }
		public int ComplianceYear { get; set; }

        public SelectList FilterOptions
        {
	        get
	        {
		        var list = new List<SelectListItem>
		        {
			        new() { Text = "filter_all_statuses", Value = "" },
			        new() { Text = "filter_accepted", Value = "accepted-all" },
			        new() { Text = "filter_awaiting_acceptance", Value = "awaiting-all" },
			        new() { Text = "filter_cancelled", Value = "cancelled-all" },
			        new() { Text = "filter_rejected", Value = "rejected-all" },
		        };
				
		        var selectedValue = SelectedFilter ?? "awaiting_all";
		        
		        return new SelectList(list, "Value", "Text", selectedValue);
	        }
		}

		public SelectList SortOptions
        {
	        get
	        {
		        var list = new List<SelectListItem>
		        {
			        new() { Text = "sort_date_issued_desc", Value = "date-issued-desc" },
			        new() { Text = "sort_date_issued_asc", Value = "date-issued-asc" },					
			        new() { Text = "sort_december_waste", Value = "december-waste-desc" },
			        new() { Text = "sort_material_asc", Value = "material-asc" },
			        new() { Text = "sort_material_desc", Value = "material-desc" },
			        new() { Text = "sort_tonnage_desc", Value = "tonnage-desc" },
			        new() { Text = "sort_tonnage_asc", Value = "tonnage-asc" }
		        };
				
		        var selectedValue = SelectedSort ?? "date-issued-desc";
				
		        return new SelectList(list, "Value", "Text", selectedValue);
	        }
        }
	}
}
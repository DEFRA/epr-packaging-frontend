using FrontendSchemeRegistration.UI.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    [ExcludeFromCodeCoverage]
    public class AwaitingAcceptancePrnsViewModel
    {
        public List<AwaitingAcceptanceResultViewModel> Prns { get; set; }

        public int TotalAwaitingPrns { get; set; }
        public SelectList FilterOptions
        {
            get
            {
                // for Text, refer to Resources/Views/Prns/SelectMultiplePrns.en.resx and SelectMultiplePrns.cy.resx
                List<SelectListItem> list = new();
                list.Add(new SelectListItem { Text = "filter_all_materials", Value = PrnConstants.Filters.AwaitingAll });
                list.Add(new SelectListItem { Text = "filter_aluminium", Value = PrnConstants.Filters.AwaitingAluminium });
                list.Add(new SelectListItem { Text = "filter_glass_other", Value = PrnConstants.Filters.AwaitingGlassOther });
                list.Add(new SelectListItem { Text = "filter_glass_remelt", Value = PrnConstants.Filters.AwaitingGlassMelt });
                list.Add(new SelectListItem { Text = "filter_paper_board_fiber", Value = PrnConstants.Filters.AwaitngPaperFiber });
                list.Add(new SelectListItem { Text = "filter_plastic", Value = PrnConstants.Filters.AwaitngPlastic });
                list.Add(new SelectListItem { Text = "filter_steel", Value = PrnConstants.Filters.AwaitngSteel });
                list.Add(new SelectListItem { Text = "filter_wood", Value = PrnConstants.Filters.AwaitngWood });

                var selected = list.Find(x => x.Value == SelectedFilter)
                               ?? list.First(x => x.Value == PrnConstants.Filters.AwaitingAll);

				selected.Selected = true;
                return new SelectList(list, "Value", "Text", selected.Value);
            }
        }

        public SelectList SortOptions
        {
            get
            {
                // for Text, refer to Resources/Views/Prns/SelectMultiplePrns.en.resx and SelectMultiplePrns.cy.resx
                List<SelectListItem> list = new();
                list.Add(new SelectListItem { Text = "sort_date_issued_desc", Value = PrnConstants.Sorts.IssueDateDesc });
                list.Add(new SelectListItem { Text = "sort_date_issued_asc", Value = PrnConstants.Sorts.IssueDateAsc });
                list.Add(new SelectListItem { Text = "sort_december_waste", Value = PrnConstants.Sorts.DescemberWasteDesc });
                list.Add(new SelectListItem { Text = "issued_by_asc", Value = PrnConstants.Sorts.IssuedByAsc });
                list.Add(new SelectListItem { Text = "issued_by_desc", Value = PrnConstants.Sorts.IssuedByDesc });
                list.Add(new SelectListItem { Text = "sort_tonnage_desc", Value = PrnConstants.Sorts.TonnageDesc });
                list.Add(new SelectListItem { Text = "sort_tonnage_asc", Value = PrnConstants.Sorts.TonnageAsc });

                var selected = list.Find(x => x.Value == SelectedSort)
                               ?? list.First(x => x.Value == PrnConstants.Sorts.IssueDateDesc);

				selected.Selected = true;
                return new SelectList(list, "Value", "Text", selected.Value);
            }
        }
        public string SelectedFilter { get; set; }
        public string SelectedSort { get; set; }
        public PagingDetail PagingDetail { get; set; } = new();
    }
}

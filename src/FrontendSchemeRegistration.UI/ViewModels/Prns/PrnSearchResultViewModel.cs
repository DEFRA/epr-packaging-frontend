using FrontendSchemeRegistration.Application.DTOs.Prns;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    /// <summary>
    /// Subset of PrnViewModel. Restrict properties to searchable fields 
    /// or those displayed in search results summary table
    /// </summary>
    public class PrnSearchResultViewModel : BasePrnViewModel
    {
        public static implicit operator PrnSearchResultViewModel(PrnModel prn)
        {
            return new PrnSearchResultViewModel
            {
                ExternalId = prn.ExternalId,
                PrnOrPernNumber = prn.PrnNumber,
                Material = prn.MaterialName,
                DateIssued = prn.IssueDate,
                IsDecemberWaste = prn.DecemberWaste,
                IssuedBy = prn.IssuedByOrg,
                Tonnage = prn.TonnageValue,
                ApprovalStatus = MapStatus(prn.PrnStatus)
            };
        }

    }
}

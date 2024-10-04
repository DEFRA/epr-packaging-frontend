using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.UI.Constants;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    [ExcludeFromCodeCoverage]
    public class AwaitingAcceptanceResultViewModel : BasePrnViewModel
    {
        public string AdditionalNotes { get; set; }

        public bool IsSelected { get; set; }

        public string NoteType { get; set; }

        public static implicit operator AwaitingAcceptanceResultViewModel(PrnModel prn)
        {
            return new AwaitingAcceptanceResultViewModel
            {
                ExternalId = prn.ExternalId,
                PrnOrPernNumber = prn.PrnNumber,
                Material = prn.MaterialName,
                DateIssued = prn.IssueDate,
                IsDecemberWaste = prn.DecemberWaste,
                IssuedBy = prn.IssuedByOrg,
                Tonnage = prn.TonnageValue,
                AdditionalNotes = prn.IssuerNotes,
                NoteType = prn.IsExport ? PrnConstants.PernsText : PrnConstants.PrnText
            };
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.UI.Constants;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns
{
    /// <summary>
    /// Collection of PRNs and PERNs, typically associated with a single
    /// Packaging Producer or Compliance Scheme.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PrnListViewModel
    {
        public string FilterBy { get; set; }

        public string SortBy { get; set; }
        public RemovedPrn? RemovedPrn { get; set; }

        public List<PrnViewModel> Prns { get; set; } = new();

        public List<PrnViewModel> PreviousSelectedPrns { get; set; } = new();
        
        public CountBreakdown GetCountBreakdown(IEnumerable<PrnViewModel> prns)
        {
            var prnCount = prns?.Count(x => x.IsPrn) ?? 0;
            var pernCount = prns?.Count(x => !x.IsPrn) ?? 0;
            
            string columnHeaderLabel;
            if (prnCount > 0 && pernCount > 0)
                columnHeaderLabel = "prn_and_pern_number";
            else if (prnCount > 0)
                columnHeaderLabel = "prn_number";
            else
                columnHeaderLabel = "pern_number";

            string removeLinkText;
            if (prnCount > 0 && pernCount > 0)
                removeLinkText = "remove_prn_or_pern_from_selection";
            else if (prnCount > 0)
                removeLinkText = "remove_prn_from_selection";
            else
                removeLinkText = "remove_pern_from_selection";

            return new CountBreakdown(prns?.Count() ?? 0, prnCount, pernCount, columnHeaderLabel, removeLinkText);
        }

        public string GetPrnWord(int count)
        {
            return count == 1 ? PrnConstants.PrnText : PrnConstants.PrnsText;
        }   

        public string GetPernWord(int count)
        {
            return count == 1 ? PrnConstants.PernText : PrnConstants.PernsText;
        }

        // retuns nomenclature e.g. PRN, PRNs, PERN, PERNs, PRNs and PERNs
        public string GetNoteType(IEnumerable<PrnViewModel> prns)
        {
            var counts = GetCountBreakdown(prns);
            if (counts.PrnCount > 0 && counts.PernCount > 0)
            {
                return PrnConstants.PrnsAndPernsText;
            }
            if (counts.PrnCount > 0)
            {
                return GetPrnWord(counts.PrnCount);
            }
            if (counts.PernCount > 0)
            {
                return GetPernWord(counts.PernCount);
            }
            return string.Empty;
        }

        // PRNs, PERNs, PRNs and PERNs
        public string GetPluralNoteType(IEnumerable<PrnViewModel> prns)
        {
            var counts = GetCountBreakdown(prns);
            if (counts.PrnCount > 0 && counts.PernCount > 0)
            {
                return PrnConstants.PrnsAndPernsText;
            }
            if (counts.PrnCount > 0)
            {
                return PrnConstants.PrnsText;
            }
            if (counts.PernCount > 0)
            {
                return PrnConstants.PernsText;
            }
            
            return string.Empty;
        }
    }

    public record RemovedPrn(string PrnNumber, bool IsPrn);

    public record CountBreakdown(int TotalCount, int PrnCount, int PernCount, string ColumnHeaderLabel, string RemoveLinkText);
}
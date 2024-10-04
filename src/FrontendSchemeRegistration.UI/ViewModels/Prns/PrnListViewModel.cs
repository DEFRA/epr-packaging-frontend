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
            int prnCount = prns?.Count(x => x.IsPrn) ?? 0;
            int pernCount = prns?.Count(x => !x.IsPrn) ?? 0;

            return new CountBreakdown(prns?.Count() ?? 0, prnCount, pernCount);
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

            if ((counts.PrnCount > 0) && (counts.PernCount > 0))
            {
                return PrnConstants.PrnsAndPernsText;
            }
            else if (counts.PrnCount > 0)
            {
                return GetPrnWord(counts.PrnCount);
            }
            else if (counts.PernCount > 0)
            {
                return GetPernWord(counts.PernCount);
            }

            return string.Empty;
        }

        // PRNs, PERNs, PRNs and PERNs
        public string GetPluralNoteType(IEnumerable<PrnViewModel> prns)
        {
            var counts = GetCountBreakdown(prns);

            if ((counts.PrnCount > 0) && (counts.PernCount > 0))
            {
                return PrnConstants.PrnsAndPernsText;
            }
            else if (counts.PrnCount > 0)
            {
                return PrnConstants.PrnsText;
            }
            else if (counts.PernCount > 0)
            {
                return PrnConstants.PernsText;
            }

            return string.Empty;
        }
    }

    public record RemovedPrn(string PrnNumber, bool IsPrn);

    public record CountBreakdown(int TotalCount, int PrnCount, int PernCount);
}
﻿using System.Diagnostics.CodeAnalysis;
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
        public RemovedPrn? RemovedPrn { get; set; }

        public List<PrnViewModel> Prns { get; set; }

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

        // retuns nomenclature e.g. PRN, PRNs, PERN, PERNs, PERs and PERNs
        public string GetNoteType(IEnumerable<PrnViewModel> prns)
        {
            var counts = GetCountBreakdown(prns);

            if ((counts.PrnCount > 0) && (counts.PernCount > 0))
            {
                return PrnConstants.PrnsAndPernsText; // PERs and PERNs
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
    }

    public record RemovedPrn(string PrnNumber, bool IsPrn);

    public record CountBreakdown(int TotalCount, int PrnCount, int PernCount);
}
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.Constants
{
    [ExcludeFromCodeCoverage]
    public static class PrnConstants
    {
        public static string PrnText => "PRN";

        public static string PrnsText => "PRNs";

        public static string PernText => "PERN";

        public static string PernsText => "PERNs";

        public static string PrnsAndPernsText => "PRNs,PERNs";

        public static class Filters
        {
            public const string AcceptedAll = "accepted-all";
            public const string CancelledAll = "cancelled-all";
            public const string RejectedAll = "rejected-all";
            public const string AwaitingAll = "awaiting-all";
            public const string AwaitingAluminium = "awaiting-aluminium";
            public const string AwaitingGlassOther = "awaiting-glassother";
            public const string AwaitingGlassMelt = "awaiting-glassremelt";
            public const string AwaitngPaperFiber = "awaiting-paperfiber";
            public const string AwaitngPlastic = "awaiting-plastic";
            public const string AwaitngSteel = "awaiting-steel";
            public const string AwaitngWood = "awaiting-wood";
        }

        public static class Sorts
        {
            public const string IssueDateDesc = "date-issued-desc";
            public const string IssueDateAsc = "date-issued-asc";
            public const string TonnageDesc = "tonnage-desc";
            public const string TonnageAsc = "tonnage-asc";
            public const string IssuedByDesc = "issued-by-desc";
            public const string IssuedByAsc = "issued-by-asc";
            public const string DescemberWasteDesc = "december-waste-desc";
            public const string MaterialDesc = "material-desc";
            public const string MaterialAsc = "material-asc";
        }
    }
}

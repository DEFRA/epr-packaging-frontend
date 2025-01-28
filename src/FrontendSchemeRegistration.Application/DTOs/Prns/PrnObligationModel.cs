using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Prns
{
    [ExcludeFromCodeCoverage]
    public class PrnObligationModel
    {
        public int NumberOfPrnsAwaitingAcceptance { get; set; }

        public List<PrnMaterialObligationModel> ObligationData { get; set; }
    }
}

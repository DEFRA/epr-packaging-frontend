using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Prns
{
    [ExcludeFromCodeCoverage]
    public class PrnMaterialObligationModel
    {
        public Guid OrganisationId { get; set; }

        public string MaterialName { get; set; }

        public int? ObligationToMeet { get; set; }

        public int TonnageAwaitingAcceptance { get; set; }

        public int TonnageAccepted { get; set; }
        
        public int? TonnageOutstanding { get; set; }

        public string Status { get; set; }

        public double Tonnage { get; set; }

        public double MaterialTarget { get; set; }
    }
}

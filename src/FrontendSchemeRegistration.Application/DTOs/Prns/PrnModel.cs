using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Prns
{
    [ExcludeFromCodeCoverage]
    public class PrnModel
    {
        public int Id { get; set; }

        public Guid ExternalId { get; set; }

        public string PrnNumber { get; set; }

        public Guid OrganisationId { get; set; }

        public string OrganisationName { get; set; }

        public string ProducerAgency { get; set; }

        public string ReprocessorExporterAgency { get; set; }

        public string PrnStatus { get; set; }

        public int TonnageValue { get; set; }

        public string MaterialName { get; set; }

        public string? IssuerNotes { get; set; }

        public string IssuerReference { get; set; }

        public string? PrnSignatory { get; set; }

        public string? PrnSignatoryPosition { get; set; }

        public string? Signature { get; set; }

        public DateTime IssueDate { get; set; }

        public string? ProcessToBeUsed { get; set; }

        public bool DecemberWaste { get; set; }

        public DateTime? StatusUpdatedOn { get; set; }

        public string IssuedByOrg { get; set; }

        public string AccreditationNumber { get; set; }

        public string? ReprocessingSite { get; set; }

        public string AccreditationYear { get; set; }

        public string ObligationYear { get; set; }

        public string PackagingProducer { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public Guid LastUpdatedBy { get; set; }

        public DateTime LastUpdatedDate { get; set; }

        public bool IsExport { get; set; }
    }
}

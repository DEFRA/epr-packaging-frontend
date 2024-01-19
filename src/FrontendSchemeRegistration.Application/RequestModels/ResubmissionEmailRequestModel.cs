using System.ComponentModel.DataAnnotations;

namespace FrontendSchemeRegistration.Application.RequestModels
{
    public class ResubmissionEmailRequestModel
    {
        [Required]
        public int NationId { get; set; }

        public string ProducerOrganisationName { get; set; } = string.Empty;

        public string OrganisationNumber { get; set; } = string.Empty;

        public string SubmissionPeriod { get; set; } = string.Empty;

        [Required]
        public bool IsComplianceScheme { get; set; } = false;

        public string ComplianceSchemeName { get; set; } = string.Empty;

        public string ComplianceSchemePersonName { get; set; } = string.Empty;
    }
}

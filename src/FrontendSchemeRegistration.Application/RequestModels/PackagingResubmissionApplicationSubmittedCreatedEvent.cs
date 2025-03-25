using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels
{
    [ExcludeFromCodeCoverage]
    public class PackagingResubmissionApplicationSubmittedCreatedEvent  
    { 
        public Guid? FileId { get; set; }

        public bool? IsResubmitted { get; set; }

        public string? SubmittedBy { get; set; }

        public DateTime? SubmissionDate { get; set; }

        public string? Comments { get; set; }
    }
}

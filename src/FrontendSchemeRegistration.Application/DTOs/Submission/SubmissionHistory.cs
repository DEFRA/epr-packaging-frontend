using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission
{
    [ExcludeFromCodeCoverage]
    public class SubmissionHistory
    {
        public Guid SubmissionId { get; set; }

        public Guid FileId { get; set; }

        public string FileName { get; set; }

        public string UserName { get; set; }

        public DateTime SubmissionDate { get; set; }

        public string Status { get; set; }

        public DateTime DateofLatestStatusChange { get; set; }
    }
}

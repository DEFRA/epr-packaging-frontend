using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class PackagingResubmissionMemberDetails
{
    public int MemberCount { get; set; }

    public string ReferenceNumber { get; set; }
}

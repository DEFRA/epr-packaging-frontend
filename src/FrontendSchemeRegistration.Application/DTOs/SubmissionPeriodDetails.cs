using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class SubmissionPeriodDetails
{
    public int Id { get; set; }

    public string WindowType { get; set; } = string.Empty;

    public int RegistrationYear { get; set; }

    public DateTime OpeningDate { get; set; }

    public DateTime DeadlineDate { get; set; }

    public DateTime ClosingDate { get; set; }
}

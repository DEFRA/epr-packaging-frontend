namespace FrontendSchemeRegistration.Application.DTOs.Submission;

public class RegistrationSubmitContext
{
    public int? SubmissionPeriodId { get; init; }

    public string? RegulatorNation { get; init; }

    public bool NotifyPaymentService { get; init; } = true;
}
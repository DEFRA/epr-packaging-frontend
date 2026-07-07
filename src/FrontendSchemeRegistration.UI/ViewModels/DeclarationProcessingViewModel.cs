namespace FrontendSchemeRegistration.UI.ViewModels;

public class DeclarationProcessingViewModel
{
    public Guid SubmissionId { get; set; }

    public string StatusUrl { get; set; } = string.Empty;

    public string FallbackUrl { get; set; } = string.Empty;

    public int PollingIntervalMs { get; set; }

    public int PollingTimeoutMs { get; set; }
}

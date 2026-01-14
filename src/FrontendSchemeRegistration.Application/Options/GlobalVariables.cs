namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;
using DTOs.Submission;

[ExcludeFromCodeCoverage]
public class GlobalVariables
{
    public string BasePath { get; set; }

    public int FileUploadLimitInBytes { get; set; }

    /// <summary>
    /// When bound to appSettings, this represents the POM submission periods - NOT registration periods
    /// </summary>
    public List<SubmissionPeriod> SubmissionPeriods { get; set; }

    public bool UseLocalSession { get; set; }
    
    public int SubsidiaryFileUploadLimitInBytes { get; set; }

    public string LogPrefix { get; set; }
    
    public DateTime LateFeeDeadline2025 { get; set; }

    public DateTime LargeProducerLateFeeDeadline2026 { get; set; }

    public DateTime SmallProducerLateFeeDeadline2026 { get; set; }

    public DateTime SmallProducersRegStartTime2026 { get; set; }

    public string RegistrationYear { get; set; }

    /// <summary>
    /// Shows appropriate Recycling Obligation compliance year
    /// based on a given year, if not provided current year will be used
    /// </summary>
    public int? OverrideCurrentYear { get; set; }

    /// <summary>
    /// Shows appropriate Recycling Obligation values compliance year 
    /// based on a given month, if not provided current month will be used
    /// </summary>
    public int? OverrideCurrentMonth { get; set; }
    
    /// <summary>
    /// If set, this overrides the current system date/time. Should never be used
    /// in a production environment
    /// </summary>
    public DateTime? StartupUtcTimestampOverride { get; set; }
}

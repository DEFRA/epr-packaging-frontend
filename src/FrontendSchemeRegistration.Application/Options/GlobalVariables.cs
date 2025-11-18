namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;
using DTOs.Submission;

[ExcludeFromCodeCoverage]
public class GlobalVariables
{
    public string BasePath { get; set; }

    public int FileUploadLimitInBytes { get; set; }

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
    /// To Show appropriate Recycling Obligation values for the compliance year 
    /// </summary>
    public int? CurrentYear { get; set; }

    /// <summary>
    /// To Show appropriate Recycling Obligation values 
    /// for the compliance year based on a current month
    /// </summary>
    public int? CurrentMonth { get; set; }
}

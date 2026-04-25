using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

public class SynapseResponse
{
    public bool IsFileSynced { get; set; }
    
    public bool IsResubmissionDataSynced { get; set; }
}
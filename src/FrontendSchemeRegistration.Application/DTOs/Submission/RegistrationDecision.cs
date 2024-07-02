using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Submission;

[ExcludeFromCodeCoverage]
public class RegistrationDecision : AbstractDecision
{
    public string Comments { get; set; } = string.Empty;

    public string Decision { get; set; } = string.Empty;
}
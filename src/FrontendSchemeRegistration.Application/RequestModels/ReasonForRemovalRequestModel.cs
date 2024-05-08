using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.RequestModels;

[ExcludeFromCodeCoverage]
public class ReasonForRemovalRequestModel
{
    public string Code { get; set; }

    public string? TellUsMore { get; set; }
}
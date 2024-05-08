namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public record DatePeriod
{
    public string StartMonth { get; init; }

    public string EndMonth { get; init; }
}
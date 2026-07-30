namespace FrontendSchemeRegistration.UI.Mappers;

using Application.DTOs.Prns;
using Application.Extensions;
using AutoMapper;

/// <summary>
///     True when the current UK date is in December or January and this December-waste PRN
///     was issued in that same immediate Dec/Jan period (not a prior year's January).
/// </summary>
public class PrnDecemberWasteFlashWindowResolver(TimeProvider timeProvider)
    : IValueResolver<PrnModel, object, bool>
{
    public bool Resolve(PrnModel source, object _, bool __, ResolutionContext context)
    {
        if (!source.DecemberWaste)
            return false;

        return timeProvider.GetUtcNow().IsInImmediateDecemberJanuaryFlashWindow(source.IssueDate);
    }
}

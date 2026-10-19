namespace FrontendSchemeRegistration.UI.Mappers;

using Application.DTOs.Prns;
using Application.Extensions;
using AutoMapper;

/// <summary>
///     True when the current UK date is in the Dec/Jan period that starts in December of this
///     December-waste PRN's accreditation year (1 Dec Y to 31 Jan Y+1).
/// </summary>
public class PrnDecemberWasteFlashWindowResolver(TimeProvider timeProvider)
    : IValueResolver<PrnModel, object, bool>
{
    public bool Resolve(PrnModel source, object _, bool __, ResolutionContext context)
    {
        if (!source.DecemberWaste || !int.TryParse(source.AccreditationYear, out var accreditationYear))
            return false;

        return timeProvider.GetUtcNow().IsInDecemberJanuaryFlashWindow(accreditationYear);
    }
}

namespace FrontendSchemeRegistration.UI.Mappers;

using Application.Constants;
using Application.DTOs.Prns;
using Application.Extensions;
using AutoMapper;
using ViewModels.Prns;

public class IsPrnStatusEditableResolver(TimeProvider timeProvider)
    : IValueResolver<PrnModel, BasePrnViewModel, bool>
{
    /// <summary>
    ///     Resolves whether the PRN status is considered editable from the User's perspective.
    ///     This is based on its current status, as well as its relation to the current Compliance Year.
    /// </summary>
    public bool Resolve(PrnModel source, BasePrnViewModel _, bool __, ResolutionContext context)
    {
        var isAwaiting = MapStatus(source.PrnStatus) == PrnStatus.AwaitingAcceptance;
        var complianceYear = timeProvider.GetUtcNow().GetComplianceYear();
        var obligationYear = int.TryParse(source.ObligationYear, out var year) ? year : 0;

        return isAwaiting &&
               (obligationYear == complianceYear
                || (source.DecemberWaste && obligationYear == complianceYear - 1));
    }

    public static string MapStatus(string oldStatus)
    {
        return oldStatus switch
        {
            "AWAITINGACCEPTANCE" => PrnStatus.AwaitingAcceptance,
            "CANCELED" => PrnStatus.Cancelled,
            _ => oldStatus
        };
    }
}
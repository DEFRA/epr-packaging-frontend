namespace FrontendSchemeRegistration.UI.Extensions;

using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Models;

public static class UserDataExtensions
{
    public static bool CanSubmit(this UserData userData)
    {
        return userData is
        {
            ServiceRole: ServiceRoles.ApprovedPerson or ServiceRoles.DelegatedPerson,
            EnrolmentStatus: EnrolmentStatuses.Approved
        };
    }
}
namespace FrontendSchemeRegistration.UI.Services.StubAuthentication;

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

public interface ICustomClaims
{
    Task<IEnumerable<Claim>> GetCustomClaims(TokenValidatedContext context);
}

[ExcludeFromCodeCoverage]
public class CustomClaims(IUserAccountService userAccountService) : ICustomClaims
{
    public async Task<IEnumerable<Claim>> GetCustomClaims(TokenValidatedContext context)
    {
        //use this to get any extra claim data
        var userAccount = await userAccountService.GetUserAccount();


        var userData = new UserData
        {
            ServiceRoleId = userAccount.User.ServiceRoleId,
            ServiceRole = userAccount.User.ServiceRole,
            Service = userAccount.User.Service,
            FirstName = userAccount.User.FirstName,
            LastName = userAccount.User.LastName,
            Email = userAccount.User.Email,
            Id = userAccount.User.Id,
            EnrolmentStatus = userAccount.User.EnrolmentStatus,
            JobTitle = "Director",
            RoleInOrganisation = userAccount.User.RoleInOrganisation,
            Organisations = userAccount.User.Organisations.Select(x =>
                new Organisation
                {
                    Id = x.Id,
                    Name = x.OrganisationName,
                    OrganisationRole = x.OrganisationRole,
                    OrganisationType = x.OrganisationType,
                    OrganisationNumber = x.OrganisationNumber
                }).ToList()
        };
        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData)),
            new (ClaimConstants.ObjectId, userAccount.User.Id.ToString())
        };
    }
}
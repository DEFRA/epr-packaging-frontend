namespace FrontendSchemeRegistration.UI.Middleware;

using Application.Options;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Services.Interfaces;
using Extensions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using System.Security.Claims;

public class UserDataCheckerMiddleware : IMiddleware
{
    private const string OrganisationIdsExtensionClaimName = "OrgIds";

    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<UserDataCheckerMiddleware> _logger;
    private readonly FrontEndAccountCreationOptions _frontEndAccountCreationOptions;
    private readonly IFeatureManager _featureManager;
    private readonly IGraphService _graphService;

    public UserDataCheckerMiddleware(
        IOptions<FrontEndAccountCreationOptions> frontendAccountCreationOptions,
        IUserAccountService userAccountService,
        IFeatureManager featureManager,
        IGraphService graphService,
        ILogger<UserDataCheckerMiddleware> logger)
    {
        _frontEndAccountCreationOptions = frontendAccountCreationOptions.Value;
        _featureManager = featureManager;
        _userAccountService = userAccountService;
        _graphService = graphService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var anonControllers = new List<string> { "Privacy", "Cookies", "Culture", "Account" };
        var controllerName = GetControllerName(context);

        if (!anonControllers.Contains(controllerName) && context.User.Identity is { IsAuthenticated: true } && context.User.TryGetUserData() is null)
        {
            var userAccount = await _userAccountService.GetUserAccount();

            if (userAccount is null)
            {
                _logger.LogInformation("User authenticated but account could not be found");
                context.Response.Redirect(_frontEndAccountCreationOptions.BaseUrl);
                return;
            }

            var userData = new UserData
            {
                ServiceRoleId = userAccount.User.ServiceRoleId,
                FirstName = userAccount.User.FirstName,
                LastName = userAccount.User.LastName,
                Email = userAccount.User.Email,
                Id = userAccount.User.Id,
                Organisations = userAccount.User.Organisations.Select(x =>
                    new Organisation
                    {
                        Id = x.Id,
                        Name = x.OrganisationName,
                        OrganisationRole = x.OrganisationRole,
                        OrganisationType = x.OrganisationType
                    }).ToList()
            };

            await UpdateOrganisationIdsClaim(context.User, userAccount.User);

            await ClaimsExtensions.UpdateUserDataClaimsAndSignInAsync(context, userData);
        }

        await next(context);
    }

    private async Task UpdateOrganisationIdsClaim(ClaimsPrincipal user, Application.DTOs.UserAccount.User accountUser)
    {
        if (!await _featureManager.IsEnabledAsync(nameof(FeatureFlags.UseGraphApiForExtendedUserClaims)))
        {
            return;
        }

        var orgIdsClaim = user.TryGetOrganisatonIds();
        if (orgIdsClaim is not null)
        {
            _logger.LogInformation("Found claim {Type} with value {Value}", CustomClaimTypes.OrganisationIds, orgIdsClaim);
        }

        var organisationIds = string.Join(",", accountUser.Organisations.Select(o => o.OrganisationNumber));
        if (organisationIds != orgIdsClaim && _graphService is not null)
        {
            await _graphService.PatchUserProperty(accountUser.Id, OrganisationIdsExtensionClaimName, organisationIds);
            _logger.LogInformation("Patched {Type} with value {Value}", OrganisationIdsExtensionClaimName, organisationIds);
        }
    }

    private static string GetControllerName(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint != null)
        {
            return endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName ?? string.Empty;
        }

        return string.Empty;
    }
}
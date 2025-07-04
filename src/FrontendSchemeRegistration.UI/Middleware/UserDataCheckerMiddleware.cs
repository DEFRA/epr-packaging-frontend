namespace FrontendSchemeRegistration.UI.Middleware;

using Application.Options;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using Extensions;
using FrontendSchemeRegistration.Application.Constants;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

public class UserDataCheckerMiddleware : IMiddleware
{
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<UserDataCheckerMiddleware> _logger;
    private readonly FrontEndAccountCreationOptions _frontEndAccountCreationOptions;

    public UserDataCheckerMiddleware(
        IOptions<FrontEndAccountCreationOptions> frontendAccountCreationOptions,
        IUserAccountService userAccountService,
        ILogger<UserDataCheckerMiddleware> logger)
    {
        _frontEndAccountCreationOptions = frontendAccountCreationOptions.Value;
        _userAccountService = userAccountService;
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

            var orgIdsClaim = context.User.TryGetOrganisatonIds();
            if (orgIdsClaim is not null)
            {
                _logger.LogInformation("Found claim {Type} with value {Value}", CustomClaimTypes.OrganisationIds, orgIdsClaim);
            }

            await ClaimsExtensions.UpdateUserDataClaimsAndSignInAsync(context, userData);
        }

        await next(context);
    }

    private static string GetControllerName(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if(endpoint != null)
        {
            return endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName ?? string.Empty;
        }

        return string.Empty;
    }
}
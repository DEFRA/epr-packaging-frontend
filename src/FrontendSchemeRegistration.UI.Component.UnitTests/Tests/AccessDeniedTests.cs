namespace FrontendSchemeRegistration.UI.Component.UnitTests.Tests;

using System.Net;
using System.Text;
using Application.Enums;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using FluentAssertions;
using Infrastructure;
using MockServer.WebApi;
using Sessions;

/// <summary>
/// End-to-end cover for the 403 -> AccessDenied chain. A signed-in user who fails an authorization
/// policy is redirected by the cookie handler to <c>AccessDeniedPath</c>, which is the framework
/// default <c>/Account/AccessDenied</c>. Before this page existed the redirect 404'd and the user
/// was shown "there is a problem with the service" instead.
/// </summary>
public class AccessDeniedTests
{
    private const string ManageComplianceSchemePath = "/report-data/change-compliance-scheme-options";
    private const string AccessDeniedPath = "/report-data/Account/AccessDenied";

    private static readonly Guid TestOrganisationId =
        Guid.Parse("b6f76437-65b6-4ed2-a7d5-c50e9af76201");

    private ComponentTestContext Context { get; } = new();

    [TearDown]
    public void TearDown() => Context.Dispose();

    [Test]
    public async Task WhenBasicUserRequestsManageComplianceScheme_ShouldRedirectToAccessDenied()
    {
        await SignInAs(ServiceRoleConstants.Basic);

        var response = await Context.Client.GetAsync(ManageComplianceSchemePath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain(AccessDeniedPath);
    }

    [TestCase(ServiceRoleConstants.Approved)]
    [TestCase(ServiceRoleConstants.Delegated)]
    public async Task WhenApprovedOrDelegatedPersonRequestsManageComplianceScheme_ShouldNotBeDenied(string serviceRole)
    {
        await SignInAs(serviceRole);

        var response = await Context.Client.GetAsync(ManageComplianceSchemePath);

        // Authorization passes for these roles. This session has no journey set up, so JourneyAccessCheckerMiddleware
        // (which runs after UseAuthorization) sends them back to the self-managed home page rather than to AccessDenied.
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("home-self-managed");
    }

    [Test]
    public async Task WhenAccessDeniedPageIsRequested_ShouldExplainTheProblemAndOfferSupport()
    {
        await SignInAs(ServiceRoleConstants.Basic);

        var response = await Context.Client.GetAsync(AccessDeniedPath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("You do not have permission to access this page");
        content.Should().Contain("mailto:eprcustomerservice@defra.gov.uk");
    }

    [Test]
    public async Task WhenAccessDeniedPageIsRequestedInWelsh_ShouldResolveTheWelshResources()
    {
        await SignInAs(ServiceRoleConstants.Basic);

        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(Language.SessionLanguageKey, Encoding.UTF8.GetBytes(Language.Welsh));

        var response = await Context.Client.GetAsync(AccessDeniedPath);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Does gennych chi ddim caniatâd i gyrchu'r tudalen yma");

        // A missing resource makes IViewLocalizer echo the key, so this would silently pass on English fallback.
        content.Should().NotContain("access_denied_title");
    }

    private async Task SignInAs(string serviceRole)
    {
        Context.SetUp(
            overrideSession: true,
            webApiOptions: new WebApiOptions { ServiceRole = serviceRole });

        await Context.Client.AuthenticateDefaultUser();

        var sessionStore = Context.GetSessionStore();
        sessionStore.Session.Set(
            nameof(FrontendSchemeRegistrationSession),
            Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new FrontendSchemeRegistrationSession
            {
                UserData = new UserData
                {
                    ServiceRole = serviceRole,
                    Organisations =
                    [
                        new Organisation
                        {
                            Id = TestOrganisationId,
                            OrganisationRole = "Producer"
                        }
                    ]
                }
            })));
    }
}

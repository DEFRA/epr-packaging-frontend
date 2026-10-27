namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.ActionFilters;

using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;
using Moq;
using NUnit.Framework;
using UI.Attributes.ActionFilters;
using UI.Sessions;

[TestFixture]
public class RedirectPastObligationYearAttributeTests
{
    private Mock<ActionExecutionDelegate> _delegateMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IFeatureManager> _featureManagerMock;
    private FrontendSchemeRegistrationSession _session;
    private RedirectPastObligationYearFilter _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _delegateMock = new Mock<ActionExecutionDelegate>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = [new Organisation { Name = "Test Organisation", OrganisationRole = OrganisationRoles.Producer, NationId = 1 }]
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(_session);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);

        _systemUnderTest = new RedirectPastObligationYearFilter(_sessionManagerMock.Object, _featureManagerMock.Object);
    }

    [Test]
    public void Attribute_RunsAfterRequestedObligationYearAttribute()
    {
        var attribute = new RedirectPastObligationYearAttribute();

        attribute.ImplementationType.Should().Be(typeof(RedirectPastObligationYearFilter));
        attribute.Order.Should().BeGreaterThan(new RequestedObligationYearAttribute().Order);
    }

    [TestCase(2025)]
    [TestCase(2024)]
    public async Task OnActionExecutionAsync_RedirectsToComplianceCertificate_WhenSessionYearIs2025OrEarlier(int year)
    {
        _session.PrnSession.SelectedObligationYear = year;
        var context = CreateContext();

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        var redirect = context.Result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(PrnsObligationController.ComplianceCertificate));
        redirect.ControllerName.Should().Be("PrnsObligation");
        _delegateMock.Verify(next => next(), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenSessionYearIsSelectable()
    {
        _session.PrnSession.SelectedObligationYear = 2026;
        var context = CreateContext();

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenNoYearIsSelectedInSession()
    {
        _session.PrnSession.SelectedObligationYear = null;
        var context = CreateContext();

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNextWithoutRedirecting_WhenMultiYearObligationsDisabled()
    {
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(false);
        _session.PrnSession.SelectedObligationYear = 2025;
        var context = CreateContext();

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenSessionIsMissing()
    {
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession?)null);
        var context = CreateContext();

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
    }

    private static ActionExecutingContext CreateContext()
    {
        var httpContext = new DefaultHttpContext
        {
            Session = Mock.Of<ISession>()
        };

        return new ActionExecutingContext(
            new ActionContext(httpContext, Mock.Of<RouteData>(), Mock.Of<ActionDescriptor>(), Mock.Of<ModelStateDictionary>()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            Mock.Of<Controller>());
    }
}

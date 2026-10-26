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
using Microsoft.Extensions.Time.Testing;
using Microsoft.FeatureManagement;
using Moq;
using NUnit.Framework;
using UI.Attributes.ActionFilters;
using UI.Sessions;

[TestFixture]
public class RequestedObligationYearAttributeTests
{
    private Mock<ActionExecutionDelegate> _delegateMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<IFeatureManager> _featureManagerMock;
    private FakeTimeProvider _timeProvider;
    private FrontendSchemeRegistrationSession _session;
    private RequestedObligationYearFilter _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _delegateMock = new Mock<ActionExecutionDelegate>();
        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
        _session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                Organisations = [new Organisation { Name = "Test Organisation", OrganisationRole = OrganisationRoles.Producer, NationId = 1 }]
            }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(_session);
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(true);

        _systemUnderTest = new RequestedObligationYearFilter(_sessionManagerMock.Object, _featureManagerMock.Object, _timeProvider);
    }

    [Test]
    public void Attribute_UsesRequestedObligationYearFilter()
    {
        var attribute = new RequestedObligationYearAttribute();

        attribute.ImplementationType.Should().Be(typeof(RequestedObligationYearFilter));
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenNoYearIsInQueryString()
    {
        var context = CreateContext(query: null);

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenYearIsNotANumber()
    {
        var context = CreateContext("year=abc");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNextWithoutRedirecting_WhenRequestIsNotGet()
    {
        var context = CreateContext("year=2025", HttpMethods.Post);

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
        _session.PrnSession.SelectedObligationYear.Should().BeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNextWithoutSelectingYear_WhenSessionIsMissing()
    {
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession?)null);
        var context = CreateContext("year=2025");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNextWithoutRedirecting_WhenMultiYearObligationsDisabled()
    {
        _featureManagerMock.Setup(x => x.IsEnabledAsync(FeatureFlags.ShowMultiYearObligations)).ReturnsAsync(false);
        var context = CreateContext("year=2025");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
        _session.PrnSession.SelectedObligationYear.Should().BeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }

    [TestCase(2025)]
    [TestCase(2024)]
    [TestCase(2000)]
    public async Task OnActionExecutionAsync_RedirectsToComplianceCertificate_WhenYearIs2025OrEarlier(int year)
    {
        var context = CreateContext($"year={year}");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        var redirect = context.Result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(PrnsObligationController.ComplianceCertificate));
        redirect.ControllerName.Should().Be("PrnsObligation");
        _delegateMock.Verify(next => next(), Times.Never);
    }

    [TestCase(2025)]
    [TestCase(2024)]
    public async Task OnActionExecutionAsync_SelectsCertificateYear_WhenYearIs2025OrEarlier(int year)
    {
        var context = CreateContext($"year={year}");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _session.PrnSession.SelectedObligationYear.Should().Be(2025);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), _session), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_SelectsYearAndCallsNext_WhenYearIsSelectable()
    {
        var context = CreateContext("year=2026");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _session.PrnSession.SelectedObligationYear.Should().Be(2026);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), _session), Times.Once);
        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
    }

    [Test]
    public async Task OnActionExecutionAsync_DoesNotSelectYear_WhenYearIsNotSelectable()
    {
        var context = CreateContext("year=2030");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _session.PrnSession.SelectedObligationYear.Should().BeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
        _delegateMock.Verify(next => next(), Times.Once);
        context.Result.Should().BeNull();
    }

    [Test]
    public async Task OnActionExecutionAsync_DoesNotSelectFutureYear_OutsideDecemberJanuaryFlashWindow()
    {
        _systemUnderTest = CreateFilterAt(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
        var context = CreateContext("year=2027");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _session.PrnSession.SelectedObligationYear.Should().BeNull();
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
        _delegateMock.Verify(next => next(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_SelectsFutureYear_InsideDecemberJanuaryFlashWindow()
    {
        _systemUnderTest = CreateFilterAt(new DateTimeOffset(2026, 12, 15, 12, 0, 0, TimeSpan.Zero));
        var context = CreateContext("year=2027");

        await _systemUnderTest.OnActionExecutionAsync(context, _delegateMock.Object);

        _session.PrnSession.SelectedObligationYear.Should().Be(2027);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), _session), Times.Once);
        _delegateMock.Verify(next => next(), Times.Once);
    }

    private RequestedObligationYearFilter CreateFilterAt(DateTimeOffset now) =>
        new(_sessionManagerMock.Object, _featureManagerMock.Object, new FakeTimeProvider(now));

    private static ActionExecutingContext CreateContext(string? query, string method = "GET")
    {
        var httpContext = new DefaultHttpContext
        {
            Session = Mock.Of<ISession>()
        };
        httpContext.Request.Method = method;
        httpContext.Request.QueryString = new QueryString(query is null ? string.Empty : $"?{query}");

        return new ActionExecutingContext(
            new ActionContext(httpContext, Mock.Of<RouteData>(), Mock.Of<ActionDescriptor>(), Mock.Of<ModelStateDictionary>()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            Mock.Of<Controller>());
    }
}

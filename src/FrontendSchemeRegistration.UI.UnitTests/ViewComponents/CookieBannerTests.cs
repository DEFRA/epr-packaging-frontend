namespace FrontendSchemeRegistration.UI.UnitTests.ViewComponents;

using Application.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using UI.ViewComponents;
using UI.ViewModels.Shared;

using CookieOptions = Application.Options.CookieOptions;

[TestFixture]
public class CookieBannerTests
{
    private Mock<HttpRequest> _httpRequest = null!;
    private Mock<HttpContext> _httpContextMock = null!;
    private Mock<ISession> _sessionMock = null!;
    private Mock<IResponseCookies>? _responseCookiesMock;
    private Mock<IRequestCookieCollection> _requestCookies;
    private Mock<ITempDataDictionary> _tempData;
    private RouteData _routeData;

    [SetUp]
    public void Setup()
    {
        _httpRequest = new Mock<HttpRequest>();
        _httpContextMock = new Mock<HttpContext>();
        _responseCookiesMock = new Mock<IResponseCookies>();
        _requestCookies = new Mock<IRequestCookieCollection>();
        _tempData = new Mock<ITempDataDictionary>();
        _routeData = new RouteData();
        _sessionMock = new Mock<ISession>();
        _httpContextMock.Setup(x => x.Session).Returns(_sessionMock.Object);
        _httpRequest.Setup(x => x.Cookies).Returns(_requestCookies.Object);
    }

    [Test]
    public void Invoke_SetsModel()
    {
       // Arrange
        _httpContextMock!
           .Setup(x => x.Response.Cookies)
       .Returns(_responseCookiesMock!.Object);

        var viewContext = new ViewContext();
        _httpContextMock.Setup(x => x.Request).Returns(_httpRequest.Object);
        viewContext.HttpContext = _httpContextMock.Object;

        viewContext.TempData = _tempData.Object;
        viewContext.RouteData = _routeData;
        var viewComponentContext = new ViewComponentContext
        {
            ViewContext = viewContext,
        };

        var eprCookieOptions = new CookieOptions
        {
            AntiForgeryCookieName = null,
            AuthenticationCookieName = null,
            CookiePolicyCookieName = null,
            CookiePolicyDurationInMonths = 60,
            SessionCookieName = null,
            TempDataCookie = null,
            TsCookieName = null
        };

        var options = Options.Create(eprCookieOptions);
        var component = new CookieBannerViewComponent(options);
        component.ViewComponentContext = viewComponentContext;

        var model = (CookieBannerModel)((ViewViewComponentResult)component.Invoke("/TestUrl")).ViewData!.Model!;
        model.Should().NotBeNull();
        model.AcceptAnalytics.Should().BeFalse();
        model.ShowAcknowledgement.Should().BeFalse();
        model.ShowBanner.Should().BeTrue();
    }

    [Test]
    public void Invoke_WhenValuesSet_SetsShowBannerToFalseAndAcceptAnalyticsToTrue()
    {
        // Arrange#
        var cookiePolicyCookieName = "CookiePolicyCookieName";

        _httpContextMock!
            .Setup(x => x.Response.Cookies)
            .Returns(_responseCookiesMock!.Object);
        _httpContextMock.Setup(x => x.Request).Returns(_httpRequest.Object);

        var tempData = new TempDataDictionary(_httpContextMock.Object, Mock.Of<ITempDataProvider>())
        {
            [CookieAcceptance.CookieAcknowledgement] = CookieAcceptance.Accept
        };
        _routeData.Values.Add("controller", "Cookies");

        var viewContext = new ViewContext
        {
            HttpContext = _httpContextMock.Object,
            TempData = tempData,
            RouteData = _routeData
        };
        var viewComponentContext = new ViewComponentContext
        {
            ViewContext = viewContext,
        };

        var eprCookieOptions = new CookieOptions
        {
            AntiForgeryCookieName = null,
            AuthenticationCookieName = null,
            CookiePolicyCookieName = cookiePolicyCookieName,
            CookiePolicyDurationInMonths = 60,
            SessionCookieName = null,
            TempDataCookie = null,
            TsCookieName = null
        };

        var options = Options.Create(eprCookieOptions);
        var component = new CookieBannerViewComponent(options)
        {
            ViewComponentContext = viewComponentContext
        };

        var model = (CookieBannerModel)((ViewViewComponentResult)component.Invoke("/TestUrl")).ViewData!.Model!;
        model.Should().NotBeNull();
        model.ShowAcknowledgement.Should().BeFalse();
        model.AcceptAnalytics.Should().BeTrue();
        model.ShowBanner.Should().BeFalse();
    }

    [Test]
    public void Invoke_WhenValuesSet_SetsShowAcknowledgementsToTrue()
    {
        // Arrange#
        var cookiePolicyCookieName = "CookiePolicyCookieName";

        _httpContextMock!
            .Setup(x => x.Response.Cookies)
            .Returns(_responseCookiesMock!.Object);
        _httpContextMock.Setup(x => x.Request).Returns(_httpRequest.Object);

        var tempData = new TempDataDictionary(_httpContextMock.Object, Mock.Of<ITempDataProvider>())
        {
            [CookieAcceptance.CookieAcknowledgement] = CookieAcceptance.Accept
        };

        var viewContext = new ViewContext
        {
            HttpContext = _httpContextMock.Object,
            TempData = tempData,
            RouteData = _routeData
        };
        var viewComponentContext = new ViewComponentContext
        {
            ViewContext = viewContext,
        };

        var eprCookieOptions = new CookieOptions
        {
            AntiForgeryCookieName = null,
            AuthenticationCookieName = null,
            CookiePolicyCookieName = cookiePolicyCookieName,
            CookiePolicyDurationInMonths = 60,
            SessionCookieName = null,
            TempDataCookie = null,
            TsCookieName = null
        };

        var options = Options.Create(eprCookieOptions);
        var component = new CookieBannerViewComponent(options)
        {
            ViewComponentContext = viewComponentContext
        };

        var model = (CookieBannerModel)((ViewViewComponentResult)component.Invoke("/TestUrl")).ViewData!.Model!;
        model.Should().NotBeNull();
        model.ShowAcknowledgement.Should().BeTrue();
    }
}
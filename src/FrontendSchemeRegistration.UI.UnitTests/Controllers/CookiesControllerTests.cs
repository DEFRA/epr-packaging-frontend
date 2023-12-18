namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.Options;
using Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Moq;
using UI.Controllers.Cookies;
using UI.ViewModels;

using CookieOptions = Application.Options.CookieOptions;

[TestFixture]
public class CookiesControllerTests
{
    private Mock<HttpContext>? _httpContextMock;
    private Mock<HttpRequest> _httpRequest = null!;
    private Mock<IOptions<GoogleAnalyticsOptions>> _analyticsOptions = null;
    private Mock<IOptions<CookieOptions>> _eprCookiesOptions = null;
    private Mock<ICookieService> _cookieService = null;

    private CookiesController _systemUnderTest = null!;

    [SetUp]
    public void TestInitialize()
    {
        _httpContextMock = new Mock<HttpContext>();
        _httpRequest = new Mock<HttpRequest>();
        _cookieService = new Mock<ICookieService>();

        _analyticsOptions = new Mock<IOptions<GoogleAnalyticsOptions>>();
        _eprCookiesOptions = new Mock<IOptions<CookieOptions>>();

        _analyticsOptions!
            .Setup(x => x.Value)
            .Returns(new GoogleAnalyticsOptions
            {
                CookiePrefix = "_ga",
                MeasurementId = "VMDE8PW9W7",
                TagManagerContainerId = "G-VMDE8PW9W7"
            });

        _eprCookiesOptions!
            .Setup(x => x.Value)
            .Returns(new CookieOptions
            {
                CookiePolicyDurationInMonths = 3,
                SessionCookieName = "SessionCookieNameTest",
                CookiePolicyCookieName = "CookiePolicyCookieNameTest",
                AntiForgeryCookieName = "AntiForgeryCookieNameTest",
                TsCookieName = "AntiForgeryCookieNameTest",
                CorrelationCookieName = "CorrelationCookieNameTest",
                OpenIdCookieName = "OpenIdCookieNameTest",
                B2CCookieName = "B2CCookieNameTest",
                AuthenticationCookieName = "AuthenticationCookieNameTest",
                TempDataCookie = "TempDataCookieTest"
            });

        _systemUnderTest = new CookiesController(_cookieService.Object, _eprCookiesOptions.Object, _analyticsOptions.Object);

        _systemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;
        _httpContextMock.Setup(x => x.Request).Returns(_httpRequest.Object);
    }

    [Test]
    public void Detail_CookieNotAccepted_SetsModel()
    {
        const string returnUrl = "/report-data/test?p=1";

        var result = _systemUnderTest!.Detail(returnUrl);
        var viewResult = (ViewResult)result;
        var cookieDetailViewModel = (CookieDetailViewModel)viewResult.Model!;

        result.Should().BeOfType(typeof(ViewResult));
        viewResult.ViewData["BackLinkToDisplay"].Should().Be(returnUrl);
        viewResult.ViewData["CurrentPage"].Should().Be(returnUrl);
        cookieDetailViewModel.AntiForgeryCookieName.Should().Be(_eprCookiesOptions.Object.Value.AntiForgeryCookieName);
        cookieDetailViewModel.AuthenticationCookieName.Should().Be(_eprCookiesOptions.Object.Value.AuthenticationCookieName);
        cookieDetailViewModel.B2CCookieName.Should().Be(_eprCookiesOptions.Object.Value.B2CCookieName);
        cookieDetailViewModel.CookiePolicyCookieName.Should().Be(_eprCookiesOptions.Object.Value.CookiePolicyCookieName);
        cookieDetailViewModel.CorrelationCookieName.Should().Be(_eprCookiesOptions.Object.Value.CorrelationCookieName);
        cookieDetailViewModel.GoogleAnalyticsAdditionalCookieName.Should().Be(_analyticsOptions.Object.Value.AdditionalCookieName);
        cookieDetailViewModel.GoogleAnalyticsDefaultCookieName.Should().Be(_analyticsOptions.Object.Value.DefaultCookieName);
        cookieDetailViewModel.OpenIdCookieName.Should().Be(_eprCookiesOptions.Object.Value.OpenIdCookieName);
        cookieDetailViewModel.SessionCookieName.Should().Be(_eprCookiesOptions.Object.Value.SessionCookieName);
        cookieDetailViewModel.TempDataCookieName.Should().Be(_eprCookiesOptions.Object.Value.TempDataCookie);
        cookieDetailViewModel.TsCookieName.Should().Be(_eprCookiesOptions.Object.Value.TsCookieName);
        cookieDetailViewModel.CookiesAccepted.Should().BeFalse();
        cookieDetailViewModel.ShowAcknowledgement.Should().BeFalse();
    }

    [Test]
    public void Detail_CookieNotAcceptedWithParam_SetsModel()
    {
        const string returnUrl = "/report-data/test?p=2";

        var result = _systemUnderTest!.Detail(returnUrl, false);
        var viewResult = (ViewResult)result;
        var cookieDetailViewModel = (CookieDetailViewModel)viewResult.Model!;

        viewResult.ViewData["BackLinkToDisplay"].Should().Be(returnUrl);
        viewResult.ViewData["CurrentPage"].Should().Be(returnUrl);

        result.Should().BeOfType(typeof(ViewResult));
        cookieDetailViewModel.CookiesAccepted.Should().BeFalse();
        cookieDetailViewModel.ShowAcknowledgement.Should().BeTrue();
    }

    [Test]
    public void Detail_CookieAcceptedWithParam_SetsModel()
    {
        const string returnUrl = "/report-data/test?p=2";

        var result = _systemUnderTest!.Detail(returnUrl, true);
        var viewResult = (ViewResult)result;
        var cookieDetailViewModel = (CookieDetailViewModel)viewResult.Model!;

        result.Should().BeOfType(typeof(ViewResult));
        viewResult.ViewData["BackLinkToDisplay"].Should().Be(returnUrl);
        viewResult.ViewData["CurrentPage"].Should().Be(returnUrl);
        cookieDetailViewModel.CookiesAccepted.Should().BeTrue();
        cookieDetailViewModel.ShowAcknowledgement.Should().BeTrue();
    }

    [Test]
    public void Detail_InvalidReturnUrl_SetBacklinkAndCurrentPage()
    {
        const string returnUrl = "/invalid-url/test?p=2";

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(m => m.IsLocalUrl(It.IsAny<string>()))
            .Returns(true)
            .Verifiable();

        mockUrlHelper
            .Setup(m => m.Content("~/"))
            .Returns("/report-data")
            .Verifiable();

        _systemUnderTest.Url = mockUrlHelper.Object;

        var result = _systemUnderTest!.Detail(returnUrl);
        var viewResult = (ViewResult)result;
        var cookieDetailViewModel = (CookieDetailViewModel)viewResult.Model!;

        result.Should().BeOfType(typeof(ViewResult));
        viewResult.ViewData["BackLinkToDisplay"].Should().Be("/report-data");
        viewResult.ViewData["CurrentPage"].Should().Be("/report-data");
    }

    [Test]
    public void Detail_PostCookieAccepted_SetsTempData()
    {
        const string returnUrl = "/report-data/test?p=1";
        var tempData = new TempDataDictionary(_httpContextMock.Object, Mock.Of<ITempDataProvider>());
        tempData[CookieAcceptance.CookieAcknowledgement] = "NOT_SET";
        _systemUnderTest.TempData = tempData;

        var requestCookiesMock = new Mock<IRequestCookieCollection>();
        _httpContextMock.Setup(ctx => ctx.Request.Cookies).Returns(requestCookiesMock.Object);

        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.SetReturnsDefault(responseCookiesMock.Object);
        _httpContextMock.Setup(ctx => ctx.Response.Cookies).Returns(responseCookiesMock.Object);

        var result = _systemUnderTest!.Detail(returnUrl, CookieAcceptance.Accept);
        var viewResult = (ViewResult)result;
        var cookieDetailViewModel = (CookieDetailViewModel)viewResult.Model!;

        tempData[CookieAcceptance.CookieAcknowledgement].Should().Be(CookieAcceptance.Accept);
    }

    [Test]
    public void Detail_PostCookieRejected_SetsTempData()
    {
        const string returnUrl = "/report-data/test?p=1";
        var tempData = new TempDataDictionary(_httpContextMock.Object, Mock.Of<ITempDataProvider>());
        tempData[CookieAcceptance.CookieAcknowledgement] = "NOT_SET";
        _systemUnderTest.TempData = tempData;

        var requestCookiesMock = new Mock<IRequestCookieCollection>();
        _httpContextMock.Setup(ctx => ctx.Request.Cookies).Returns(requestCookiesMock.Object);

        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.SetReturnsDefault(responseCookiesMock.Object);
        _httpContextMock.Setup(ctx => ctx.Response.Cookies).Returns(responseCookiesMock.Object);

        var result = _systemUnderTest!.Detail(returnUrl, CookieAcceptance.Reject);
        var viewResult = (ViewResult)result;
        var cookieDetailViewModel = (CookieDetailViewModel)viewResult.Model!;

        tempData[CookieAcceptance.CookieAcknowledgement].Should().Be(CookieAcceptance.Reject);
    }

    [Test]
    public void UpdateAcceptance_RedirectsToCorrectUrl()
    {
        // Arrange
        const string returnUrl = "~/home/index";
        const string cookies = "dummy cookie";

        var tempData = new TempDataDictionary(_httpContextMock.Object, Mock.Of<ITempDataProvider>());
        tempData[CookieAcceptance.CookieAcknowledgement] = "NOT_SET";
        _systemUnderTest.TempData = tempData;

        var requestCookiesMock = new Mock<IRequestCookieCollection>();
        _httpContextMock.Setup(ctx => ctx.Request.Cookies).Returns(requestCookiesMock.Object);

        var responseCookiesMock = new Mock<IResponseCookies>();
        responseCookiesMock.SetReturnsDefault(responseCookiesMock.Object);
        _httpContextMock.Setup(ctx => ctx.Response.Cookies).Returns(responseCookiesMock.Object);

        // Act
        var result = _systemUnderTest.UpdateAcceptance(returnUrl, cookies);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType(typeof(LocalRedirectResult));
    }

    [Test]
    public void AcknowledgeAcceptance_RedirectsToCorrectUrl()
    {
        // Arrange
        const string returnUrl = "~/home/index";
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(m => m.IsLocalUrl(It.IsAny<string>()))
            .Returns(true)
            .Verifiable();
        _systemUnderTest.Url = mockUrlHelper.Object;

        // Act
        var result = _systemUnderTest!.AcknowledgeAcceptance(returnUrl);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType(typeof(LocalRedirectResult));
    }
}

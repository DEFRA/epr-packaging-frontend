namespace FrontendSchemeRegistration.UI.UnitTests.ViewComponents;

using System.Security.Claims;
using Application.Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;
using Moq;
using Application.Options;
using Newtonsoft.Json;
using UI.ViewComponents;
using UI.ViewModels.Shared;

[TestFixture]
public class PrimaryNavigationViewComponentTests
{
    private PrimaryNavigationViewComponent _systemUnderTest;
    private ExternalUrlOptions _externalUrlOptions;
    private FrontEndAccountManagementOptions _frontEndAccountManagementOptions;

    [SetUp]
    public void Setup()
    {
        _externalUrlOptions = new ExternalUrlOptions { LandingPage = "/landing-page-url" };
        _frontEndAccountManagementOptions = new FrontEndAccountManagementOptions { BaseUrl = "/account-management-base-url" };

        var userData = new UserData { Id = Guid.NewGuid() };
        var claims = new List<Claim> { new(ClaimTypes.UserData, JsonConvert.SerializeObject(userData)) };
        var userMock = new Mock<ClaimsPrincipal>();

        userMock.Setup(x => x.Claims).Returns(claims);

        _systemUnderTest = new PrimaryNavigationViewComponent(Options.Create(_externalUrlOptions), Options.Create(_frontEndAccountManagementOptions));
    }

    [Test]
    [TestCase(PagePaths.ComplianceSchemeLanding, true)]
    [TestCase(PagePaths.ComplianceSchemeMemberLanding, true)]
    [TestCase(PagePaths.HomePageSelfManaged, true)]
    [TestCase(PagePaths.FileUpload, false)]
    public async Task PrimaryNavigationViewComponent_RendersWithCorrectViewModel(string pagePath, bool homeButtonIsActive)
    {
        // Arrange
        var userData = new UserData { Id = Guid.NewGuid() };
        var claims = new List<Claim> { new(ClaimTypes.UserData, JsonConvert.SerializeObject(userData)) };
        var context = new DefaultHttpContext
        {
            Request = { Path = pagePath.EnsureLeadingSlash() },
            User = new ClaimsPrincipal(new ClaimsIdentity(claims))
        };
        var viewContext = new ViewContext { HttpContext = context };
        var viewComponentContext = new ViewComponentContext { ViewContext = viewContext };

        _systemUnderTest.ViewComponentContext = viewComponentContext;

        // Act
        var result = _systemUnderTest.Invoke();

        // Assert
        result.ViewData?.Model.Should().BeEquivalentTo(new PrimaryNavigationModel
        {
            Items = new List<NavigationModel>
            {
                new()
                {
                    LinkValue = "/landing-page-url",
                    LocalizerKey = "home",
                    IsActive = homeButtonIsActive
                },
                new()
                {
                    LinkValue = "/account-management-base-url/",
                    LocalizerKey = "manage_account_details",
                }
            }
        });
    }

    [Test]
    [TestCase(PagePaths.Cookies, false)]
    public async Task PrimaryNavigationViewComponent_RendersWithCorrectViewModel_WhenUserNotInDB(string pagePath, bool homeButtonIsActive)
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request = { Path = pagePath.EnsureLeadingSlash() },
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var viewContext = new ViewContext { HttpContext = context };
        var viewComponentContext = new ViewComponentContext { ViewContext = viewContext };

        _systemUnderTest.ViewComponentContext = viewComponentContext;

        // Act
        var result = _systemUnderTest.Invoke();

        // Assert
        result.ViewData?.Model.Should().BeEquivalentTo(new PrimaryNavigationModel
        {
            Items = new List<NavigationModel>()
        });
    }
}
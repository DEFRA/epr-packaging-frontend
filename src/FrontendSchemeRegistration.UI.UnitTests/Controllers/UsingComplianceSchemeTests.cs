namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Controllers.FrontendSchemeRegistration;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class UsingComplianceSchemeTests : FrontendSchemeRegistrationTestBase
{
    private const string ModelErrorValue = "Select if you're using a compliance scheme";
    private const string ViewName = "UsingComplianceScheme";
    private UserData _userData;

    [SetUp]
    public void Setup()
    {
        _userData = new UserData
        {
            Organisations = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Some org",
                    OrganisationRole = "Producer"
                }
            }
        };

        SetupBase(_userData);

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new()
            {
                Journey = new List<string>
                {
                    PagePaths.LandingPage, PagePaths.UsingAComplianceScheme,
                }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);
    }

    [Test]
    public async Task GivenOnUsingComplianceSchemePage_WhenUsingAComplianceSchemeHttpGetCalled_ThenUsingComplianceSchemeViewModelReturned_WithLandingPageAsTheBackLink()
    {
        // Act
        var result = await SystemUnderTest.UsingAComplianceScheme() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        AssertBackLink(result, PagePaths.LandingPage);
    }

    // selected yes
    [Test]
    [TestCase(true)]
    public async Task GivenOnUsingComplianceSchemePage_WhenUsingComplianceSchemePageHttpPostCalled_SelectedYes_ThenRedirectToSelectComplianceScheme_AndUpdateSession(bool usingComplianceScheme)
    {
        // Act
        var viewModel = new UsingComplianceSchemeViewModel
        {
            UsingComplianceScheme = usingComplianceScheme,
        };

        var result = await SystemUnderTest.UsingAComplianceScheme(viewModel) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.SelectComplianceScheme));
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    // selected no
    [Test]
    [TestCase(false)]
    public async Task GivenOnUsingComplianceSchemePage_WhenUsingComplianceSchemePageHttpPostCalled_SelectedNo_ThenRedirectToSelectComplianceScheme_AndUpdateSession(bool usingComplianceScheme)
    {
        // Act
        var viewModel = new UsingComplianceSchemeViewModel
        {
            UsingComplianceScheme = usingComplianceScheme,
        };

        var result = await SystemUnderTest.UsingAComplianceScheme(viewModel) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    // selected nothing
    [Test]
    public async Task GivenOnUsingComplianceSchemePage_WhenUsingComplianceSchemePageHttpPostCalled_SelectedNothing_ThenReturnViewWithModelErrors_ConfirmNoSessionUpdates()
    {
        // Act
        var viewModel = new UsingComplianceSchemeViewModel();
        SystemUnderTest.ModelState.AddModelError(ModelErrorKey, ModelErrorValue);

        var result = await SystemUnderTest.UsingAComplianceScheme(viewModel) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.ViewData.ModelState["Error"].Errors[0].ErrorMessage.Should().Be(ModelErrorValue);
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }
}
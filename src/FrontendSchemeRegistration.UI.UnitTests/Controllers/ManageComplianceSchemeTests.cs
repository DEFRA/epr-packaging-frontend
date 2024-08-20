namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Enums;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Controllers.FrontendSchemeRegistration;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class ManageComplianceSchemeTests : FrontendSchemeRegistrationTestBase
{
    private const string ModelErrorValue = "Select whether you've changed compliance scheme or no longer use one.";
    private const string ViewName = "ChangeComplianceSchemeOptions";
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
                    PagePaths.LandingPage,
                    PagePaths.UsingAComplianceScheme,
                    PagePaths.SelectComplianceScheme,
                    PagePaths.ComplianceSchemeSelectionConfirmation,
                    PagePaths.HomePageComplianceScheme,
                    PagePaths.ChangeComplianceSchemeOptions,
                }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);
    }

    [Test]
    public async Task
        GivenOnChangeComplianceSchemeOptionsPage_WhenManageComplianceSchemeHttpGetCalledForOrganisation_ThenChangeComplianceSchemeOptionsViewModelReturned_WithHomePageComplianceSchemeAsTheBackLink()
    {
        // Act
        var result = await SystemUnderTest.ManageComplianceScheme() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        AssertBackLink(result, PagePaths.HomePageComplianceScheme);
    }

    // selected I have changed compliance scheme
    [Test]
    [TestCase(ChangeComplianceSchemeOptions.ChooseNewComplianceScheme)]
    public async Task
        GivenOnChangeComplianceSchemeOptionsPage_WhenManageComplianceSchemeHttpPostCalled_SelectedChooseNew_ThenRedirectToSelectComplianceScheme_AndUpdateSession(
            ChangeComplianceSchemeOptions selectedOption)
    {
        // Act
        var viewModel = new ChangeComplianceSchemeOptionsViewModel
        {
            ChangeComplianceSchemeOptions = selectedOption,
        };

        var result = await SystemUnderTest.ManageComplianceScheme(viewModel) as RedirectToActionResult;

        // Assert
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney.Should().BeTrue();
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.SelectComplianceScheme));
        SessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    // selected stop compliance scheme
    [Test]
    [TestCase(ChangeComplianceSchemeOptions.StopComplianceScheme)]
    public async Task
        GivenOnChangeComplianceSchemeOptionsPage_WhenManageComplianceSchemeCalledForOrganisation_SelectedStop_ThenRedirectToSelectComplianceScheme_AndUpdateSession(
            ChangeComplianceSchemeOptions selectedOption)
    {
        // Act
        var viewModel = new ChangeComplianceSchemeOptionsViewModel
        {
            ChangeComplianceSchemeOptions = selectedOption,
        };

        var result = await SystemUnderTest.ManageComplianceScheme(viewModel) as RedirectToActionResult;

        // Assert
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney.Should().BeFalse();
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RemoveComplianceScheme));
        SessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    // selected nothing
    [Test]
    public async Task
        GivenOnChangeComplianceSchemeOptionsPage_WhenManageComplianceSchemeCalledForOrganisation_SelectedNothing_ThenReturnViewWithModelErrors_ConfirmNoSessionUpdates()
    {
        // Act
        var viewModel = new ChangeComplianceSchemeOptionsViewModel();
        SystemUnderTest.ModelState.AddModelError(ModelErrorKey, ModelErrorValue);

        var result = await SystemUnderTest.ManageComplianceScheme(viewModel) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.ViewData.ModelState["Error"].Errors[0].ErrorMessage.Should().Be(ModelErrorValue);
        SessionManagerMock.Verify(
            x => x.SaveSessionAsync(
                It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }
}
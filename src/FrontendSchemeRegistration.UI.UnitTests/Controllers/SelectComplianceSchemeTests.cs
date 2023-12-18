namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Controllers.FrontendSchemeRegistration;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class SelectComplianceSchemeTests : FrontendSchemeRegistrationTestBase
{
    private const string ModelErrorValue = "Select which compliance scheme you're using";
    private const string ViewName = "SelectComplianceScheme";
    private const string OrganisationName = "Test Organisation";
    private const string OrganisationRole = OrganisationRoles.Producer;
    private readonly Guid _organisationId = Guid.NewGuid();
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
                    Id = _organisationId,
                    Name = "Some org",
                    OrganisationRole = "Producer"
                }
            }
        };

        SetupBase(_userData);

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string>
                {
                    PagePaths.LandingPage,
                    PagePaths.UsingAComplianceScheme,
                    PagePaths.SelectComplianceScheme,
                },
            },
            UserData = new()
            {
                Organisations = new()
                {
                    new ()
                    {
                        Id = _organisationId,
                        Name = OrganisationName,
                        OrganisationRole = OrganisationRole
                    }
                }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);
    }

    // From using a compliance scheme
    [Test]
    public async Task GivenOnSelectComplianceSchemePage_WhenSelectComplianceSchemeHttpGetCalledForOrganisation_WithCallFromUsingComplianceScheme_ThenSelectComplianceSchemeViewModelReturned_WithUsingComplianceSchemePageAsTheBackLink()
    {
        // Act
        var result = await SystemUnderTest.SelectComplianceScheme() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        AssertBackLink(result, PagePaths.UsingAComplianceScheme);
    }

    // From home page self managed -> add scheme
    [Test]
    public async Task GivenOnSelectComplianceSchemePage_WhenSelectComplianceSchemeHttpGetCalledForOrganisation_WithCallFromHomePageSelfManaged_ThenSelectComplianceSchemeViewModelReturned_WithHomePageSelfManagedPageAsTheBackLink()
    {
        // Arrange
        FrontEndSchemeRegistrationSession.RegistrationSession.Journey = new List<string>
        {
            PagePaths.LandingPage,
            PagePaths.UsingAComplianceScheme,
            PagePaths.HomePageSelfManaged,
            PagePaths.SelectComplianceScheme,
        };

        // Act
        var result = await SystemUnderTest.SelectComplianceScheme() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        AssertBackLink(result, PagePaths.HomePageSelfManaged);
    }

    // From Change Compliance Scheme Options -> update scheme
    [Test]
    public async Task GivenOnSelectComplianceSchemePage_WhenSelectComplianceSchemeHttpGetCalledForOrganisation_WithCallFromChangeComplianceSchemeOptions_ThenSelectComplianceSchemeViewModelReturned_WithChangeComplianceSchemeOptionsPageAsTheBackLink()
    {
        // Arrange
        FrontEndSchemeRegistrationSession.RegistrationSession.Journey = new List<string>
        {
            PagePaths.LandingPage,
            PagePaths.UsingAComplianceScheme,
            PagePaths.SelectComplianceScheme,
            PagePaths.ComplianceSchemeSelectionConfirmation,
            PagePaths.HomePageComplianceScheme,
            PagePaths.ChangeComplianceSchemeOptions,
            PagePaths.SelectComplianceScheme,
        };
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney = true;

        // Act
        var result = await SystemUnderTest.SelectComplianceScheme() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        AssertBackLink(result, PagePaths.ChangeComplianceSchemeOptions);
    }

    // Get compliance schemes NOT successful
    [Test]
    public async Task GivenOnSelectComplianceSchemePage_WhenSelectComplianceSchemePageHttpGetCalled_GetComplianceSchemesFailed_ThenThrowException_AndDoNotUpdateSession()
    {
        // Arrange
        FrontEndSchemeRegistrationSession.UserData.Organisations.FirstOrDefault().Id = _organisationId;
        ComplianceSchemeService.Setup(x => x.GetComplianceSchemes()).ThrowsAsync(new HttpRequestException());

        // Act
        var result = SystemUnderTest.SelectComplianceScheme();

        // Assert
        result.Exception.InnerException.Should().BeOfType<HttpRequestException>();
    }

    // selected scheme
    [Test]
    public async Task GivenOnSelectComplianceSchemePage_WhenSelectComplianceSchemeHttpPostCalled_SelectedScheme_ThenRedirectToConfirmComplianceScheme_AndUpdateSession()
    {
        // Act
        var viewModel = new SelectComplianceSchemeViewModel
        {
            SelectedComplianceSchemeValues =
                $"{SelectedComplianceScheme.Id}:{SelectedComplianceScheme.Name}",
        };

        var result = await SystemUnderTest.SelectComplianceScheme(viewModel) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.ConfirmComplianceScheme));
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    // selected nothing
    [Test]
    public async Task GivenOnSelectComplianceSchemePage_WhenSelectComplianceSchemeHttpPostCalled_SelectedNothing_ThenReturnViewWithModelErrors_ConfirmNoSessionUpdates()
    {
        // Act
        var viewModel = new SelectComplianceSchemeViewModel();
        SystemUnderTest.ModelState.AddModelError(ModelErrorKey, ModelErrorValue);

        var result = await SystemUnderTest.SelectComplianceScheme(viewModel) as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.ViewData.ModelState["Error"].Errors[0].ErrorMessage.Should().Be(ModelErrorValue);
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
    }
}
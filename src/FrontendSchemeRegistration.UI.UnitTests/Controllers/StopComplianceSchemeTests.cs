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
public class StopComplianceSchemeTests : FrontendSchemeRegistrationTestBase
{
    private const string ViewName = "Stop";
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
                    PagePaths.ComplianceSchemeStop,
                }
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

    [Test]
    public async Task GivenOnStopComplianceSchemePage_WhenStopComplianceSchemeHttpGetCalled_ThenComplianceSchemeStopViewModelReturned_WithChangeComplianceSchemeOptionsPageAsTheBackLink()
    {
        // Act
        var result = await SystemUnderTest.StopComplianceScheme() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        AssertBackLink(result, PagePaths.ChangeComplianceSchemeOptions);
    }

    // Successful
    [Test]
    [TestCase("00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000002")]
    public async Task GivenOnStopComplianceSchemePage_WhenStopComplianceSchemePageHttpPostCalled_APICallSuccessful_ThenRedirectToVisitHomePageSelfManaged_AndUpdateSession(string selectedSchemeId, string organisationId)
    {
        // Arrange
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme = CurrentComplianceScheme;
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney = true;
        ComplianceSchemeService.Setup(x => x.StopComplianceScheme(new Guid(selectedSchemeId), new Guid(organisationId)))
            .ReturnsAsync(new HttpResponseMessage());

        // Act
        var viewModel = new ComplianceSchemeStopViewModel();
        var result = await SystemUnderTest.StopComplianceScheme(viewModel) as RedirectToActionResult;

        // Assert
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme.Should().BeNull();
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney.Should().BeFalse();
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    // Not successful
    [Test]
    public async Task GivenOnStopComplianceSchemePage_WhenStopComplianceSchemePageHttpPostCalled_APICallNotSuccessful_ThenThrowException()
    {
        // Arrange
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme = CurrentComplianceScheme;
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney = true;
        ComplianceSchemeService.Setup(x => x.StopComplianceScheme(It.IsAny<Guid>(), It.IsAny<Guid>())).Throws(new HttpRequestException());
        SetupBase(_userData);

        // Act
        var viewModel = new ComplianceSchemeStopViewModel();

        try
        {
            await SystemUnderTest.StopComplianceScheme(viewModel);
        }
        catch (Exception expectedException)
        {
            // Assert
            expectedException.GetType().Should().Be(typeof(NullReferenceException));
            FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme.ComplianceSchemeId.Should()
                .Be(CurrentComplianceScheme.ComplianceSchemeId);
            FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney.Should().BeTrue();
        }
        finally
        {
            SessionManagerMock.Verify(
                x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()),
                Times.Never);
        }
    }
}
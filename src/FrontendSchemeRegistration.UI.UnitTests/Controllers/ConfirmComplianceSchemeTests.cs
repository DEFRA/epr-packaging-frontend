namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Controllers;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class ConfirmComplianceSchemeTests : FrontendSchemeRegistrationTestBase
{
    private const string ViewName = "Confirmation";
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
                    Name = OrganisationName,
                    OrganisationRole = OrganisationRole
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
                }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);
    }

    [Test]
    public async Task
        GivenOnConfirmComplianceSchemePage_WhenConfirmComplianceSchemeHttpGetCalled_APISuccessful_ThenComplianceSchemeConfirmationViewModelReturned_WithSelectComplianceSchemePageAsTheBackLink()
    {
        // Act
        var result = await SystemUnderTest.ConfirmComplianceScheme() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        AssertBackLink(result, PagePaths.SelectComplianceScheme);
    }

    // Add compliance scheme successful
    [Test]
    public async Task
        GivenOnConfirmComplianceSchemePage_WhenConfirmComplianceSchemePageHttpPostCalled_AddComplianceSchemeSuccessful_ThenRedirectToVisitHomePageComplianceScheme_AndUpdateSession()
    {
        // Arrange
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme = null;

        ComplianceSchemeService.Setup(x => x.ConfirmAddComplianceScheme(
                SelectedComplianceScheme.Id,
                _organisationId))
            .ReturnsAsync(CommittedSelectedScheme);

        // Act
        var viewModel = new ComplianceSchemeConfirmationViewModel
        {
            CurrentComplianceScheme = null,
            SelectedComplianceScheme = SelectedComplianceScheme,
        };

        var result = await SystemUnderTest.ConfirmComplianceScheme(viewModel) as RedirectToActionResult;

        // Assert
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme.ComplianceSchemeId.Should().Be(SelectedComplianceScheme.Id);
        result.ActionName.Should().Be(nameof(ComplianceSchemeMemberLandingController.Get));
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    // Update compliance scheme successful
    [Test]
    public async Task
        GivenOnConfirmComplianceSchemePage_WhenConfirmComplianceSchemePageHttpPostCalled_UpdateComplianceSchemeSuccessful_ThenRedirectToVisitHomePageComplianceScheme_AndUpdateSession()
    {
        // Arrange
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme = CurrentComplianceScheme;
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney = true;

        ComplianceSchemeService.Setup(x => x.ConfirmUpdateComplianceScheme(
                SelectedComplianceScheme.Id,
                CurrentComplianceScheme.SelectedSchemeId,
                _organisationId))
            .ReturnsAsync(CommittedSelectedScheme);

        // Act
        var viewModel = new ComplianceSchemeConfirmationViewModel
        {
            SelectedComplianceScheme = SelectedComplianceScheme,
            CurrentComplianceScheme = CurrentComplianceScheme,
        };

        var result = await SystemUnderTest.ConfirmComplianceScheme(viewModel) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeMemberLandingController.Get));
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme.ComplianceSchemeId.Should().Be(SelectedComplianceScheme.Id);
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    // Add compliance scheme NOT successful
    [Test]
    public async Task
        GivenOnConfirmComplianceSchemePage_WhenConfirmComplianceSchemePageHttpPostCalled_AddComplianceSchemeFailed_ThenThrowException_AndDoNotUpdateSession()
    {
        // Arrange
        ComplianceSchemeService.Setup(x => x.ConfirmAddComplianceScheme(
            SelectedComplianceScheme.Id,
            _organisationId)).ThrowsAsync(new HttpRequestException());
        SetupBase(_userData);

        // Act
        var viewModel = new ComplianceSchemeConfirmationViewModel
        {
            SelectedComplianceScheme = SelectedComplianceScheme,
            CurrentComplianceScheme = CurrentComplianceScheme,
        };

        try
        {
            SystemUnderTest.ConfirmComplianceScheme(viewModel);
        }
        catch (Exception expectedException)
        {
            // Assert
            expectedException.GetType().Should().Be(typeof(HttpRequestException));
            FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme.Should().BeNull();
        }
        finally
        {
            SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
        }
    }

    // Update compliance scheme NOT successful
    [Test]
    public async Task
        GivenOnConfirmComplianceSchemePage_WhenConfirmComplianceSchemePageHttpPostCalled_UpdateComplianceSchemeFailed_ThenThrowException_AndDoNotUpdateSession()
    {
        // Arrange
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme = CurrentComplianceScheme;
        FrontEndSchemeRegistrationSession.RegistrationSession.IsUpdateJourney = true;
        ComplianceSchemeService.Setup(x => x.ConfirmUpdateComplianceScheme(
            SelectedComplianceScheme.Id,
            CurrentComplianceScheme.ComplianceSchemeId,
            _organisationId)).ThrowsAsync(new HttpRequestException());
        SetupBase(_userData);

        // Act
        var viewModel = new ComplianceSchemeConfirmationViewModel
        {
            SelectedComplianceScheme = SelectedComplianceScheme,
            CurrentComplianceScheme = CurrentComplianceScheme,
        };

        try
        {
            await SystemUnderTest.ConfirmComplianceScheme(viewModel);
        }
        catch (Exception expectedException)
        {
            // Assert
            expectedException.GetType().Should().Be(typeof(NullReferenceException));
            FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme.ComplianceSchemeId.Should()
                .Be(CurrentComplianceScheme.ComplianceSchemeId);
        }
        finally
        {
            SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Never);
        }
    }

    [Test]
    public async Task
        GivenOnConfirmComplianceSchemePage_WhenConfirmComplianceSchemePageHttpPostCalled_WithCache_ThenVerifyCacheUsageAndRedirect()
    {
        // Arrange
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme = null;

        ComplianceSchemeService.Setup(x => x.HasCache()).Returns(true);
        ComplianceSchemeService.Setup(x => x.GetProducerComplianceScheme(_organisationId))
            .ReturnsAsync(new ProducerComplianceSchemeDto());

        ComplianceSchemeService.Setup(x => x.ConfirmAddComplianceScheme(
                SelectedComplianceScheme.Id,
                _organisationId))
            .ReturnsAsync(CommittedSelectedScheme);

        // Act
        var viewModel = new ComplianceSchemeConfirmationViewModel
        {
            CurrentComplianceScheme = null,
            SelectedComplianceScheme = SelectedComplianceScheme,
        };

        var result = await SystemUnderTest.ConfirmComplianceScheme(viewModel);

        // Assert
        result.Should().NotBeNull();
        var checkResult = result as RedirectToActionResult;
        FrontEndSchemeRegistrationSession.Should().NotBeNull();
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme.Should().NotBeNull();
        FrontEndSchemeRegistrationSession.RegistrationSession.CurrentComplianceScheme.ComplianceSchemeId.Should().Be(SelectedComplianceScheme.Id);
        checkResult!.ActionName.Should().Be(nameof(ComplianceSchemeMemberLandingController.Get));
        SessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);

        ComplianceSchemeService.Verify(x => x.HasCache(), Times.Once);
        ComplianceSchemeService.Verify(x => x.GetProducerComplianceScheme(_organisationId), Times.Once);
    }

    [Test]
    public async Task GivenInvalidModelState_WhenConfirmComplianceSchemeHttpPostCalled_ThenReturnConfirmationView()
    {
        // Arrange
        var viewModel = new ComplianceSchemeConfirmationViewModel();
        SystemUnderTest.ModelState.AddModelError("PropertyName", "Error Message");

        // Act
        var result = await SystemUnderTest.ConfirmComplianceScheme(viewModel);

        // Assert
        result.Should().NotBeNull();
        var checkResult = result as ViewResult;
        checkResult.Should().NotBeNull();
        checkResult.ViewName.Should().Be("Confirmation");
    }

    [Test]
    public async Task GivenInvalidModelState_WhenStopComplianceSchemeHttpPostCalled_ThenReturnStopView()
    {
        // Arrange
        SystemUnderTest.ModelState.AddModelError("PropertyName", "Error Message");

        // Act
        var result = await SystemUnderTest.StopComplianceScheme();

        // Assert
        result.Should().NotBeNull();
        var checkResult = result as ViewResult;
        checkResult.Should().NotBeNull();
        checkResult.ViewName.Should().Be("Stop");
    }
}
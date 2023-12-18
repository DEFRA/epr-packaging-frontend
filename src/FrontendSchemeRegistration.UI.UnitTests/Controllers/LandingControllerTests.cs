namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using System.Text.Json;
using Application.DTOs.ComplianceScheme;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.Controllers.FrontendSchemeRegistration;

[TestFixture]
public class LandingControllerTests
{
    private Mock<IComplianceSchemeService> _complianceSchemeServiceMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private LandingController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _complianceSchemeServiceMock = new Mock<IComplianceSchemeService>();
        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _systemUnderTest = new LandingController(_complianceSchemeServiceMock.Object)
        {
            ControllerContext =
            {
                HttpContext = new DefaultHttpContext
                {
                    User = _claimsPrincipalMock.Object
                }
            }
        };
    }

    [Test]
    public async Task Get_RedirectsToComplianceSchemeLandingController_WhenOrganisationRoleIsComplianceScheme()
    {
        // Arrange
        var claims = CreateUserDataClaim(OrganisationRoles.ComplianceScheme);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeLandingController.Get));
        result.ControllerName.Should().Be(nameof(ComplianceSchemeLandingController).RemoveControllerFromName());
    }

    [Test]
    public async Task Get_RedirectsToComplianceSchemeMemberController_WhenOrganisationRoleIsProducerAndProducerIsLinkedWithAComplianceScheme()
    {
        // Arrange
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);
        _complianceSchemeServiceMock.Setup(x => x.GetProducerComplianceScheme(It.IsAny<Guid>())).ReturnsAsync(new ProducerComplianceSchemeDto());

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeMemberLandingController.Get));
        result.ControllerName.Should().Be(nameof(ComplianceSchemeMemberLandingController).RemoveControllerFromName());
    }

    [Test]
    public async Task Get_RedirectsToDirectProducerLandingPage_WhenOrganisationRoleIsProducerAndProducerIsNotLinkedWithAComplianceScheme()
    {
        // Arrange
        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));
        result.ControllerName.Should().Be(nameof(FrontendSchemeRegistrationController).RemoveControllerFromName());
    }

    private static List<Claim> CreateUserDataClaim(string organisationRole)
    {
        var userData = new UserData
        {
            Organisations = new List<Organisation>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrganisationRole = organisationRole
                }
            }
        };

        return new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };
    }
}
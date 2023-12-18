namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using UI.Controllers;

[TestFixture]
public class OrganisationDetailsSubmissionFailedControllerTests
{
    private OrganisationDetailsSubmissionFailedController _controller;

    [SetUp]
    public void SetUp()
    {
        _controller = new OrganisationDetailsSubmissionFailedController();

        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new()
            {
                new()
                {
                    OrganisationRole = OrganisationRoles.ComplianceScheme
                }
            }
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, JsonConvert.SerializeObject(userData))
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        // Set the User property
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Test]
    public async Task Get_ReturnsOrganisationDetailsSubmissionFailedView_WhenCalled()
    {
        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("OrganisationDetailsSubmissionFailed");
    }

    [Test]
    public async Task Post_RedirectsToFileUploadSubLandingGet_WhenCalled()
    {
        // Act
        var result = await _controller.Post();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ControllerName.Should().Be("FileUploadSubLanding");
    }
}
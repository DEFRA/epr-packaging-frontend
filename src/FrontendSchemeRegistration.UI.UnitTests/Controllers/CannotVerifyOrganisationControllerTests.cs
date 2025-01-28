namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using System.Text.Json;
using Application.DTOs.ComplianceScheme;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using UI.Controllers;
using UI.Controllers.ControllerExtensions;
using UI.Controllers.FrontendSchemeRegistration;

[TestFixture]
public class CannotVerifyOrganisationControllerTests
{
    private CannotVerifyOrganisationController _systemUnderTest;
    private Mock<IUrlHelper> _urlHelperMock;

    [SetUp]
    public void SetUp()
    {
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.SubsidiaryCompaniesHouseNumberSearch);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);
        _systemUnderTest = new CannotVerifyOrganisationController();
        _systemUnderTest.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_RedirectsToComplianceSchemeCannotVerifyOrganisationController_WhenOrganisationRoleIsComplianceScheme()
    {
        // Act
        var result = _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("CannotVerifyOrganisation");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.SubsidiaryCompaniesHouseNumberSearch}");
    }
}
namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Controllers;
using UI.Sessions;

[TestFixture]
public class SubsidiaryAddedControllerTests
{
    private const string CompanyName = "Test company";
    private const string CompanySubsidiaryId = "100001";

    private FrontendSchemeRegistrationSession _session;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock = new();
    private SubsidiaryAddedController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _session = new FrontendSchemeRegistrationSession
        {
            SubsidiarySession = new()
            {
                Company = new Company
                {
                    OrganisationId = CompanySubsidiaryId,
                    Name = CompanyName
                }
            }
        };

        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _sessionManagerMock.Setup(m => m.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(_session);
        _sessionManagerMock.Setup(x => x.UpdateSessionAsync(
            It.IsAny<ISession>(), It.IsAny<Action<FrontendSchemeRegistrationSession>>()))
            .Callback<ISession, Action<FrontendSchemeRegistrationSession>>((_, action) => action.Invoke(_session));

        _systemUnderTest = new SubsidiaryAddedController(
            _sessionManagerMock.Object);
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new Mock<HttpContext>().Object
        };
    }

    [Test]
    public async Task Get_SubsidiaryAdded_ReturnsCorrectViewAndModel()
    {
        // Act
        var result = await _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryAdded");
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<SubsidiaryAddedViewModel>();

        var viewModel = (result as ViewResult).Model as SubsidiaryAddedViewModel;
        viewModel.OrganisationName.Should().Be(CompanyName);
        viewModel.OrganisationId.Should().Be(CompanySubsidiaryId);
    }

    [Test]
    public async Task Get_SubsidiaryAdded_With_Null_SubsidiarySession()
    {
        // Arrange
        _session.SubsidiarySession = null;

        // Act
        var result = await _systemUnderTest.Get() as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryCompaniesHouseNumber");
    }

    [Test]
    public async Task Get_SubsidiaryAdded_Calls_UpdateSession()
    {
        // Act
        await _systemUnderTest.Get();

        // Assert
        _sessionManagerMock.Verify(s => s.UpdateSessionAsync(It.IsAny<ISession>(), It.IsAny<Action<FrontendSchemeRegistrationSession>>()), Times.Once);
    }

    [Test]
    public async Task Get_SubsidiaryAdded_Clears_SubsidiarySession()
    {
        // Act
        await _systemUnderTest.Get();

        // Assert
        _session.SubsidiarySession.Should().BeNull();
    }
}

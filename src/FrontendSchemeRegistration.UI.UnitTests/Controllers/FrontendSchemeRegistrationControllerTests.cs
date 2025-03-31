using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class FrontendSchemeRegistrationControllerTests : FrontendSchemeRegistrationTestBase
{
    private const string OrganisationName = "Acme Org Ltd";
    private readonly Guid _organisationId = Guid.NewGuid();

    private UserData _userData;

    [SetUp]
    public void SetUp()
    {
        _userData = GetUserData("Producer");

        SetupBase(_userData);
    }

    private UserData GetUserData(string organisationRole)
    {
        return new UserData
        {
            Id = Guid.NewGuid(),
            Organisations =
            [
                new Organisation
                {
                    Name = OrganisationName,
                    OrganisationNumber = "123456",
                    Id = _organisationId,
                    OrganisationRole = organisationRole
                }
            ]
        };
    }

    [Test]
    public void ApprovedPersonCreated_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var message = "some_new_message";
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = [PagePaths.Root]
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.ApprovedPersonCreated(message).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        FrontEndSchemeRegistrationSession.RegistrationSession.NotificationMessage.Should().Be(message);
    }   
}
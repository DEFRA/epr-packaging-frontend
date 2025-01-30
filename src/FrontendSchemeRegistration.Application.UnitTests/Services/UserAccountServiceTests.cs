namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using System.Net;
using Application.Services;
using Application.Services.Interfaces;
using DTOs.UserAccount;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UI.Extensions;

[TestFixture]
public class UserAccountServiceTests
{
    private Mock<IAccountServiceApiClient> _userAccountServiceApiClientMock;
    private UserAccountService _sut;

    [SetUp]
    public void Init()
    {
        _userAccountServiceApiClientMock = new Mock<IAccountServiceApiClient>();
        _sut = new UserAccountService(_userAccountServiceApiClientMock.Object, new NullLogger<UserAccountService>());
    }

    [Test]
    public async Task GetUserAccount_ReturnsAccount()
    {
        // Arrange
        var userAcc = new UserAccountDto
        {
            User = new User
            {
                Id = Guid.NewGuid(),
                Email = "Email"
            }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = userAcc.ToJsonContent();
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var res = await _sut.GetUserAccount();

        // Assert
        res.Should().BeOfType<UserAccountDto>();
        res.User.Email.Should().Be("Email");
    }

    [Test]
    public async Task GetUserAccount_WhenClientThrowsException_ThrowsException()
    {
        // Arrange
        var userAcc = new UserAccountDto
        {
            User = new User
            {
                Id = Guid.NewGuid(),
                Email = "Email"
            }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = userAcc.ToJsonContent();

        // Act &  Assert
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception());
        Func<Task> act = async () => await _sut.GetUserAccount();
        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task GetUserAccount_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetUserAccount();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetPersonByUserId_ReturnsAccount()
    {
        // Arrange
        var person = new PersonDto
        {
            FirstName = "Test"
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = person.ToJsonContent();
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var res = await _sut.GetPersonByUserId(Guid.NewGuid());

        // Assert
        res.Should().BeOfType<PersonDto>();
        res.FirstName.Should().Be("Test");
    }

    [Test]
    public async Task GetPersonByUserId_WhenClientThrowsException_ThrowsException()
    {
        // Arrange
        var person = new PersonDto
        {
            FirstName = "Test"
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = person.ToJsonContent();

        // Act &  Assert
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception());
        Func<Task> act = async () => await _sut.GetPersonByUserId(Guid.NewGuid());
        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task GetAllPersonByUserId_ReturnsAccount()
    {
        // Arrange
        var person = new PersonDto
        {
            FirstName = "Test"
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = person.ToJsonContent();
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var res = await _sut.GetAllPersonByUserId(Guid.NewGuid());

        // Assert
        res.Should().BeOfType<PersonDto>();
        res.FirstName.Should().Be("Test");
    }

    [Test]
    public async Task GetAllPersonByUserId_WhenClientThrowsException_ThrowsException()
    {
        // Arrange
        var person = new PersonDto
        {
            FirstName = "Test"
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = person.ToJsonContent();

        // Act &  Assert
        _userAccountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception());
        Func<Task> act = async () => await _sut.GetAllPersonByUserId(Guid.NewGuid());
        await act.Should().ThrowAsync<Exception>();
    }
}
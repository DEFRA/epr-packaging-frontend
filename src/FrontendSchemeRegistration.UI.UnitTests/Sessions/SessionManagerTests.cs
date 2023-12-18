namespace FrontendSchemeRegistration.UI.UnitTests.Sessions;

using System.Text;
using System.Text.Json;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using UI.Sessions;

[TestFixture]
public class SessionManagerTests
{
    private const string OrganisationName = "TestCo";
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly string _sessionKey = nameof(FrontendSchemeRegistrationSession);
    private FrontendSchemeRegistrationSession _testSession;

    private string _serializedTestSession;
    private byte[] _sessionBytes;

    private Mock<ISession> _sessionMock;
    private ISessionManager<FrontendSchemeRegistrationSession> _sessionManager;

    [SetUp]
    public void Setup()
    {
        _serializedTestSession = JsonSerializer.Serialize(_testSession);
        _sessionBytes = Encoding.UTF8.GetBytes(_serializedTestSession);

        _sessionMock = new Mock<ISession>();
        _sessionManager = new SessionManager<FrontendSchemeRegistrationSession>();

        _testSession = new()
        {
            UserData = new()
            {
                Organisations = new()
                {
                    new()
                    {
                        Name = OrganisationName,
                        Id = _organisationId
                    }
                }
            }
        };
    }

    [Test]
    public async Task GivenNoSessionInMemory_WhenGetSessionAsyncCalled_ThenSessionReturnedFromSessionStore()
    {
        // Arrange
        _sessionMock.Setup(x => x.TryGetValue(_sessionKey, out _sessionBytes)).Returns(true);

        // Act
        var session = await _sessionManager.GetSessionAsync(_sessionMock.Object);

        // Assert
        _sessionMock.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Once());

        session.UserData.Organisations.FirstOrDefault().Name.Should().Be(_testSession.UserData.Organisations.FirstOrDefault().Name);
    }

    [Test]
    public async Task GivenSessionInMemory_WhenGetSessionAsyncCalled_ThenSessionReturnedFromMemory()
    {
        // Arrange
        _sessionMock.Setup(x => x.Set(_sessionKey, It.IsAny<byte[]>()));
        await _sessionManager.SaveSessionAsync(_sessionMock.Object, _testSession);

        // Act
        var session = await _sessionManager.GetSessionAsync(_sessionMock.Object);

        // Assert
        _sessionMock.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sessionMock.Verify(x => x.TryGetValue(_sessionKey, out It.Ref<byte[]>.IsAny), Times.Never);

        session.UserData.Organisations.FirstOrDefault().Name.Should().Be(_testSession.UserData.Organisations.FirstOrDefault().Name);
    }

    [Test]
    public async Task GivenNewSession_WhenSaveSessionAsyncCalled_ThenSessionSavedInStoreAndMemory()
    {
        // Arrange
        _sessionMock.Setup(x => x.Set(_sessionKey, It.IsAny<byte[]>()));

        // Act
        await _sessionManager.SaveSessionAsync(_sessionMock.Object, _testSession);

        // Assert
        var savedSession = await _sessionManager.GetSessionAsync(_sessionMock.Object);

        _sessionMock.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sessionMock.Verify(x => x.Set(_sessionKey, It.IsAny<byte[]>()), Times.Once);

        savedSession.Should().NotBeNull();
        savedSession.UserData.Organisations.FirstOrDefault().Name.Should().Be(_testSession.UserData.Organisations.FirstOrDefault().Name);
    }

    [Test]
    public async Task GivenSessionKey_WhenRemoveSessionCalled_ThenSessionRemovedFromMemoryAndSessionStore()
    {
        // Arrange
        _sessionMock.Setup(x => x.Set(_sessionKey, It.IsAny<byte[]>()));

        await _sessionManager.SaveSessionAsync(_sessionMock.Object, _testSession);

        // Act
        _sessionManager.RemoveSession(_sessionMock.Object);

        // Assert
        var savedSession = await _sessionManager.GetSessionAsync(_sessionMock.Object);

        _sessionMock.Verify(x => x.Remove(_sessionKey), Times.Once);

        savedSession.Should().BeNull();
    }

    [Test]
    public async Task GivenNoSessionInMemory_WhenUpdateSessionAsyncCalled_ThenSessionHasBeenUpdatedInMemoryAndStore()
    {
        // Act
        await _sessionManager.UpdateSessionAsync(_sessionMock.Object, (x) => x.UserData = _testSession.UserData);

        // Assert
        var savedSession = await _sessionManager.GetSessionAsync(_sessionMock.Object);

        _sessionMock.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _sessionMock.Verify(x => x.Set(_sessionKey, It.IsAny<byte[]>()), Times.Once);

        savedSession.Should().NotBeNull();
        savedSession.UserData.Should().Be(_testSession.UserData);
    }

    [Test]
    public async Task GivenSessionInMemory_WhenUpdateSessionAsyncCalled_ThenSessionHasBeenUpdatedInMemoryAndStore()
    {
        _sessionMock.Setup(x => x.Set(_sessionKey, It.IsAny<byte[]>()));
        await _sessionManager.SaveSessionAsync(_sessionMock.Object, _testSession);

        // Act
        await _sessionManager.UpdateSessionAsync(_sessionMock.Object, (x) => x.UserData.Organisations.FirstOrDefault().Name = OrganisationName);

        // Assert
        var savedSession = await _sessionManager.GetSessionAsync(_sessionMock.Object);

        _sessionMock.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _sessionMock.Verify(x => x.Set(_sessionKey, It.IsAny<byte[]>()), Times.Exactly(2));

        savedSession.Should().NotBeNull();
        savedSession.UserData.Organisations.FirstOrDefault().Name.Should().Be(OrganisationName);
    }
}

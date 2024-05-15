using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewComponents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewComponents
{
    [TestFixture]
    public class ApprovedPerconInvitationSubmittedNotificationTests
    {
        private ApprovedPerconInvitationSubmittedNotification _viewComponent;
        private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
        private Mock<HttpContext> _httpContextMock;
        private Mock<ISession> _sessionMock;

        [SetUp]
        public void SetUp()
        {
            _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
            _httpContextMock = new Mock<HttpContext>();
            _sessionMock = new Mock<ISession>();

            _httpContextMock.SetupGet(c => c.Session).Returns(_sessionMock.Object);

            _viewComponent = new ApprovedPerconInvitationSubmittedNotification(_sessionManagerMock.Object);
            _viewComponent.ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext
                {
                    HttpContext = _httpContextMock.Object
                }
            };
        }

        [Test]
        public async Task InvokeAsync_Returns_ViewComponentResult_With_Message()
        {
            // Arrange
            var session = new FrontendSchemeRegistrationSession { NominatedApprovedPersonSession = new NominatedApprovedPersonSession { IsNominationSubmittedSuccessfully = true } };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(_sessionMock.Object)).ReturnsAsync(session);

            // Act
            var result = await _viewComponent.InvokeAsync();

            // Assert
            var viewResult = result.Should().BeOfType<ViewViewComponentResult>().Subject;
            viewResult.ViewData.Model.Should().Be(true);
        }

        [Test]
        public async Task InvokeAsync_Returns_ViewComponentResult_With_False_Message_When_Session_Is_Null()
        {
            // Arrange
            _sessionManagerMock.Setup(m => m.GetSessionAsync(_sessionMock.Object)).ReturnsAsync((FrontendSchemeRegistrationSession)null);

            // Act
            var result = await _viewComponent.InvokeAsync();

            // Assert
            var viewResult = result.Should().BeOfType<ViewViewComponentResult>().Subject;
            viewResult.ViewData.Model.Should().Be(false);
        }

        [Test]
        public async Task InvokeAsync_Saves_Session_With_IsNominationSubmittedSuccessfully_Set_To_False()
        {
            // Arrange
            var session = new FrontendSchemeRegistrationSession { NominatedApprovedPersonSession = new NominatedApprovedPersonSession { IsNominationSubmittedSuccessfully = true } };
            _sessionManagerMock.Setup(m => m.GetSessionAsync(_sessionMock.Object)).ReturnsAsync(session);

            // Act
            await _viewComponent.InvokeAsync();

            // Assert
            _sessionManagerMock.Verify(m => m.SaveSessionAsync(_sessionMock.Object, It.Is<FrontendSchemeRegistrationSession>(s => s.NominatedApprovedPersonSession.IsNominationSubmittedSuccessfully == false)), Times.Once);
        }
    }
}

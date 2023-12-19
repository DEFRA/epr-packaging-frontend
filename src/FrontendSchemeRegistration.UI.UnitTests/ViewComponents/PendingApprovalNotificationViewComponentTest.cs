using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewComponents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewComponents;

[TestFixture]
public class PendingApprovalNotificationViewComponentTest
{
    [Test]
    public async Task Invoking_Component_Uses_And_Unsets_SessionApprovedPersonNotification()
    {
        // Arrange
        const string message = "some_text";
        var mockSm = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession { NotificationMessage = message }
        };
        var component = new PendingApprovalNotificationViewComponent(mockSm.Object);
        mockSm.Setup(m => m.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(session);
        var mockHc = new Mock<HttpContext>();
        var vcc = new ViewComponentContext
        {
            ViewContext = new ViewContext
            {
                HttpContext = mockHc.Object
            }
        };
        component.ViewComponentContext = vcc;

        // Act
        var result = await component.InvokeAsync();

        // Assert
        (result as ViewViewComponentResult)?.ViewData?.Model.Should().Be(message);
        mockSm.Verify(m => m.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }
}
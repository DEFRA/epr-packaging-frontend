namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Controllers;

[TestFixture]
public class HomeControllerTests
{
    [Test]
    public void SignedOut_Should_Clear_Session_And_Return_ViewResult()
    {
        // Arrange
        var controller = new HomeController();
        var context = new DefaultHttpContext
        {
            Session = new Mock<ISession>().Object
        };
        controller.ControllerContext.HttpContext = context;

        // Act
        var result = controller.SignedOut();

        // Assert
        context.Session.Keys.Should().BeEmpty();
        result.Should().BeOfType<ViewResult>();
    }
}
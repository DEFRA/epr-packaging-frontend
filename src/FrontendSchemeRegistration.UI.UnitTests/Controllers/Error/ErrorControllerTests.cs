namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Error;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UI.Controllers.Error;

[TestFixture]
public class ErrorControllerTests
{
    private Mock<ControllerContext> _controllerContextMock;
    private ErrorController _controller;

    [SetUp]
    public void SetUp()
    {
        _controllerContextMock = new Mock<ControllerContext>();
        _controller = new ErrorController
        {
            ControllerContext = _controllerContextMock.Object
        };
    }

    [Test]
    public void HandleThrownExceptions_ReturnsProblemWithServiceErrorView()
    {
        // Act
        var result = _controller.HandleThrownExceptions();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("ProblemWithServiceError");
    }

    [Test]
    public void HandleThrownSubmissionException_ReturnsProblemWithSubmissionErrorView()
    {
        // Act
        var result = _controller.HandleThrownSubmissionException();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("ProblemWithSubmissionError");
    }

    [Test]
    public void JavaScriptRequired_ReturnsJavaScriptRequiredView()
    {
        // Act
        var result = _controller.JavaScriptRequired();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("JavaScriptRequired");
    }
}
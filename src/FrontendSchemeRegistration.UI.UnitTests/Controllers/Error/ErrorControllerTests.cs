namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Error;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UI.Controllers.Error;

[TestFixture]
public class ErrorControllerTests
{
    private DefaultHttpContext _httpContext;
    private ErrorController _controller;

    [SetUp]
    public void SetUp()
    {
        _httpContext = new DefaultHttpContext();
        _controller = new ErrorController
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    [Test]
    public void HandleThrownExceptions_ReturnsProblemWithServiceErrorView()
    {
        // Arrange
        _httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Act
        var result = _controller.HandleThrownExceptions();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("ProblemWithServiceError");
    }

    [Test]
    public void HandleThrownExceptions_WhenStatusCodeIsNotFound_ReturnsPageNotFoundView()
    {
        // Arrange
        _httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        // Act
        var result = _controller.HandleThrownExceptions();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("PageNotFound");
    }

    [Test]
    public void HandleThrownExceptions_WhenStatusCodeQueryParamIsSpoofed_StillReturnsProblemWithServiceErrorView()
    {
        // Arrange - UseExceptionHandler leaves the original query string intact, so ?statusCode=404
        // on a request that then throws must not be mistaken for a genuine 404.
        _httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        _httpContext.Request.QueryString = new QueryString("?statusCode=404");

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
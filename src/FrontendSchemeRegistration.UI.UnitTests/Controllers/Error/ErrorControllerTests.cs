namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Error;

using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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
    public void HandleThrownExceptions_WhenNothingReExecutedIt_ReturnsProblemWithServiceErrorAsA500()
    {
        // Arrange - a plain GET of /error, which is how the app's own failure redirects arrive
        // (RegistrationApplicationController, SubmissionIdActionFilter, the B2C remote failure hook).
        // The response starts out as 200, which would serve a service failure page as OK.

        // Act
        var result = _controller.HandleThrownExceptions();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("ProblemWithServiceError");
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Test]
    public void HandleThrownExceptions_WhenReExecutedByTheExceptionHandler_KeepsTheStatusCodeItWasGiven()
    {
        // Arrange - UseExceptionHandler has already set the status code, so the action must not overwrite it.
        _httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        _httpContext.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new InvalidOperationException("boom"),
            Path = "/some-page"
        });

        // Act
        var result = _controller.HandleThrownExceptions();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("ProblemWithServiceError");
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Test]
    public void HandleThrownExceptions_WhenReExecutedForANonNotFoundStatus_KeepsTheOriginalStatusCode()
    {
        // Arrange - UseStatusCodePagesWithReExecute preserves the original status on the response.
        _httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
        _httpContext.Features.Set<IStatusCodeReExecuteFeature>(
            new ReExecutedFor(StatusCodes.Status502BadGateway));

        // Act
        var result = _controller.HandleThrownExceptions();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("ProblemWithServiceError");
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Test]
    public void HandleThrownExceptions_WhenReExecutedForANotFound_KeepsThe404AndDoesNotEscalateToA500()
    {
        // Arrange
        _httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        _httpContext.Features.Set<IStatusCodeReExecuteFeature>(
            new ReExecutedFor(StatusCodes.Status404NotFound, "/no-such-page"));

        // Act
        var result = _controller.HandleThrownExceptions();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("PageNotFound");
        _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
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

    /// <summary>
    /// StatusCodeReExecuteFeature exposes OriginalStatusCode as get-only, so the feature the
    /// status code pages middleware would have set has to be stood up by hand.
    /// </summary>
    private sealed class ReExecutedFor(int originalStatusCode, string originalPath = "/some-page")
        : IStatusCodeReExecuteFeature
    {
        public int OriginalStatusCode { get; } = originalStatusCode;

        public string OriginalPath { get; set; } = originalPath;

        public string OriginalPathBase { get; set; } = string.Empty;

        public string? OriginalQueryString { get; set; }

        public Endpoint? Endpoint { get; set; }

        public RouteValueDictionary? RouteValues { get; set; }
    }
}
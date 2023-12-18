namespace FrontendSchemeRegistration.UI.UnitTests.Attributes.ActionFilters;

using Application.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using UI.Attributes.ActionFilters;

[TestFixture]
public class SubmissionIdActionFilterAttributeTests
{
    private Mock<ActionExecutionDelegate> _delegateMock;
    private Mock<HttpContext> _httpContextMock;
    private SubmissionIdActionFilterAttribute _systemUnderTest;
    private ActionExecutingContext _actionExecutingContext;

    [SetUp]
    public void SetUp()
    {
        _delegateMock = new Mock<ActionExecutionDelegate>();
        _httpContextMock = new Mock<HttpContext>();

        _actionExecutingContext = new ActionExecutingContext(
            new ActionContext(
                _httpContextMock.Object,
                Mock.Of<RouteData>(),
                Mock.Of<ActionDescriptor>(),
                Mock.Of<ModelStateDictionary>()),
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            Mock.Of<Controller>());
        _systemUnderTest = new SubmissionIdActionFilterAttribute(PagePaths.FileUpload);
    }

    [Test]
    public async Task OnActionExecutionAsync_CallsNext_WhenSubmissionIdIsPresent()
    {
        // Arrange
        var queryParameters = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "submissionId", Guid.NewGuid().ToString() }
        });
        _httpContextMock.Setup(x => x.Request.Query).Returns(queryParameters);

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _delegateMock.Verify(next => next(), Times.Once);
    }

    [Test]
    public async Task OnActionExecutionAsync_RedirectsToPagePath_WhenSubmissionIdIsNotPresent()
    {
        // Arrange
        var queryParameters = new QueryCollection();
        _httpContextMock.Setup(x => x.Request.Query).Returns(queryParameters);

        // Act
        await _systemUnderTest.OnActionExecutionAsync(_actionExecutingContext, _delegateMock.Object);

        // Assert
        _actionExecutingContext.Result.Should().BeOfType<RedirectResult>().Subject.Url.Should().Be($"~{PagePaths.FileUpload}");
        _delegateMock.Verify(next => next(), Times.Never);
    }
}

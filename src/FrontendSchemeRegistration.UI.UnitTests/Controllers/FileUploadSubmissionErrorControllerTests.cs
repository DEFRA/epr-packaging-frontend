namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using UI.Controllers;
using UI.ViewModels;

[TestFixture]
public class FileUploadSubmissionErrorControllerTests
{
    private const string ViewName = "FileUploadSubmissionError";
    private readonly Guid _submissionId = Guid.NewGuid();
    private FileUploadSubmissionErrorController _systemUnderTest;

    [SetUp]
    public void SetUp()
    {
        _systemUnderTest = new FileUploadSubmissionErrorController();
        _systemUnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "submissionId", _submissionId.ToString() }
                    })
                }
            }
        };
    }

    [Test]
    public void Get_ReturnsCorrectViewAndModel()
    {
        // Arrange / Act
        var result = _systemUnderTest.Get() as ViewResult;

        // Assert
        result.ViewName.Should().Be(ViewName);
        result.Model.Should().BeEquivalentTo(new FileUploadSubmissionErrorViewModel
        {
            SubmissionId = _submissionId
        });
    }
}
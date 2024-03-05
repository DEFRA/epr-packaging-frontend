using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers
{
    public class FileUploadNoSubmissionHistoryControllerTests
    {
        private Mock<ControllerContext> _controllerContext;
        private FileUploadNoSubmissionHistoryController _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _controllerContext = new Mock<ControllerContext>();
            _systemUnderTest = new FileUploadNoSubmissionHistoryController
            {
                ControllerContext = _controllerContext.Object
            };
        }

        [Test]
        public async Task Get_ReturnsNoHistoryPage_WhenCalled()
        {
            // Act
            var result = await _systemUnderTest.Get() as ViewResult;

            // Assert
            result.ViewName.Should().Be("FileUploadNoSubmissionHistory");
        }
    }
}

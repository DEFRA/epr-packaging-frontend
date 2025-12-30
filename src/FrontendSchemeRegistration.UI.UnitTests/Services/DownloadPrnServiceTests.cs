using FluentAssertions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Linq;


namespace FrontendSchemeRegistration.UI.UnitTests.Services
{
    [TestFixture]
    public class DownloadPrnServiceTests
    {
        private Mock<IPrnService> _prnServiceMock;
        private Mock<IViewRenderService> _viewRenderServiceMock;
        private DownloadPrnService _systemUnderTest;
       
        [SetUp]
        public void SetUp()
        {
            _prnServiceMock = new Mock<IPrnService>();
            _viewRenderServiceMock = new Mock<IViewRenderService>();
            _systemUnderTest = new DownloadPrnService(_prnServiceMock.Object, _viewRenderServiceMock.Object);
        }

        [Test]
        public async Task DownloadPrnAsync_ReturnsOkObjectResultWithExpectedData()
        {
            // Arrange
            var id = Guid.NewGuid();
            var viewName = "TestView";
            var actionContext = new ActionContext();

            PrnViewModel prnData = new PrnViewModel { PrnOrPernNumber = "12345" };
            _prnServiceMock.Setup(service => service.GetPrnForPdfByExternalIdAsync(id)).ReturnsAsync(prnData);

            var htmlContent = "<html>Test HTML Content</html>";
            _viewRenderServiceMock.Setup(service => service.RenderViewToStringAsync(actionContext, viewName, prnData))
                .ReturnsAsync(htmlContent);

            // Act
            var result = await _systemUnderTest.DownloadPrnAsync(id, viewName, actionContext);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();

            var resultValue = JObject.FromObject(okResult.Value);
            resultValue["fileName"].ToString().Should().Be("12345");
            resultValue["htmlContent"].ToString().Should().Be(htmlContent);

            _prnServiceMock.Verify(service => service.GetPrnForPdfByExternalIdAsync(id), Times.Once);
            _viewRenderServiceMock.Verify(service => service.RenderViewToStringAsync(actionContext, viewName, prnData), Times.Once);
        }

        [Test]
        public async Task DownloadPrnAsync_WithValidData_ReturnsOkObjectResultWithCorrectJson()
        {
            // Arrange
            var id = Guid.NewGuid();
            var viewName = "PrnPdfView";
            var actionContext = new ActionContext();

            var prnData = new PrnViewModel
            {
                PrnOrPernNumber = "12345"
            };

            var htmlContent = "<html><body>Rendered PRN</body></html>";

            _prnServiceMock
                .Setup(service => service.GetPrnForPdfByExternalIdAsync(id))
                .ReturnsAsync(prnData);

            _viewRenderServiceMock
                .Setup(service => service.RenderViewToStringAsync(actionContext, viewName, prnData))
                .ReturnsAsync(htmlContent);

            // Act
            var result = await _systemUnderTest.DownloadPrnAsync(id, viewName, actionContext);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();

            var jsonResult = JObject.FromObject(okResult.Value);

            jsonResult["fileName"].ToString().Should().Be("12345");
            jsonResult["htmlContent"].ToString().Should().Be(htmlContent);

            // Verify interactions
            _prnServiceMock.Verify(service => service.GetPrnForPdfByExternalIdAsync(id), Times.Once);
            _viewRenderServiceMock.Verify(service => service.RenderViewToStringAsync(actionContext, viewName, prnData), Times.Once);
        }

        [Test]
        public async Task DownloadPrnAsync_WhenPrnServiceReturnsNull_ReturnsBadRequest()
        {
            // Arrange
            var id = Guid.NewGuid();
            var viewName = "PrnPdfView";
            var actionContext = new ActionContext();

            _prnServiceMock
                .Setup(service => service.GetPrnForPdfByExternalIdAsync(id))
                .ReturnsAsync((PrnViewModel)null);

            // Act
            var result = await _systemUnderTest.DownloadPrnAsync(id, viewName, actionContext);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }
    }
}
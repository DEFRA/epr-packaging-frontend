using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns
{
    [TestFixture]
    public class AcceptedPernsOrPrnsControllerTests
    {
        private Mock<IPrnService> _mockPrnService;

        private AcceptedPernsOrPrnsController _sut;

        [SetUp]
        public void SetUp()
        {
            this._mockPrnService = new Mock<IPrnService>();
            _sut = new AcceptedPernsOrPrnsController(_mockPrnService.Object);
        }

        [Test]
        public void Post_RedirectToLandingPage_ifmodelIsNull()
        {
            // Act
            var result = _sut.AcceptedPernsOrPrns((AcceptedPernsOrPrnsViewModel)null) as RedirectToActionResult;

            result.ActionName.Should().Be(nameof(PrnsController.HomePagePrn));
            result.ControllerName.Should().Be("Prns");
        }

        [Test]
        public void Post_RedirectToLandingPage_IfStatusIsNotAccepted()
        {
            var model = new AcceptedPernsOrPrnsViewModel();
            // Act
            var result = _sut.AcceptedPernsOrPrns(model) as RedirectToActionResult;

            result.ActionName.Should().Be(nameof(PrnsController.HomePagePrn));
            result.ControllerName.Should().Be("Prns");
        }

        [Test]
        public void Post_ReturnsCorrectView_IfModelIsNotNullAndStatusIsAccepted()
        {
            var model = new AcceptedPernsOrPrnsViewModel();
            model.Status = "accepted";
            // Act
            var result = _sut.AcceptedPernsOrPrns(model) as ViewResult;

            result.ViewName.Should().Be("Views/Prns/AcceptedPernsOrPrns.cshtml");
        }

        [Test]
        public void Get_ReturnsCorrectView_IfPernOrPrnExists()
        {
            var model = new AcceptedPernsOrPrnsViewModel();
            model.Status = "accepted";
            model.PrnOrPernNumber = "test";

            _mockPrnService.Setup(x => x.GetPrn(It.IsAny<string>())).Returns(model);
            // Act
            var result = _sut.AcceptedPernsOrPrns(model.PrnOrPernNumber) as ViewResult;

            result.ViewName.Should().Be("Views/Prns/AcceptedPernsOrPrns.cshtml");
        }

        [Test]
        public void Get_RedirectToLandingPage_IfPernOrPrnDoesnotExists()
        {
            var model = new AcceptedPernsOrPrnsViewModel();
            // Act
            var result = _sut.AcceptedPernsOrPrns((string)null) as RedirectToActionResult;

            result.ActionName.Should().Be(nameof(PrnsController.HomePagePrn));
            result.ControllerName.Should().Be("Prns");
        }
    }
}

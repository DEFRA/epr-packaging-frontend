using FluentAssertions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.ViewComponents;
using FrontendSchemeRegistration.UI.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewComponents
{
    public class PhaseBannerViewComponentTests
    {
        [Test]
        public void Invoke_ReturnsCorrectModel()
        {
            // Arrange
            const string phaseBanner = "PhaseBanner";

            var options = new PhaseBannerOptions
            {
                ApplicationStatus = "Test",
                SurveyUrl = "test-url",
                Enabled = true
            };

            var systemUnderTest = new PhaseBannerViewComponent(Options.Create(options));

            // Act
            var result = systemUnderTest.Invoke() as ViewViewComponentResult;

            // Assert
            result.ViewData.Model.Should().BeEquivalentTo(new PhaseBannerModel
            {
                Status = $"{phaseBanner}.{options!.ApplicationStatus}",
                Url = options!.SurveyUrl,
                ShowBanner = options.Enabled,
            });
        }
    }
}

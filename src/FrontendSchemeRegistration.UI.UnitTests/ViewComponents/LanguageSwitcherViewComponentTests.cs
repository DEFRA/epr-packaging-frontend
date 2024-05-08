using System.Globalization;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.ViewComponents;
using FrontendSchemeRegistration.UI.ViewModels.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewComponents
{
    public class LanguageSwitcherViewComponentTests
    {
        [Test]
        public async Task Invoke_RendersCorrectView()
        {
            // Arrange
            const bool SHOW_LANGUAGE_SWITCHER = true;
            const string PATH = "/test";
            const string QUERY = "?test=true";
            const string CURRENT_CULTURE = Language.English;

            var options = new RequestLocalizationOptions();
            options.AddSupportedCultures(Language.English, Language.Welsh);

            var featureManagerMock = new Mock<IFeatureManager>();
            featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowLanguageSwitcher))).
                ReturnsAsync(SHOW_LANGUAGE_SWITCHER);

            var systemUnderTest = new LanguageSwitcherViewComponent(Options.Create(options), featureManagerMock.Object);

            var httpContext = new Mock<HttpContext>();
            var httpRequest = new Mock<HttpRequest>();
            httpRequest.Setup(x => x.Path).Returns(PATH);
            httpRequest.Setup(x => x.QueryString).Returns(new QueryString(QUERY));
            httpContext.Setup(x => x.Features.Get<IRequestCultureFeature>())
                .Returns(new RequestCultureFeature(new RequestCulture(CURRENT_CULTURE), null));
            httpContext.Setup(x => x.Request).Returns(httpRequest.Object);
            systemUnderTest.ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = httpContext.Object }
            };

            // Act
            var result = await systemUnderTest.InvokeAsync() as ViewViewComponentResult;

            // Assert
            result.ViewData.Model.Should().BeEquivalentTo(new LanguageSwitcherModel
            {
                SupportedCultures = new List<CultureInfo>
                {
                    new CultureInfo(Language.English),
                    new CultureInfo(Language.Welsh)
                },
                CurrentCulture = new CultureInfo(CURRENT_CULTURE),
                ReturnUrl = $"~{PATH}{QUERY}",
                ShowLanguageSwitcher = SHOW_LANGUAGE_SWITCHER
            });
        }
    }
}

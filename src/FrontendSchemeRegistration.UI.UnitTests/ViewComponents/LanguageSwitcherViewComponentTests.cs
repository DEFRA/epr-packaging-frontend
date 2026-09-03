using System.Globalization;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.ViewComponents;
using FrontendSchemeRegistration.UI.ViewModels.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
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

        [Test]
        public async Task Invoke_WhenReExecutedByStatusCodePages_ReturnsUrlOfTheOriginalRequest()
        {
            // Arrange - UseStatusCodePagesWithReExecute has already rewritten the request to the error
            // handler, so the live path would send the user to /error instead of the page they asked for.
            const string ERROR_PATH = "/error";
            const string ORIGINAL_PATH = "/no-such-page";
            const string ORIGINAL_QUERY = "?year=2026";

            var options = new RequestLocalizationOptions();
            options.AddSupportedCultures(Language.English, Language.Welsh);

            var featureManagerMock = new Mock<IFeatureManager>();
            featureManagerMock.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.ShowLanguageSwitcher)))
                .ReturnsAsync(true);

            var systemUnderTest = new LanguageSwitcherViewComponent(Options.Create(options), featureManagerMock.Object);

            var httpContext = new Mock<HttpContext>();
            var httpRequest = new Mock<HttpRequest>();
            httpRequest.Setup(x => x.Path).Returns(ERROR_PATH);
            httpRequest.Setup(x => x.QueryString).Returns(new QueryString("?statusCode=404"));
            httpContext.Setup(x => x.Request).Returns(httpRequest.Object);
            httpContext.Setup(x => x.Features.Get<IRequestCultureFeature>())
                .Returns(new RequestCultureFeature(new RequestCulture(Language.English), null));
            httpContext.Setup(x => x.Features.Get<IStatusCodeReExecuteFeature>())
                .Returns(new StatusCodeReExecuteFeature
                {
                    OriginalPath = ORIGINAL_PATH,
                    OriginalQueryString = ORIGINAL_QUERY
                });
            systemUnderTest.ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = httpContext.Object }
            };

            // Act
            var result = await systemUnderTest.InvokeAsync() as ViewViewComponentResult;

            // Assert
            var model = result!.ViewData.Model as LanguageSwitcherModel;
            model!.ReturnUrl.Should().Be($"~{ORIGINAL_PATH}{ORIGINAL_QUERY}");
        }
    }
}

using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.Services
{
    [ExcludeFromCodeCoverage]
    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public ViewRenderService(IRazorViewEngine viewEngine, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderViewToStringAsync(ActionContext actionContext, string viewName, object model)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            using (var sw = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(actionContext, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"View '{viewName}' not found.");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                viewDictionary["IsPdf"] = true;

                var tempData = new TempDataDictionary(httpContext, _serviceProvider.GetRequiredService<ITempDataProvider>());

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    tempData,
                    sw,
                    new HtmlHelperOptions());

                await viewResult.View.RenderAsync(viewContext);

                return sw.ToString();
            }
        }
    }
}

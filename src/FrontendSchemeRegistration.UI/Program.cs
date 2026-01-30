using FrontendSchemeRegistration.Application.ConfigurationExtensions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Logging;

using CookieOptions = FrontendSchemeRegistration.Application.Options.CookieOptions;

var builder = WebApplication.CreateBuilder(args);
var isComponentTest = builder.Environment.EnvironmentName.Equals("ComponentTest", StringComparison.OrdinalIgnoreCase);
var services = builder.Services;
var builderConfig = builder.Configuration;
var globalVariables = builderConfig.Get<GlobalVariables>();
string basePath = globalVariables.BasePath;

ThreadPool.SetMinThreads(30, 30);

services.AddFeatureManagement();

//TODO check this as this is the second usage of this, which is causing issues...
// services.AddAntiforgery(opts =>
// {
//     var cookieOptions = builderConfig.GetSection(CookieOptions.ConfigSection).Get<CookieOptions>();
//
//     opts.Cookie.Name = cookieOptions.AntiForgeryCookieName;
//     opts.Cookie.Path = basePath;
// });

var isStubAuth = builderConfig.GetValue<bool>("IsStubAuth", false);
#if !DEBUG
    isStubAuth = false;
#endif

services
    .AddHttpContextAccessor()
    .RegisterWebComponents(builderConfig, builder.Environment, isStubAuth)
    .ConfigureMsalDistributedTokenOptions();

services
    .AddControllersWithViews(options =>
    {
        if (!isComponentTest)
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());    
        }
        
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddCookieTempDataProvider(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

services.AddRazorPages();

services.Configure<ForwardedHeadersOptions>(options =>
{
    var forwardedHeadersOptions = builderConfig.GetSection("ForwardedHeaders").Get<ForwardedHeadersOptions>();

    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
    options.ForwardedHostHeaderName = forwardedHeadersOptions.ForwardedHostHeaderName;
    options.OriginalHostHeaderName = forwardedHeadersOptions.OriginalHostHeaderName;
    options.AllowedHosts = forwardedHeadersOptions.AllowedHosts;
});

builder.Services.Configure<LargeProducerRegistrationWarning>(
    builder.Configuration.GetSection("LargeProducerBanner"));


services.AddHealthChecks();

services.AddApplicationInsightsTelemetry()
        .AddLogging();

services.AddHsts(options =>
{
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

services.AddWebApiGatewayClient();

var app = builder.Build();

app.MapHealthChecks("/admin/health").AllowAnonymous();

app.UsePathBase(basePath);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseForwardedHeaders();

app.UseMiddleware<SecurityHeaderMiddleware>();
app.UseCookiePolicy();
app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseRouting();
app.UseSession();

app.UseAuthorization();
app.UseMiddleware<UserDataCheckerMiddleware>();
app.UseMiddleware<JourneyAccessCheckerMiddleware>();
app.UseMiddleware<AnalyticsCookieMiddleware>();

app.MapControllerRoute("default", "{controller=LandingController}/{action=Get}");

app.MapRazorPages();
app.MapControllers();
app.UseRequestLocalization();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.Run();

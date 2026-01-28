using EPR.Common.Authorization.Extensions;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Attributes.ActionFilters;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Helpers;
using FrontendSchemeRegistration.UI.Middleware;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FeatureManagement;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CookieOptions = FrontendSchemeRegistration.Application.Options.CookieOptions;
using SessionOptions = FrontendSchemeRegistration.Application.Options.SessionOptions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FrontendSchemeRegistration.UI.UnitTests")]
namespace FrontendSchemeRegistration.UI.Extensions;

using Application.Helpers;
using Application.Options.RegistrationPeriodPatterns;
using Polly;
using Services.RegistrationPeriods;
using System.Security.Claims;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web.TokenCacheProviders;
using Services.StubAuthentication;

[ExcludeFromCodeCoverage]
public static class ServiceProviderExtension
{
    public static IServiceCollection RegisterWebComponents(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, bool isStubAuth)
    {
        ConfigureCookiePolicy(services);
        ConfigureOptions(services, configuration);
        ConfigureLocalization(services);
        RegisterServices(services);
        if (!isStubAuth)
        {
            ConfigureAuthentication(services, configuration);    
        }
        ConfigureAuthorization(services, configuration, isStubAuth);
        ConfigureSession(services);
        RegisterServices(services);
        ConfigureTimeProvider(services, hostingEnvironment);
        RegisterAccountAndPaymentApiClients(services);
        RegisterPrnTimeProviderServices(services, configuration);

        return services;
    }

    public static IServiceCollection ConfigureMsalDistributedTokenOptions(this IServiceCollection services)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddApplicationInsights());
        var buildLogger = loggerFactory.CreateLogger<Program>();

        services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
        {
            var msalOptions = services.BuildServiceProvider().GetRequiredService<IOptions<MsalOptions>>().Value;

            options.DisableL1Cache = msalOptions.DisableL1Cache;
            options.SlidingExpiration = TimeSpan.FromMinutes(msalOptions.L2SlidingExpiration);

            options.OnL2CacheFailure = exception =>
            {
                if (exception is RedisConnectionException)
                {
                    buildLogger.LogError(exception, "L2 Cache Failure Redis connection exception: {Message}", exception.Message);
                    return true;
                }

                buildLogger.LogError(exception, "L2 Cache Failure: {Message}", exception.Message);
                return false;
            };
        });

        return services;
    }

    private static void ConfigureAuthorization(IServiceCollection services, IConfiguration configuration, bool isStubAuth)
    {

        if (isStubAuth)
        {
            ConfigureStubAuthentication(services, configuration);    
        }
        else
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
        
                var azureB2COptions = services.BuildServiceProvider().GetRequiredService<IOptions<AzureAdB2COptions>>().Value;
        
                options.LoginPath = azureB2COptions.SignedOutCallbackPath;
                options.AccessDeniedPath = azureB2COptions.SignedOutCallbackPath;
        
                options.SlidingExpiration = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
        }
        
        
        services.AddAntiforgery(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        
        services.AddAuthorization();
        
        services.RegisterPolicy<FrontendSchemeRegistrationSession>(configuration);
        services.AddScoped<ISessionManager<RegistrationApplicationSession>, SessionManager<RegistrationApplicationSession>>();
    }

    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GlobalVariables>(configuration);
        services.Configure<PhaseBannerOptions>(configuration.GetSection(PhaseBannerOptions.Section));
        services.Configure<FrontEndAccountManagementOptions>(configuration.GetSection(FrontEndAccountManagementOptions.ConfigSection));
        services.Configure<FrontEndAccountCreationOptions>(configuration.GetSection(FrontEndAccountCreationOptions.ConfigSection));
        services.Configure<ExternalUrlOptions>(configuration.GetSection(ExternalUrlOptions.ConfigSection));
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.ConfigSection));
        services.Configure<EmailAddressOptions>(configuration.GetSection(EmailAddressOptions.ConfigSection));
        services.Configure<SiteDateOptions>(configuration.GetSection(SiteDateOptions.ConfigSection));
        services.Configure<CookieOptions>(configuration.GetSection(CookieOptions.ConfigSection));
        services.Configure<GoogleAnalyticsOptions>(configuration.GetSection(GoogleAnalyticsOptions.ConfigSection));
        services.Configure<MsalOptions>(configuration.GetSection(MsalOptions.ConfigSection));
        services.Configure<AzureAdB2COptions>(configuration.GetSection(AzureAdB2COptions.ConfigSection));
        services.Configure<HttpClientOptions>(configuration.GetSection(HttpClientOptions.ConfigSection));
        services.Configure<AccountsFacadeApiOptions>(configuration.GetSection(AccountsFacadeApiOptions.ConfigSection));
        services.Configure<PaymentFacadeApiOptions>(configuration.GetSection(PaymentFacadeApiOptions.ConfigSection));
        services.Configure<WebApiOptions>(configuration.GetSection(WebApiOptions.ConfigSection));
        services.Configure<ValidationOptions>(configuration.GetSection(ValidationOptions.ConfigSection));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.ConfigSection));
        services.Configure<ComplianceSchemeMembersPaginationOptions>(configuration.GetSection(ComplianceSchemeMembersPaginationOptions.ConfigSection));
        services.Configure<SessionOptions>(configuration.GetSection(SessionOptions.ConfigSection));

        services.AddSingleton<GuidanceLinkOptions>();
        services.Configure<List<RegistrationPeriodPattern>>(configuration.GetSection(RegistrationPeriodPattern.ConfigSection));
        services.Configure<NotificationBannerOptions>(configuration.GetSection(NotificationBannerOptions.Section));
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ICompaniesHouseService, CompaniesHouseService>();
        services.AddScoped<IComplianceSchemeMemberService, ComplianceSchemeMemberService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IComplianceSchemeService, ComplianceSchemeService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IFileUploadService, FileUploadService>();
        services.AddScoped<IErrorReportService, ErrorReportService>();
        services.AddScoped<ICloner, Cloner>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IRegulatorService, RegulatorService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<ISubsidiaryService, SubsidiaryService>();
        services.AddScoped<IPaymentCalculationService, PaymentCalculationService>();
        services.AddScoped<ISubsidiaryUtilityService, SubsidiaryUtilityService>();
        services.AddScoped<RegistrationApplicationServiceDependencies>(sp => new RegistrationApplicationServiceDependencies
        {
            SubmissionService = sp.GetRequiredService<ISubmissionService>(),
            PaymentCalculationService = sp.GetRequiredService<IPaymentCalculationService>(),
            RegistrationSessionManager = sp.GetRequiredService<ISessionManager<RegistrationApplicationSession>>(),
            FrontendSessionManager = sp.GetRequiredService<ISessionManager<FrontendSchemeRegistrationSession>>(),
            Logger = sp.GetRequiredService<ILogger<RegistrationApplicationService>>(),
            FeatureManager = sp.GetRequiredService<IFeatureManager>(),
            HttpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>(),
            GlobalVariables = sp.GetRequiredService<IOptions<GlobalVariables>>(),
            RegistrationPeriodProvider = sp.GetRequiredService<IRegistrationPeriodProvider>(),
        });
        services.AddScoped<IRegistrationApplicationService, RegistrationApplicationService>();
        services.AddSingleton<IPatchService, PatchService>();
        services.AddAutoMapper((serviceProvider, automapper) =>
        {
            automapper.ConstructServicesUsing(serviceProvider.GetRequiredService);
        }, AppDomain.CurrentDomain.GetAssemblies());
        services.AddTransient<UserDataCheckerMiddleware>();
        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddScoped<IPrnService, PrnService>();
        services.AddScoped<IViewRenderService, ViewRenderService>();
        services.AddScoped<IDownloadPrnService, DownloadPrnService>();
        services.AddScoped<IFileDownloadService, FileDownloadService>();
        services.AddScoped<ComplianceSchemeIdHttpContextFilterAttribute>();
        services.AddScoped<IResubmissionApplicationService, ResubmissionApplicationServices>();
        services.AddSingleton<IRegistrationPeriodProvider, RegistrationPeriodProvider>();
    }

    // When testing PRNs use a configurable date in place of the current date
    private static void RegisterPrnTimeProviderServices(IServiceCollection services, IConfiguration configuration)
    {
        // Check feature flags, [FeatureGate] won't work here
        if (configuration.IsFeatureEnabled(FeatureFlags.ShowPrn) && configuration.IsFeatureEnabled(FeatureFlags.OverridePrnCurrentDateForTestingPurposes))
        {
            var prnOptions = configuration.GetSection("Prn").Get<PrnOptions>();
            var fake = new FakeTimeProvider();
            fake.SetUtcNow(new DateTimeOffset(new DateTime(prnOptions.Year, prnOptions.Month, prnOptions.Day, 0, 0, 0, DateTimeKind.Utc)));
            services.AddSingleton(typeof(TimeProvider), fake);
        }
    }

    private static void RegisterAccountAndPaymentApiClients(IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<HttpClientOptions>>().Value;
        
        services
            .AddHttpClient<IAccountServiceApiClient, AccountServiceApiClient>(client =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<AccountsFacadeApiOptions>>().Value.BaseEndpoint);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
                options.RetryCount, _ => TimeSpan.FromSeconds(options.RetryDelaySeconds)));

        services
            .AddHttpClient<IPaymentCalculationServiceApiClient, PaymentCalculationServiceApiClient>(client =>
            {
                var featureManager = sp.GetRequiredService<IFeatureManager>();
                var baseUrl = sp.GetRequiredService<IOptions<PaymentFacadeApiOptions>>().Value.BaseUrl;
                var useV2 = featureManager.IsEnabledAsync(FeatureFlags.EnableRegistrationFeeV2).GetAwaiter().GetResult();

                if (useV2)
                {
                    baseUrl = Regex.Replace(baseUrl, @"/v1(/|$)", "/v2$1", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
                }

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
                options.RetryCount, _ => TimeSpan.FromSeconds(options.RetryDelaySeconds)));
    }

    private static void ConfigureLocalization(IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources")
            .Configure<RequestLocalizationOptions>(options =>
            {
                var cultureList = new[] { Language.English, Language.Welsh };
                options.SetDefaultCulture(Language.English);
                options.AddSupportedCultures(cultureList);
                options.AddSupportedUICultures(cultureList);
                options.RequestCultureProviders = [new SessionRequestCultureProvider()];
            });
    }

    private static void ConfigureSession(IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var globalVariables = sp.GetRequiredService<IOptions<GlobalVariables>>().Value;

        if (!globalVariables.UseLocalSession)
        {
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var redisConnectionString = redisOptions.ConnectionString;

            services.AddDataProtection()
                .SetApplicationName("EprProducers")
                .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(redisConnectionString), "DataProtection-Keys");

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = redisOptions.InstanceName;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSession(options =>
        {
            var cookieOptions = sp.GetRequiredService<IOptions<CookieOptions>>().Value;
            var sessionOptions = sp.GetRequiredService<IOptions<SessionOptions>>().Value;

            options.Cookie.Name = cookieOptions.SessionCookieName;
            options.IdleTimeout = TimeSpan.FromMinutes(sessionOptions.IdleTimeoutMinutes);
            options.Cookie.IsEssential = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
    }

    // Allows time-travel testing by registering a test TimeProvider. Does not register the test provider
    // in a production environment
    private static void ConfigureTimeProvider(IServiceCollection services, IWebHostEnvironment hostingEnvironment)
    {
        if (hostingEnvironment.IsProduction()) return;
        
        var sp = services.BuildServiceProvider();
        var globalVariables = sp.GetRequiredService<IOptions<GlobalVariables>>().Value;
        
        if (globalVariables.StartupUtcTimestampOverride.HasValue)
        {
            services.AddSingleton(typeof(TimeProvider), _ => new TimeTravelTestingTimeProvider(globalVariables.StartupUtcTimestampOverride.Value));    
        }
    }

    private static void ConfigureStubAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        
        services.AddHttpContextAccessor();
        services.AddTransient<IStubAuthenticationService, StubAuthenticationService>();
        services.AddTransient<ICustomClaims, CustomClaims>();
        
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/services/account-details";
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.Cookie.Name = StubAuthConstants.StubAuthCookieName;
                options.SlidingExpiration = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.Domain = "localhost";
                options.CookieManager = new ChunkingCookieManager { ChunkSize = 3000 };
                options.LogoutPath = "/services/stub-signed-out";
                options.Events.OnSigningOut = c =>
                {
                    c.Response.Cookies.Delete(StubAuthConstants.StubAuthCookieName);
                    c.Response.Redirect("/services/stub-signin");
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToLogin = c =>
                {
                    var redirectUri = new Uri(c.RedirectUri);

                    var redirectQuery = HttpUtility.UrlEncode(
                        $"{redirectUri.Authority}{HttpUtility.UrlDecode(redirectUri.Query.Replace("?ReturnUrl=", ""))}");

                    c.Response.Redirect("/services/account-details?ReturnUrl=" + redirectQuery);

                    return Task.CompletedTask;
                };
            });
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure((options) =>
            {
                options.Events.OnValidatePrincipal = async (ctx) =>
                {
                    var customClaims = ctx.HttpContext.RequestServices.GetRequiredService<ICustomClaims>();
                    var claims = new List<Claim>();
                    claims.AddRange(ctx.Principal.Claims);

                    var additionalClaims = await customClaims.GetCustomClaims(new TokenValidatedContext(
                        ctx.HttpContext,
                        new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, "Cookie",
                            typeof(StubAuthHandler)), new OpenIdConnectOptions(), ctx.Principal,
                        new AuthenticationProperties()));

                    foreach (var additionalClaim in additionalClaims)
                    {
                        if (claims.FirstOrDefault(c => c.Type == additionalClaim.Type) == null)
                        {
                            claims.Add(additionalClaim);
                        }
                    }
                    
                    ctx.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
                    await ctx.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, ctx.Principal);
                };
            });
        
        services.AddScoped<ITokenAcquisition, StubTokenAcquisition>();
        services.AddScoped<IAuthorizationHeaderProvider, StubAuthorizationHeaderProvider>();
        services.AddScoped<ITokenAcquirerFactory, StubTokenAcquirerFactory>();
        services.AddScoped<ITokenAcquirer, StubTokenAcquirer>();
        services.TryAddSingleton<IMsalTokenCacheProvider, StubMsalTokenCacheProvider>();      

        services.ConfigureGraphServiceClient(configuration);
    }

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var sp = services.BuildServiceProvider();
        var cookieOptions = sp.GetRequiredService<IOptions<CookieOptions>>().Value;
        var facadeApiOptions = sp.GetRequiredService<IOptions<AccountsFacadeApiOptions>>().Value;

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(
                options =>
                {
                    var section = configuration.GetSection(AzureAdB2COptions.ConfigSection);
                    section.Bind(options);

                    options.CorrelationCookie.Name = cookieOptions.CorrelationCookieName;

                    options.CorrelationCookie.HttpOnly = true;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

                    options.NonceCookie.Name = cookieOptions.OpenIdCookieName;
                    options.ErrorPath = "/error";
                    options.ClaimActions.Add(new CorrelationClaimAction());

                    options.TokenValidationParameters.ValidateLifetime = section.GetValue("ValidateTokenLifetime", true);
                },
                options =>
                {
                    options.Cookie.Name = cookieOptions.AuthenticationCookieName;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(cookieOptions.AuthenticationExpiryInMinutes);
                    options.SlidingExpiration = true;
                    options.Cookie.Path = "/";

                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
            .EnableTokenAcquisitionToCallDownstreamApi([facadeApiOptions.DownstreamScope])
            .AddDistributedTokenCaches();

        services.ConfigureGraphServiceClient(configuration);
    }

    private static IServiceCollection ConfigureGraphServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.IsFeatureEnabled(FeatureFlags.UseGraphApiForExtendedUserClaims))
        {
            services.RegisterGraphServiceClient(configuration);
        }
        else
        {
            services.RegisterNullGraphServiceClient();
        }

        return services;
    }

    private static void ConfigureCookiePolicy(IServiceCollection services)
    {
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.HttpOnly = HttpOnlyPolicy.Always;
            options.Secure = CookieSecurePolicy.Always;
            options.CheckConsentNeeded = context => true;
        });
    }
}
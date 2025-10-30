﻿using EPR.Common.Authorization.Extensions;
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
using System.Text.RegularExpressions;
using CookieOptions = FrontendSchemeRegistration.Application.Options.CookieOptions;
using SessionOptions = FrontendSchemeRegistration.Application.Options.SessionOptions;

namespace FrontendSchemeRegistration.UI.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceProviderExtension
{
    public static IServiceCollection RegisterWebComponents(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureCookiePolicy(services);
        ConfigureOptions(services, configuration);
        ConfigureLocalization(services);
        ConfigureAuthentication(services, configuration);
        ConfigureAuthorization(services, configuration);
        ConfigureSession(services);
        RegisterServices(services);
        RegisterHttpClients(services);
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

    private static void ConfigureAuthorization(IServiceCollection services, IConfiguration configuration)
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

        services.AddAntiforgery(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

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
        services.Configure<GuidanceLinkOptions>(configuration.GetSection(GuidanceLinkOptions.ConfigSection));
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
            GlobalVariables = sp.GetRequiredService<IOptions<GlobalVariables>>()
        });
        services.AddScoped<IRegistrationApplicationService, RegistrationApplicationService>();
        services.AddTransient<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPatchService, PatchService>();
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        services.AddTransient<UserDataCheckerMiddleware>();
        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddScoped<IPrnService, PrnService>();
        services.AddScoped<IViewRenderService, ViewRenderService>();
        services.AddScoped<IDownloadPrnService, DownloadPrnService>();
        services.AddScoped<IFileDownloadService, FileDownloadService>();
        services.AddScoped<ComplianceSchemeIdHttpContextFilterAttribute>();
        services.AddScoped<IResubmissionApplicationService, ResubmissionApplicationServices>();
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

    private static void RegisterHttpClients(IServiceCollection services)
    {
        services.AddHttpClient<IAccountServiceApiClient, AccountServiceApiClient>((sp, client) =>
        {
            var facadeApiOptions = sp.GetRequiredService<IOptions<AccountsFacadeApiOptions>>().Value;
            var httpClientOptions = sp.GetRequiredService<IOptions<HttpClientOptions>>().Value;

            client.BaseAddress = new Uri(facadeApiOptions.BaseEndpoint);
            client.Timeout = TimeSpan.FromSeconds(httpClientOptions.TimeoutSeconds);
        });

        services.AddHttpClient<IPaymentCalculationServiceApiClient, PaymentCalculationServiceApiClient>((sp, client) =>
        {
            var facadeApiOptions = sp.GetRequiredService<IOptions<PaymentFacadeApiOptions>>().Value;
            var httpClientOptions = sp.GetRequiredService<IOptions<HttpClientOptions>>().Value;
            var featureManager = sp.GetRequiredService<IFeatureManager>();

            var baseUrl = facadeApiOptions.BaseUrl;

            var useV2 = featureManager.IsEnabledAsync(FeatureFlags.EnableRegistrationFeeV2)
                .GetAwaiter().GetResult();

            if (useV2)
            {
                baseUrl = Regex.Replace(baseUrl, @"/v1(/|$)", "/v2$1", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(httpClientOptions.TimeoutSeconds);
        });
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

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var sp = services.BuildServiceProvider();
        var cookieOptions = sp.GetRequiredService<IOptions<CookieOptions>>().Value;
        var facadeApiOptions = sp.GetRequiredService<IOptions<AccountsFacadeApiOptions>>().Value;

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(
                options =>
                {
                    configuration.GetSection(AzureAdB2COptions.ConfigSection).Bind(options);

                    options.CorrelationCookie.Name = cookieOptions.CorrelationCookieName;

                    options.CorrelationCookie.HttpOnly = true;
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

                    options.NonceCookie.Name = cookieOptions.OpenIdCookieName;
                    options.ErrorPath = "/error";
                    options.ClaimActions.Add(new CorrelationClaimAction());
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
        });
    }
}
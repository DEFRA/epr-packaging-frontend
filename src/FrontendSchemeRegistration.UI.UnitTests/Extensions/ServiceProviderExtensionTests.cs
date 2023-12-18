namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Moq;
using UI.Extensions;

[TestFixture]
public class ServiceProviderExtensionTests
{
    private IConfiguration _configuration;

    [SetUp]
    public void SetUp()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {
                    "PhaseBanner", "true"
                },
                {
                    "SubmissionPeriods", "true"
                },
                {
                    "FrontEndAccountManagement", "true"
                },
                {
                    "FrontEndAccountCreation", "true"
                },
                {
                    "ExternalUrls", "true"
                },
                {
                    "Caching", "true"
                },
                {
                    "EmailAddresses", "true"
                },
                {
                    "SiteDates", "true"
                },
                {
                    "Cookie", "true"
                },
                {
                    "GoogleAnalytics", "true"
                },
                {
                    "UseLocalSession", "true"
                }
            })
            .Build();
    }

    [Test]
    public void RegisterWebComponents_Should_Register_Services()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterWebComponents(_configuration);

        // Assert
        services.Count.Should().BeGreaterThan(0);
    }

    [Test]
    public void ConfigureMsalDistributedTokenOptions_RegistersMsalDistributedTokenCacheAdapterOptions()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceCollection>();
        var descriptors = new List<ServiceDescriptor>();
        mockServiceProvider
            .Setup(m => m.Add(It.IsAny<ServiceDescriptor>()))
            .Callback((ServiceDescriptor a) =>
            {
                descriptors.Add(a);
            });

        // Act
        mockServiceProvider.Object.ConfigureMsalDistributedTokenOptions();

        // Assert
        mockServiceProvider.Verify(m => m.Add(It.IsAny<ServiceDescriptor>()), Times.AtLeastOnce);
        descriptors.Any(d => d.ServiceType == typeof(IConfigureOptions<MsalDistributedTokenCacheAdapterOptions>)).Should().BeTrue();
    }
}
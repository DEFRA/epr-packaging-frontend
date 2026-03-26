namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using Application.Extensions;
using Application.Options;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.FeatureManagement;
using Moq;
using UI.Helpers;

[TestFixture]
public class CsocHelperTests
{
    private Mock<IFeatureManager> MockFeatureManager { get; set; } = new();
    
    [Test]
    public async Task CreateViewModel_WhenDisabled_ShouldBeNull()
    {
        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object, 
            isApprovedUser: false, 
            new Organisation(), 
            DateTime.Now,
            new CsocOptions());

        result.Should().BeNull();
    }
    
    [Test]
    public async Task CreateViewModel_WhenEnabled_ShouldNotBeNull()
    {
        MockFeatureManager.Setup(x => x.IsEnabledAsync(FeatureFlags.CsocEnabled)).ReturnsAsync(true);
        var now = DateTime.Now;
        
        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object, 
            isApprovedUser: true, 
            new Organisation(), 
            now,
            new CsocOptions
            {
                UnderstandingObligationsEndpoint = "href"
            });

        result.Should().NotBeNull();
        result?.IsApprovedUser.Should().BeTrue();
        result?.IsComplianceScheme.Should().BeFalse();
        result?.IsDirectProducer.Should().BeFalse();
        result?.SubmissionDeadline.Should().BeAfter(now);
        result?.ComplianceYear.Should().Be(now.GetComplianceYear());
        result?.UnderstandingObligationsEndpoint.Should().Be("href");
    }
}
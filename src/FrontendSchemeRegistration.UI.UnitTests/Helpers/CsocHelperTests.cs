namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using Application.Enums;
using Application.Extensions;
using Application.Options;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.FeatureManagement;
using Moq;
using System;
using UI.Constants;
using UI.Helpers;
using UI.ViewModels.Prns;

[TestFixture]
public class CsocHelperTests
{
    private Mock<IFeatureManager> MockFeatureManager { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        MockFeatureManager = new Mock<IFeatureManager>();
    }
    
    [Test]
    public async Task CreateViewModel_WhenDisabled_ShouldBeNull()
    {
        MockFeatureManager.Setup(x => x.IsEnabledAsync(FeatureFlags.CsocEnabled)).ReturnsAsync(false);

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
        var organisationId = Guid.NewGuid();
        
        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object, 
            isApprovedUser: true, 
            new Organisation
            {
                Id = organisationId,
                OrganisationRole = OrganisationRoles.Producer
            }, 
            now,
            new CsocOptions
            {
                WasteObligationsBaseAddress = "href"
            });

        result.Should().NotBeNull();
        result?.IsApprovedUser.Should().BeTrue();
        result?.IsComplianceScheme.Should().BeFalse();
        result?.IsDirectProducer.Should().BeTrue();
        result?.SubmissionDeadline.Should().BeAfter(now);
        result?.ComplianceYear.Should().Be(now.GetComplianceYear());
        result?.WasteObligationsBaseAddress.Should()
            .Be($"href/compliance/{organisationId}/certificate?year={now.GetComplianceYear()}");
    }
    
    [TestCase(null, false)]
    [TestCase(ObligationStatus.NoDataYet, false)]
    [TestCase(ObligationStatus.Met, true)]
    [TestCase(ObligationStatus.NotMet, true)]
    public async Task CreateViewModel_WhenPrnObligationViewModel_ShouldNotBeNull(ObligationStatus? overallStatus, bool expectedSubmissionState)
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
                WasteObligationsBaseAddress = "href"
            },
            overallStatus is null ? null : new PrnObligationViewModel
            {
                OverallStatus = overallStatus.Value
            });

        result.Should().NotBeNull();
        result?.IsObligationDataSubmitted.Should().Be(expectedSubmissionState);
    }

    [TestCase(ComplianceDeclarationStatus.Submitted)]
    [TestCase(ComplianceDeclarationStatus.Cancelled)]
    public async Task CreateViewModel_WhenDeclarationStatusSet_ShouldMapComplianceDeclarationStatus(
        ComplianceDeclarationStatus complianceDeclarationStatus)
    {
        MockFeatureManager.Setup(x => x.IsEnabledAsync(FeatureFlags.CsocEnabled)).ReturnsAsync(true);

        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object,
            isApprovedUser: true,
            new Organisation(),
            DateTime.Now,
            new CsocOptions(),
            new PrnObligationViewModel
            {
                OverallStatus = ObligationStatus.Met,
                ComplianceDeclarationStatus = complianceDeclarationStatus
            });

        result.Should().NotBeNull();
        result?.ComplianceDeclarationStatus.Should().Be(complianceDeclarationStatus);
    }

    [Test]
    public async Task CreateViewModel_WhenPrnObligationViewModelIsNull_ShouldHaveNullComplianceDeclarationStatus()
    {
        MockFeatureManager.Setup(x => x.IsEnabledAsync(FeatureFlags.CsocEnabled)).ReturnsAsync(true);

        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object,
            isApprovedUser: true,
            new Organisation(),
            DateTime.Now,
            new CsocOptions(),
            null);

        result.Should().NotBeNull();
        result?.ComplianceDeclarationStatus.Should().BeNull();
    }

    [Test]
    public async Task CreateViewModel_WhenOrganisationIsComplianceScheme_ShouldUseStatementWasteObligationsBaseAddress()
    {
        MockFeatureManager.Setup(x => x.IsEnabledAsync(FeatureFlags.CsocEnabled)).ReturnsAsync(true);
        var organisationId = Guid.NewGuid();
        var now = DateTime.Now;

        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object,
            isApprovedUser: true,
            new Organisation
            {
                Id = organisationId,
                OrganisationRole = OrganisationRoles.ComplianceScheme
            },
            now,
            new CsocOptions
            {
                WasteObligationsBaseAddress = "https://understanding-obligations"
            },
            new PrnObligationViewModel
            {
                OverallStatus = ObligationStatus.Met,
                ComplianceDeclarationStatus = ComplianceDeclarationStatus.Cancelled
            });

        result.Should().NotBeNull();
        result?.WasteObligationsBaseAddress.Should()
            .Be($"https://understanding-obligations/compliance/{organisationId}/statement?year={now.GetComplianceYear()}");
    }
}
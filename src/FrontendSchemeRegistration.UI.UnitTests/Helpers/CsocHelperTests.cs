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
using Application.DTOs.ComplianceScheme;
using UI.Helpers;
using UI.Sessions;
using UI.ViewModels.Prns;

[TestFixture]
public class CsocHelperTests
{
    private Mock<IFeatureManager> MockFeatureManager { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        MockFeatureManager = new Mock<IFeatureManager>();
        MockFeatureManager.Setup(x => x.IsEnabledAsync(FeatureFlags.CsocEnabled)).ReturnsAsync(true);
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
    public async Task CreateViewModel_WhenEnabled_AsDirectProducer_ShouldBuildViewModelAndCertificateUrl()
    {
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
                WasteObligationsBaseAddress = "https://understanding-obligations"
            });

        result.Should().NotBeNull();
        result?.IsApprovedUser.Should().BeTrue();
        result?.IsComplianceScheme.Should().BeFalse();
        result?.IsDirectProducer.Should().BeTrue();
        result?.SubmissionDeadline.Should().BeAfter(now);
        result?.ComplianceYear.Should().Be(now.GetComplianceYear());
        result?.WasteObligationsBaseAddress.Should()
            .Be($"https://understanding-obligations/compliance/producer/{organisationId}/certificate?year={now.GetComplianceYear()}");
    }

    [TestCase(null, false)]
    [TestCase(ObligationStatus.NoDataYet, false)]
    [TestCase(ObligationStatus.Met, true)]
    [TestCase(ObligationStatus.NotMet, true)]
    public async Task CreateViewModel_WhenPrnObligationViewModel_ShouldSetObligationSubmittedState(
        ObligationStatus? overallStatus,
        bool expectedSubmissionState)
    {
        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object,
            isApprovedUser: true,
            new Organisation(),
            DateTime.Now,
            new CsocOptions(),
            overallStatus is null ? null : new PrnObligationViewModel
            {
                OverallStatus = overallStatus.Value
            });

        result.Should().NotBeNull();
        result?.IsObligationDataSubmitted.Should().Be(expectedSubmissionState);
    }

    [TestCase(ComplianceDeclarationStatus.Submitted)]
    [TestCase(ComplianceDeclarationStatus.Accepted)]
    [TestCase(ComplianceDeclarationStatus.Cancelled)]
    public async Task CreateViewModel_WhenDeclarationStatusSet_ShouldMapComplianceDeclarationStatus(
        ComplianceDeclarationStatus complianceDeclarationStatus)
    {
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

    [TestCase(ComplianceDeclarationStatus.Cancelled)]
    [TestCase(ComplianceDeclarationStatus.Submitted)]
    [TestCase(ComplianceDeclarationStatus.Accepted)]
    public async Task CreateViewModel_WhenOrganisationIsComplianceScheme_ShouldUseStatementUrlWithYear(
        ComplianceDeclarationStatus status)
    {
        var organisationId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var now = DateTime.Now;
        var session = new RegistrationSession
        {
            SelectedComplianceScheme = new ComplianceSchemeDto
            {
                Id = complianceSchemeId
            }
        };

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
                ComplianceDeclarationStatus = status
            },
            session);

        result.Should().NotBeNull();
        result?.WasteObligationsBaseAddress.Should()
            .Be($"https://understanding-obligations/compliance/cso/{complianceSchemeId}/statement?year={now.GetComplianceYear()}");
    }

    [TestCase(ComplianceDeclarationStatus.Submitted)]
    [TestCase(ComplianceDeclarationStatus.Accepted)]
    public async Task CreateViewModel_WhenOrganisationIsComplianceScheme_AndDeclarationCanBeViewed_ShouldUseStatementDeepLink(
        ComplianceDeclarationStatus status)
    {
        var organisationId = Guid.NewGuid();
        var complianceSchemeId = Guid.NewGuid();
        var complianceDeclarationId = "6830b9d4c7e21f5a8d3e64b2";
        var session = new RegistrationSession
        {
            SelectedComplianceScheme = new ComplianceSchemeDto
            {
                Id = complianceSchemeId
            }
        };

        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object,
            isApprovedUser: true,
            new Organisation
            {
                Id = organisationId,
                OrganisationRole = OrganisationRoles.ComplianceScheme
            },
            DateTime.Now,
            new CsocOptions
            {
                WasteObligationsBaseAddress = "https://understanding-obligations"
            },
            new PrnObligationViewModel
            {
                OverallStatus = ObligationStatus.Met,
                ComplianceDeclarationStatus = status,
                ComplianceDeclarationId = complianceDeclarationId
            },
            session);

        result.Should().NotBeNull();
        result?.WasteObligationsBaseAddress.Should()
            .Be($"https://understanding-obligations/compliance/cso/{complianceSchemeId}/statement/{complianceDeclarationId}");
    }

    [TestCase(ComplianceDeclarationStatus.Submitted)]
    [TestCase(ComplianceDeclarationStatus.Accepted)]
    public async Task CreateViewModel_WhenOrganisationIsDirectProducer_AndDeclarationCanBeViewed_ShouldUseCertificateDeepLink(
        ComplianceDeclarationStatus status)
    {
        var organisationId = Guid.NewGuid();
        var complianceDeclarationId = "6830b9d4c7e21f5a8d3e64b2";

        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object,
            isApprovedUser: true,
            new Organisation
            {
                Id = organisationId,
                OrganisationRole = OrganisationRoles.Producer
            },
            DateTime.Now,
            new CsocOptions
            {
                WasteObligationsBaseAddress = "https://understanding-obligations"
            },
            new PrnObligationViewModel
            {
                OverallStatus = ObligationStatus.Met,
                ComplianceDeclarationStatus = status,
                ComplianceDeclarationId = complianceDeclarationId
            });

        result.Should().NotBeNull();
        result?.WasteObligationsBaseAddress.Should()
            .Be($"https://understanding-obligations/compliance/producer/{organisationId}/certificate/{complianceDeclarationId}");
    }

    [TestCase(ComplianceDeclarationStatus.Submitted)]
    [TestCase(ComplianceDeclarationStatus.Accepted)]
    public async Task CreateViewModel_WhenOrganisationIsDirectProducer_AndDeclarationCanBeViewedWithoutId_ShouldUseCertificateUrlWithYear(
        ComplianceDeclarationStatus status)
    {
        var organisationId = Guid.NewGuid();
        var now = DateTime.Now;

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
                WasteObligationsBaseAddress = "https://understanding-obligations"
            },
            new PrnObligationViewModel
            {
                OverallStatus = ObligationStatus.Met,
                ComplianceDeclarationStatus = status
            });

        result.Should().NotBeNull();
        result?.WasteObligationsBaseAddress.Should()
            .Be($"https://understanding-obligations/compliance/producer/{organisationId}/certificate?year={now.GetComplianceYear()}");
    }

    [Test]
    public async Task CreateViewModel_WhenOrganisationIsNeitherDirectProducerNorComplianceScheme_ShouldUseBaseAddress()
    {
        var organisationId = Guid.NewGuid();

        var result = await CsocHelper.CreateViewModel(
            MockFeatureManager.Object,
            isApprovedUser: true,
            new Organisation
            {
                Id = organisationId
            },
            DateTime.Now,
            new CsocOptions
            {
                WasteObligationsBaseAddress = "https://understanding-obligations"
            });

        result.Should().NotBeNull();
        result?.WasteObligationsBaseAddress.Should().Be("https://understanding-obligations");
    }
}

namespace FrontendSchemeRegistration.UI.Helpers;

using Application.Enums;
using Application.Extensions;
using Application.Options;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using Microsoft.FeatureManagement;
using Sessions;
using ViewModels;
using ViewModels.Prns;

public static class CsocHelper
{
    private const string ProducerCompliancePathPrefix = "/compliance/producer";
    private const string CsoCompliancePathPrefix = "/compliance/cso";

    public static async Task<CsocViewModel?> CreateViewModel(IFeatureManager featureManager,
        bool isApprovedUser,
        Organisation organisation,
        DateTime now,
        CsocOptions options,
        PrnObligationViewModel? prnObligationViewModel = null,
        RegistrationSession? registrationSession = null)
    {
        var enabled = await featureManager.IsEnabledAsync(FeatureFlags.CsocEnabled);
        if (!enabled) return null;

        var complianceYear = now.GetComplianceYear();
        var complianceDeclarationStatus = prnObligationViewModel?.ComplianceDeclarationStatus;

        return new CsocViewModel
        {
            IsApprovedUser = isApprovedUser,
            IsDirectProducer = organisation.IsDirectProducer(),
            IsComplianceScheme = organisation.IsComplianceScheme(),
            SubmissionDeadline = now.GetCsocSubmissionDeadline(),
            ComplianceYear = complianceYear,
            WasteObligationsBaseAddress = GetWasteObligationsBaseAddress(
                options.WasteObligationsBaseAddress,
                organisation.Id,
                organisation.IsComplianceScheme(),
                organisation.IsDirectProducer(),
                complianceYear,
                complianceDeclarationStatus,
                prnObligationViewModel?.ComplianceDeclarationId,
                registrationSession),
            IsObligationDataSubmitted = prnObligationViewModel is not null &&
                                        prnObligationViewModel.OverallStatus != ObligationStatus.NoDataYet,
            ComplianceDeclarationStatus = complianceDeclarationStatus,
            NationId = prnObligationViewModel?.NationId
        };
    }

    private static string? GetWasteObligationsBaseAddress(string? baseEndpoint,
        Guid? organisationId,
        bool isComplianceScheme,
        bool isDirectProducer,
        int complianceYear,
        ComplianceDeclarationStatus? complianceDeclarationStatus,
        string? complianceDeclarationId,
        RegistrationSession? registrationSession)
    {
        if (string.IsNullOrWhiteSpace(baseEndpoint) ||
            !organisationId.HasValue)
        {
            return baseEndpoint;
        }

        string? documentType = null;
        
        if (isComplianceScheme)
        {
            documentType = "statement";
        }
        else if (isDirectProducer)
        {
            documentType = "certificate";
        }

        if (documentType is null)
        {
            return baseEndpoint;
        }

        var normalizedBaseEndpoint = baseEndpoint.TrimEnd('/');
        var canView = complianceDeclarationStatus is ComplianceDeclarationStatus.Submitted
            or ComplianceDeclarationStatus.Accepted;

        if (isDirectProducer)
        {
            if (canView && !string.IsNullOrWhiteSpace(complianceDeclarationId))
            {
                return $"{normalizedBaseEndpoint}{ProducerCompliancePathPrefix}/{organisationId.Value}/certificate/{complianceDeclarationId}";
            }

            return $"{normalizedBaseEndpoint}{ProducerCompliancePathPrefix}/{organisationId.Value}/certificate?year={complianceYear}";
        }

        if (isComplianceScheme)
        {
            var schemeId = registrationSession?.SelectedComplianceScheme?.Id ?? organisationId.Value;

            if (canView && !string.IsNullOrWhiteSpace(complianceDeclarationId))
            {
                return $"{normalizedBaseEndpoint}{CsoCompliancePathPrefix}/{schemeId}/statement/{complianceDeclarationId}";
            }

            return $"{normalizedBaseEndpoint}{CsoCompliancePathPrefix}/{schemeId}/statement?year={complianceYear}";
        }

        return normalizedBaseEndpoint;
    }
}
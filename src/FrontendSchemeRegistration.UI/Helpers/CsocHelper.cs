namespace FrontendSchemeRegistration.UI.Helpers;

using Application.Enums;
using Application.Extensions;
using Application.Options;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using Microsoft.FeatureManagement;
using ViewModels;
using ViewModels.Prns;

public static class CsocHelper
{
    public static async Task<CsocViewModel?> CreateViewModel(
        IFeatureManager featureManager,
        bool isApprovedUser,
        Organisation organisation,
        DateTime now,
        CsocOptions options, 
        PrnObligationViewModel? prnObligationViewModel = null)
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
                complianceYear),
            IsObligationDataSubmitted = prnObligationViewModel is not null &&
                                        prnObligationViewModel.OverallStatus != ObligationStatus.NoDataYet,
            ComplianceDeclarationStatus = complianceDeclarationStatus
        };
    }

    private static string? GetWasteObligationsBaseAddress(
        string? baseEndpoint,
        Guid? organisationId,
        bool isComplianceScheme,
        bool isDirectProducer,
        int complianceYear)
    {
        if (string.IsNullOrWhiteSpace(baseEndpoint) ||
            !organisationId.HasValue)
        {
            return baseEndpoint;
        }

        var documentType = isComplianceScheme
            ? "statement"
            : isDirectProducer
                ? "certificate"
                : null;

        if (documentType is null)
        {
            return baseEndpoint;
        }

        var normalizedBaseEndpoint = baseEndpoint.TrimEnd('/');
        return $"{normalizedBaseEndpoint}/compliance/{organisationId.Value}/{documentType}?year={complianceYear}";
    }
}
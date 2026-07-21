namespace FrontendSchemeRegistration.UI.Helpers;

using Application.Enums;
using Application.Extensions;
using Application.Options;
using Application.Services.Interfaces;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using Microsoft.Extensions.Logging;
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
                organisation,
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

    public static async Task<PrnObligationViewModel?> TryGetCsocObligationViewModelAsync(
        IFeatureManager featureManager,
        IWebApiGatewayClient webApiGatewayClient,
        ILogger logger,
        int complianceYear)
    {
        if (!await featureManager.IsEnabledAsync(FeatureFlags.CsocEnabled))
        {
            return null;
        }

        try
        {
            var declaration = await webApiGatewayClient.GetLatestComplianceDeclaration(complianceYear);
            if (declaration is null)
            {
                return new PrnObligationViewModel();
            }

            return new PrnObligationViewModel
            {
                ComplianceDeclarationStatus = declaration.Status,
                ComplianceDeclarationId = declaration.Id
            };
        }
        catch (HttpRequestException ex)
        {
            return HandleLoadFailure(ex);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return HandleLoadFailure(ex);
        }

        PrnObligationViewModel HandleLoadFailure(Exception ex)
        {
            logger.LogWarning(ex, "Failed to load compliance declaration for year {ComplianceYear}", complianceYear);
            return new PrnObligationViewModel();
        }
    }

    private static string? GetWasteObligationsBaseAddress(
        string? baseEndpoint,
        Organisation organisation,
        int complianceYear,
        ComplianceDeclarationStatus? complianceDeclarationStatus,
        string? complianceDeclarationId,
        RegistrationSession? registrationSession)
    {
        if (string.IsNullOrWhiteSpace(baseEndpoint) ||
            !organisation.Id.HasValue)
        {
            return baseEndpoint;
        }

        var documentType = GetDocumentType(organisation);

        if (documentType is null)
        {
            return baseEndpoint;
        }

        var normalizedBaseEndpoint = baseEndpoint.TrimEnd('/');
        var organisationId = organisation.Id.Value;
        var canView = complianceDeclarationStatus is ComplianceDeclarationStatus.Submitted
            or ComplianceDeclarationStatus.Accepted;

        return documentType switch
        {
            "certificate" when canView && !string.IsNullOrWhiteSpace(complianceDeclarationId) =>
                $"{normalizedBaseEndpoint}{ProducerCompliancePathPrefix}/{organisationId}/certificate/{complianceDeclarationId}",
            "certificate" =>
                $"{normalizedBaseEndpoint}{ProducerCompliancePathPrefix}/{organisationId}/certificate?year={complianceYear}",
            "statement" when canView && !string.IsNullOrWhiteSpace(complianceDeclarationId) =>
                $"{normalizedBaseEndpoint}{CsoCompliancePathPrefix}/{GetSchemeId(organisationId, registrationSession)}/statement/{complianceDeclarationId}",
            "statement" =>
                $"{normalizedBaseEndpoint}{CsoCompliancePathPrefix}/{GetSchemeId(organisationId, registrationSession)}/statement?year={complianceYear}",
            _ => baseEndpoint
        };
    }

    private static string? GetDocumentType(Organisation organisation)
    {
        if (organisation.IsComplianceScheme())
        {
            return "statement";
        }

        if (organisation.IsDirectProducer())
        {
            return "certificate";
        }

        return null;
    }

    private static Guid GetSchemeId(Guid organisationId, RegistrationSession? registrationSession) =>
        registrationSession?.SelectedComplianceScheme?.Id ?? organisationId;

    public static string? GetWasteObligationsClearSessionUrl(string? wasteObligationsBaseAddress)
    {
        var baseAddress = wasteObligationsBaseAddress?.TrimEnd('/');
        return string.IsNullOrEmpty(baseAddress) ? null : $"{baseAddress}/clear-session";
    }

    public static string ResolveSignOutCallbackUrl(
        string signedOutCallbackUrl,
        bool csocEnabled,
        string? wasteObligationsBaseAddress)
    {
        if (!csocEnabled)
        {
            return signedOutCallbackUrl;
        }

        return GetWasteObligationsClearSessionUrl(wasteObligationsBaseAddress) ?? signedOutCallbackUrl;
    }
}

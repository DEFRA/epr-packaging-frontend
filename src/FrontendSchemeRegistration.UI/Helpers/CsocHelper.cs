namespace FrontendSchemeRegistration.UI.Helpers;

using Application.Extensions;
using Application.Options;
using Constants;
using EPR.Common.Authorization.Models;
using Extensions;
using Microsoft.FeatureManagement;
using ViewModels;

public static class CsocHelper
{
    public static async Task<CsocViewModel?> CreateViewModel(
        IFeatureManager featureManager,
        bool isApprovedUser,
        Organisation organisation,
        DateTime now,
        CsocOptions options)
    {
        var enabled = await featureManager.IsEnabledAsync(FeatureFlags.CsocEnabled);
        if (!enabled) return null;

        return new CsocViewModel
        {
            IsApprovedUser = isApprovedUser,
            IsDirectProducer = organisation.IsDirectProducer(),
            IsComplianceScheme = organisation.IsComplianceScheme(),
            SubmissionDeadline = now.GetCsocSubmissionDeadline(),
            ComplianceYear = now.GetComplianceYear(),
            UnderstandingObligationsEndpoint = options.UnderstandingObligationsEndpoint
        };
    }
}
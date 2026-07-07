using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Domain;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;

namespace FrontendSchemeRegistration.UI.Services;

using RegistrationPeriods;

public interface IRegistrationFactory
{
    Task<Registration> CreateAsync(ISession httpSession, Organisation organisation, int registrationYear, RegistrationJourney? registrationJourney, bool? isResubmission = null);
}

public class RegistrationFactory(
    ISubmissionService submissionService,
    IPaymentCalculationService paymentCalculationService,
    ISessionManager<FrontendSchemeRegistrationSession> frontendSessionManager,
    IFeatureManager featureManager,
    IRegistrationPeriodProvider registrationPeriodProvider,
    TimeProvider timeProvider) : IRegistrationFactory
{
    public async Task<Registration> CreateAsync(ISession httpSession, Organisation organisation, int registrationYear, RegistrationJourney? registrationJourney, bool? isResubmission = null)
    {
        var frontEndSession = await frontendSessionManager.GetSessionAsync(httpSession) ?? new FrontendSchemeRegistrationSession();
        var selectedComplianceScheme = frontEndSession.RegistrationSession.SelectedComplianceScheme;

        var details = await submissionService.GetRegistrationApplicationDetails(new GetRegistrationApplicationDetailsRequest
        {
            OrganisationNumber = int.Parse(organisation.OrganisationNumber),
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = selectedComplianceScheme?.Id,
            SubmissionPeriod = $"January to December {registrationYear}",
            RegistrationJourney = registrationJourney?.ToString()
        }) ?? new RegistrationApplicationDetails();

        var feeCalculationDetails = details.RegistrationFeeCalculationDetails;
        if (details.SubmissionId is { } submissionId && submissionId != Guid.Empty
            && await featureManager.IsEnabledAsync(FeatureFlags.EnableRegistrationFeeCalculationViaPaymentService))
        {
            var snapshot = await paymentCalculationService.GetRegistrationFeeCalculationDetails(submissionId);
            if (snapshot is not null)
                feeCalculationDetails = snapshot;
        }

        var isComplianceScheme = selectedComplianceScheme is not null;
        var isSmallProducer = string.Equals(feeCalculationDetails?[0].OrganisationSize, "Small", StringComparison.InvariantCultureIgnoreCase);
        var window = registrationPeriodProvider.GetRegistrationWindow(isComplianceScheme, isSmallProducer, registrationYear)
            ?? throw new InvalidOperationException($"There is no registration window for registration year {registrationYear}, IsComplianceScheme: {isComplianceScheme}, isSmallProducer: {isSmallProducer}");

        return new Registration(
            details,
            feeCalculationDetails,
            selectedComplianceScheme,
            registrationJourney,
            isResubmission,
            organisation.NationId,
            window.RegistrationYear,
            window.DeadlineDate,
            timeProvider.GetLocalNow());
    }
}

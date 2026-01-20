using System.Security.Claims;
using EPR.Common.Authorization.Extensions;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace FrontendSchemeRegistration.UI.Services;

public class ResubmissionApplicationServices(
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    IPaymentCalculationService paymentCalculationService,
    ISubmissionService submissionService,
    IOptions<GlobalVariables> globalVariables,
    IFeatureManager featureManager) : IResubmissionApplicationService
{
    public async Task<string> CreatePomResubmissionReferenceNumberForProducer(FrontendSchemeRegistrationSession session, SubmissionPeriod submissionPeriod, string organisationNumber, string submittedByName, Guid submissionId, int? historyCount)
    {
        var resubmissionCount = historyCount == null ? 0 : historyCount.Value + 1;
        var period = (submissionPeriod.StartMonth == "January" && submissionPeriod.EndMonth == "June") ? 1 : 2;

        var pomResubmissionReferenceNumber = $"PEPR{organisationNumber}{(submissionPeriod.Year[^2..])}{period.ToString("D2")}S{resubmissionCount.ToString("D2")}";

        return await CreateSubmissionEvent(pomResubmissionReferenceNumber, submissionId);
    }

    public async Task<string> CreatePomResubmissionReferenceNumberForCSO(FrontendSchemeRegistrationSession session, SubmissionPeriod submissionPeriod, string organisationNumber, string submittedByName, Guid submissionId, int? historyCount)
    {
        var period = (submissionPeriod.StartMonth == "January" && submissionPeriod.EndMonth == "June") ? 1 : 2;
        var nation = session.PomResubmissionSession.RegulatorNation.Split('-')[1][0];
        var resubmissionCount = historyCount == null ? 0 : historyCount.Value + 1;

        var pomResubmissionReferenceNumber = $"PEPR{organisationNumber}{nation}{(submissionPeriod.Year[^2..])}{period.ToString("D2")}{resubmissionCount.ToString("D2")}";

        return await CreateSubmissionEvent(pomResubmissionReferenceNumber, submissionId);
    }

    public async Task<string> CreatePomResubmissionReferenceNumber(FrontendSchemeRegistrationSession session, string submittedByName, Guid submissionId, int? historyCount)
    {
        var resbumissonSession = session.PomResubmissionSession;
        var organisation = resbumissonSession.PackagingResubmissionApplicationSession.Organisation;
        var submissionPeriod = resbumissonSession.Period;
        var isComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;

        return isComplianceScheme ?
            await CreatePomResubmissionReferenceNumberForCSO(session, submissionPeriod, organisation.OrganisationNumber, submittedByName, submissionId, historyCount)
            : await CreatePomResubmissionReferenceNumberForProducer(session, submissionPeriod, organisation.OrganisationNumber, submittedByName, submissionId, historyCount);
    }

    public async Task<List<PackagingResubmissionApplicationDetails>> GetPackagingDataResubmissionApplicationDetails(
        Organisation organisation,
        List<string> submissionPeriods,
        Guid? complianceSchemeId)
    {
        var packagingResubmissionApplicationDetails = await submissionService.GetPackagingDataResubmissionApplicationDetails(
            new GetPackagingResubmissionApplicationDetailsRequest
            {
                OrganisationNumber = int.Parse(organisation.OrganisationNumber),
                OrganisationId = organisation.Id.Value,
                ComplianceSchemeId = complianceSchemeId,
                SubmissionPeriods = submissionPeriods,
            });

        return packagingResubmissionApplicationDetails ?? new List<PackagingResubmissionApplicationDetails>();
    }

    public async Task<string> InitiatePayment(ClaimsPrincipal user, ISession httpSession)
    {
        var userData = user.GetUserData();
        var organisation = userData.Organisations[0];
        var session = await sessionManager.GetSessionAsync(httpSession);

        var request = new PaymentInitiationRequest
        {
            UserId = userData.Id!.Value,
            OrganisationId = organisation.Id!.Value,
            Reference = session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber,
            Description = "Packaging data resubmission fee",
            Regulator = session.PomResubmissionSession.RegulatorNation,
            Amount = Convert.ToInt32(session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding)
        };

        return await paymentCalculationService.InitiatePayment(request);
    }

    public async Task<PackagingResubmissionMemberDetails?> GetPackagingResubmissionMemberDetails(PackagingResubmissionMemberRequest request)
    {
        var packagingResubmissionMemberDetails = await submissionService.GetPackagingResubmissionMemberDetails(request) ?? new PackagingResubmissionMemberDetails();

        return packagingResubmissionMemberDetails;
    }

    public async Task<PackagingPaymentResponse> GetResubmissionFees(string applicationReferenceNumber, string regulatorNation, int memberCount, bool isComplianceScheme, DateTime? resubmissionDate)
    {
        return await paymentCalculationService.GetResubmissionFees(applicationReferenceNumber, regulatorNation, memberCount, isComplianceScheme, resubmissionDate);
    }

    public async Task<string> GetRegulatorNation(Guid? organisationId)
    {
        return await paymentCalculationService.GetRegulatorNation(organisationId);
    }

    public async Task CreatePackagingResubmissionFeeViewEvent(Guid? submissionId, Guid? filedId)
    {
        await submissionService.CreatePackagingResubmissionFeeViewEvent(submissionId, filedId);
    }

    public async Task CreatePackagingDataResubmissionFeePaymentEvent(Guid? submissionId, Guid? filedId, string paymentMethod)
    {
        await submissionService.CreatePackagingDataResubmissionFeePaymentEvent(submissionId, filedId, paymentMethod);
    }

    public async Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent(Guid? submissionId, Guid? filedId, string submittedBy, DateTime submissionDate, string comment)
    {
        await submissionService.CreatePackagingResubmissionApplicationSubmittedCreatedEvent(submissionId, filedId, submittedBy, submissionDate, comment);
    }

    public async Task<List<PackagingResubmissionApplicationSession>> GetPackagingResubmissionApplicationSession(Organisation organisation, List<string> submissionPeriods, Guid? complianceSchemeId)
    {
        var detailsForAllSubmissionPeriods = await GetPackagingDataResubmissionApplicationDetails(organisation, submissionPeriods, complianceSchemeId);
        var sessionForAllPeriods = FrontendSchemeRegistration.UI.Extensions.PackagingResubmissionApplicationDetailsExtension.ToPackagingResubmissionApplicationSessionList(detailsForAllSubmissionPeriods, organisation);

        return sessionForAllPeriods;
    }

    public async Task<List<SubmissionHistory>> GetSubmissionHistoryAsync(Guid submissionId, DateTime lastSyncTime)
    {
        return await submissionService.GetSubmissionHistoryAsync(submissionId, lastSyncTime);
    }

    public async Task<List<SubmissionPeriodId>> GetSubmissionIdsAsync(Guid organisationId, SubmissionType type, Guid? complianceSchemeId, int? year)
    {
        return await submissionService.GetSubmissionIdsAsync(organisationId, type, complianceSchemeId, year);
    }

    private async Task<string> CreateSubmissionEvent(string pomResubmissionReferenceNumber, Guid submissionId)
    {
        var packagingResubmissionReferenceNumberCreatedEvent = new EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent.PackagingResubmissionReferenceNumberCreatedEvent()
        {
            PackagingResubmissionReferenceNumber = pomResubmissionReferenceNumber
        };

        await submissionService.CreatePackagingResubmissionReferenceNumberEvent(submissionId, packagingResubmissionReferenceNumberCreatedEvent);

        return pomResubmissionReferenceNumber;
    }

    public async Task<SubmissionPeriod?> GetActiveSubmissionPeriod(TimeProvider tp)
    {
        var nowYear = tp.GetLocalNow().Year;
        var currentYear = new[] {nowYear.ToString(), (nowYear + 1).ToString() };
        return globalVariables.Value.SubmissionPeriods.Find(s => currentYear.Contains(s.Year) && s.ActiveFrom.Year == nowYear);
    }

    public async Task<string> GetActualSubmissionPeriod(Guid submissionId, string submissionPeriod)
    {
        return await submissionService.GetActualSubmissionPeriod(submissionId, submissionPeriod);
    }

    public async Task<bool> GetFeatureFlagForProducersFeebreakdown()
    {
        return await featureManager.IsEnabledAsync(nameof(FeatureFlags.IncludeSubsidariesInFeeCalculationsForProducers));
    }

    public SubmissionPeriod PackagingResubmissionPeriod(string[] currentYear, DateTime nowDateTime)
    {
        var packagingResubmissionPeriod = globalVariables.Value.SubmissionPeriods.Find(s => currentYear.Contains(s.Year) && s.ActiveFrom.Year == nowDateTime.Year);
        return packagingResubmissionPeriod;
    }
}
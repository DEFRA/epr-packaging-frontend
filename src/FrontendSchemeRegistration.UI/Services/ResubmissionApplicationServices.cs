using System.Security.Claims;
using EPR.Common.Authorization.Extensions;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;

namespace FrontendSchemeRegistration.UI.Services;

public class ResubmissionApplicationServices(
    ISessionManager<FrontendSchemeRegistrationSession> sessionManager,
    IPaymentCalculationService paymentCalculationService,
    ISubmissionService submissionService) : IResubmissionApplicationService
{
    public async Task<string> CreatePomResubmissionReferenceNumberForProducer(FrontendSchemeRegistrationSession session, SubmissionPeriod submissionPeriod, string organisationNumber, string submittedByName, Guid submissionId)
    {
        var submissions = session.PomResubmissionSession.PomSubmissions;
        var resubmissionCount = submissions.Count(x => x.IsSubmitted) + 1;

        var pomResubmissionReferenceNumber = new KeyValuePair<string, string>(submissionPeriod.DataPeriod, $"PEPR{organisationNumber}{(submissionPeriod.Year[^2..])}S{resubmissionCount.ToString("D2")}");

        return await CreateSubmissionEvent(session, pomResubmissionReferenceNumber, submissionId);
    }

    public async Task<string> CreatePomResubmissionReferenceNumberForCSO(FrontendSchemeRegistrationSession session, SubmissionPeriod submissionPeriod, string organisationNumber, string submittedByName, Guid submissionId)
    {
        var period = (submissionPeriod.StartMonth == "January" && submissionPeriod.EndMonth == "June") ? 1 : 2;
        var csRowNumber = session.RegistrationSession.SelectedComplianceScheme.RowNumber;

        var pomResubmissionReferenceNumber = new KeyValuePair<string, string>(submissionPeriod.DataPeriod, $"PEPR{organisationNumber!}{csRowNumber}{(submissionPeriod.Year[^2..])}S{period.ToString("D2")}");

        return await CreateSubmissionEvent(session, pomResubmissionReferenceNumber, submissionId);
    }

    public async Task<string> CreatePomResubmissionReferenceNumber(FrontendSchemeRegistrationSession session, string submittedByName, Guid submissionId)
    {
        var resbumissonSession = session.PomResubmissionSession;
        var organisation = resbumissonSession.PackagingResubmissionApplicationSession.Organisation;
        var submissionPeriod = resbumissonSession.Period;
        var isComplianceScheme = organisation.OrganisationRole == OrganisationRoles.ComplianceScheme;

        return isComplianceScheme ?
            await CreatePomResubmissionReferenceNumberForCSO(session, submissionPeriod, organisation.OrganisationNumber, submittedByName, submissionId)
            : await CreatePomResubmissionReferenceNumberForProducer(session, submissionPeriod, organisation.OrganisationNumber, submittedByName, submissionId);
    }

    public async Task<PackagingResubmissionApplicationDetails> GetPackagingDataResubmissionApplicationDetails(Organisation organisation, string submissionPeriod, Guid? complianceSchemeId)
    {
        var packagingResubmissionApplicationDetails = await submissionService.GetPackagingDataResubmissionApplicationDetails(new GetPackagingResubmissionApplicationDetailsRequest
        {
            OrganisationNumber = int.Parse(organisation.OrganisationNumber),
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = complianceSchemeId,
            SubmissionPeriod = submissionPeriod,
        }) ?? new PackagingResubmissionApplicationDetails();

        return packagingResubmissionApplicationDetails;
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

    public async Task SubmitAsync(Guid submissionId, Guid fileId, string submittedBy, string? appReferenceNumber = null, bool? isResubmitted = null)
    {
        await submissionService.SubmitAsync(submissionId, fileId, submittedBy, appReferenceNumber, isResubmitted);
    }

    public async Task CreatePackagingResubmissionFeeViewEvent(Guid? submissionId)
    {
        await submissionService.CreatePackagingResubmissionFeeViewEvent(submissionId);
    }

    public async Task CreatePackagingDataResubmissionFeePaymentEvent(Guid? submissionId, Guid? filedId, string paymentMethod)
    {
        await submissionService.CreatePackagingDataResubmissionFeePaymentEvent(submissionId, filedId, paymentMethod);
    }

    public async Task CreatePackagingResubmissionApplicationSubmittedCreatedEvent(Guid? submissionId, Guid? filedId, string submittedBy, DateTime submissionDate, string comment)
    {
        await submissionService.CreatePackagingResubmissionApplicationSubmittedCreatedEvent(submissionId, filedId, submittedBy, submissionDate, comment);
    }

    private async Task<string> CreateSubmissionEvent(FrontendSchemeRegistrationSession session, KeyValuePair<string, string> pomResubmissionReferenceNumber, Guid submissionId)
    {
        session.PomResubmissionSession.PomResubmissionReferences.Add(pomResubmissionReferenceNumber);

        var packagingResubmissionReferenceNumberCreatedEvent = new EPR.SubmissionMicroservice.Data.Entities.SubmissionEvent.PackagingResubmissionReferenceNumberCreatedEvent()
        {
            PackagingResubmissionReferenceNumber = pomResubmissionReferenceNumber.Value
        };

        await submissionService.CreatePackagingResubmissionReferenceNumberEvent(submissionId, packagingResubmissionReferenceNumberCreatedEvent);

        return pomResubmissionReferenceNumber.Value;
    }
}
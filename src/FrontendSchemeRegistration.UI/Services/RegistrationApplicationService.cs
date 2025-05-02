using System.Globalization;
using System.Security.Claims;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using EPR.Common.Authorization.Models;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;
using Microsoft.Extensions.Options;

namespace FrontendSchemeRegistration.UI.Services;

public class RegistrationApplicationService(
    ISubmissionService submissionService,
    IPaymentCalculationService paymentCalculationService,
    ISessionManager<RegistrationApplicationSession> sessionManager,
    ISessionManager<FrontendSchemeRegistrationSession> frontEndSessionManager,
    ILogger<RegistrationApplicationService> logger,
    IOptions<GlobalVariables> globalVariables) : IRegistrationApplicationService
{
    public async Task<RegistrationApplicationSession> GetRegistrationApplicationSession(ISession httpSession, Organisation organisation, bool? isResubmission = null)
    {
        var session = await sessionManager.GetSessionAsync(httpSession) ?? new RegistrationApplicationSession();
        var frontEndSession = await frontEndSessionManager.GetSessionAsync(httpSession) ?? new FrontendSchemeRegistrationSession();

        //this is wrong needs fixing 
        var submissionYear = DateTime.Now.Year;
        session.Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" };
        session.SubmissionPeriod = session.Period.DataPeriod;

        var lateFeeDeadline = globalVariables.Value.LateFeeDeadline != DateTime.MinValue
            ? new DateTime(submissionYear, globalVariables.Value.LateFeeDeadline.Month, globalVariables.Value.LateFeeDeadline.Day, 1, 1, 1, DateTimeKind.Utc)
            : new DateTime(submissionYear, 4, 1, 1, 1, 1, DateTimeKind.Utc);

        var registrationApplicationDetails = await submissionService.GetRegistrationApplicationDetails(new GetRegistrationApplicationDetailsRequest
        {
            OrganisationNumber = int.Parse(organisation.OrganisationNumber),
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = frontEndSession.RegistrationSession.SelectedComplianceScheme?.Id,
            SubmissionPeriod = session.Period.DataPeriod,
            LateFeeDeadline = lateFeeDeadline
        }) ?? new RegistrationApplicationDetails();

        session.SelectedComplianceScheme = frontEndSession.RegistrationSession.SelectedComplianceScheme;

        session.ApplicationStatus = registrationApplicationDetails.ApplicationStatus;
        session.SubmissionId = registrationApplicationDetails.SubmissionId;
        session.IsSubmitted = registrationApplicationDetails.IsSubmitted;
        session.ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber;
        session.RegistrationReferenceNumber = registrationApplicationDetails.RegistrationReferenceNumber;
        session.LastSubmittedFile = registrationApplicationDetails.LastSubmittedFile;
        session.RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod;
        session.RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate;
        session.RegistrationApplicationSubmittedComment = registrationApplicationDetails.RegistrationApplicationSubmittedComment;
        session.RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails;
        session.IsLateFeeApplicable = registrationApplicationDetails.IsLateFeeApplicable;
        session.IsResubmission = (registrationApplicationDetails.IsResubmission ?? isResubmission) ?? false;

        int? nationId;
        if (session.IsComplianceScheme)
            nationId = session.SelectedComplianceScheme.NationId;
        else if (session.FileReachedSynapse)
            nationId = session.RegistrationFeeCalculationDetails[0].NationId;
        else
            nationId = organisation.NationId;

        session.RegulatorNation = nationId switch
        {
            (int) Nation.England => "GB-ENG",
            (int) Nation.Scotland => "GB-SCT",
            (int) Nation.Wales => "GB-WLS",
            (int) Nation.NorthernIreland => "GB-NIR",
            _ => "regulator"
        };

        if (session.ApplicationStatus
                is ApplicationStatusType.AcceptedByRegulator
                or ApplicationStatusType.ApprovedByRegulator
            && isResubmission is true)
        {
            session.IsResubmission = true;
            session.ApplicationStatus = ApplicationStatusType.NotStarted;
        }

        await sessionManager.SaveSessionAsync(httpSession, session);

        if (session.FileUploadStatus is not RegistrationTaskListStatus.Completed
            || session.PaymentViewStatus is RegistrationTaskListStatus.Completed)
            return session;

        bool isOutstandingPaymentAmountZero;
        if (session.IsComplianceScheme)
        {
            var complianceSchemeFeeDetails = await GetComplianceSchemeRegistrationFees(httpSession);
            isOutstandingPaymentAmountZero = complianceSchemeFeeDetails?.TotalAmountOutstanding <= 0;
        }
        else
        {
            var producerFeeDetails = await GetProducerRegistrationFees(httpSession);
            isOutstandingPaymentAmountZero = producerFeeDetails?.TotalAmountOutstanding <= 0;
        }

        if (isOutstandingPaymentAmountZero)
        {
            await CreateRegistrationApplicationEvent(httpSession, null, "No-Outstanding-Payment", SubmissionType.RegistrationFeePayment);
        }

        return session;
    }

    public async Task<FeeCalculationBreakdownViewModel?> GetProducerRegistrationFees(ISession httpSession)
    {
        var session = await sessionManager.GetSessionAsync(httpSession);

        if (!session.FileReachedSynapse)
        {
            logger.LogWarning("Unable to GetProducerRegistrationFees Details, session.FileReachedSynapse is null");
            return null;
        }

        var feeCalculationDetails = session.RegistrationFeeCalculationDetails[0];

        var response = await paymentCalculationService.GetProducerRegistrationFees(new PaymentCalculationRequest
        {
            Regulator = session.RegulatorNation,
            ApplicationReferenceNumber = session.ApplicationReferenceNumber,
            IsLateFeeApplicable = session.IsLateFeeApplicable,
            IsProducerOnlineMarketplace = feeCalculationDetails.IsOnlineMarketplace,
            NoOfSubsidiariesOnlineMarketplace = feeCalculationDetails.NumberOfSubsidiariesBeingOnlineMarketPlace,
            NumberOfSubsidiaries = feeCalculationDetails.NumberOfSubsidiaries,
            ProducerType = feeCalculationDetails.OrganisationSize,
            SubmissionDate = session.LastSubmittedFile.SubmittedDateTime.Value
        });

        if (response is null)
        {
            logger.LogWarning("Unable to GetProducerRegistrationFees Details, paymentCalculationService.GetProducerRegistrationFees is null");
            return null;
        }

        session.TotalAmountOutstanding = response.OutstandingPayment < 0 ? 0 : response.OutstandingPayment;
        await sessionManager.SaveSessionAsync(httpSession, session);

        return new FeeCalculationBreakdownViewModel
        {
            ApplicationStatus = session.ApplicationStatus,
            OrganisationSize = feeCalculationDetails.OrganisationSize,
            IsOnlineMarketplace = feeCalculationDetails.IsOnlineMarketplace,
            NumberOfSubsidiaries = feeCalculationDetails.NumberOfSubsidiaries,
            NumberOfSubsidiariesBeingOnlineMarketplace = feeCalculationDetails.NumberOfSubsidiariesBeingOnlineMarketPlace,
            IsLateFeeApplicable = session.IsLateFeeApplicable,
            BaseFee = response.ProducerRegistrationFee,
            OnlineMarketplaceFee = response.ProducerOnlineMarketPlaceFee,
            TotalSubsidiaryFee = response.SubsidiariesFee - response.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
            TotalSubsidiaryOnlineMarketplaceFee = response.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
            TotalPreviousPayments = response.PreviousPayment,
            TotalFeeAmount = response.TotalFee,
            TotalAmountOutstanding = session.TotalAmountOutstanding,
            IsRegistrationFeePaid = session.IsRegistrationFeePaid,
            ProducerLateRegistrationFee = response.ProducerLateRegistrationFee,
            RegistrationApplicationSubmitted = session.RegistrationApplicationSubmitted
        };
    }

    public async Task<ComplianceSchemeFeeCalculationBreakdownViewModel?> GetComplianceSchemeRegistrationFees(ISession httpSession)
    {
        var session = await sessionManager.GetSessionAsync(httpSession);

        if (!session.FileReachedSynapse)
        {
            logger.LogWarning("Unable to GetComplianceSchemeRegistrationFees Details, session.FileReachedSynapse is null");
            return null;
        }

        var feeCalculationDetails = session.RegistrationFeeCalculationDetails;

        var response = await paymentCalculationService.GetComplianceSchemeRegistrationFees(
            new ComplianceSchemePaymentCalculationRequest
            {
                Regulator = session.RegulatorNation,
                ApplicationReferenceNumber = session.ApplicationReferenceNumber,
                SubmissionDate = session.LastSubmittedFile.SubmittedDateTime.Value,
                ComplianceSchemeMembers = feeCalculationDetails.Select(c => new ComplianceSchemePaymentCalculationRequestMember
                {
                    IsLateFeeApplicable = session.IsLateFeeApplicable && c.IsNewJoiner,
                    IsOnlineMarketplace = c.IsOnlineMarketplace,
                    MemberId = c.OrganisationId,
                    MemberType = c.OrganisationSize,
                    NoOfSubsidiariesOnlineMarketplace = c.NumberOfSubsidiariesBeingOnlineMarketPlace,
                    NumberOfSubsidiaries = c.NumberOfSubsidiaries
                }).ToList()
            });

        if (response is null)
        {
            logger.LogWarning("Unable to GetComplianceSchemeRegistrationFees Details, paymentCalculationService.GetComplianceSchemeRegistrationFees is null");
            return null;
        }

        session.TotalAmountOutstanding = response.OutstandingPayment < 0 ? 0 : response.OutstandingPayment;
        await sessionManager.SaveSessionAsync(httpSession, session);

        var individualProducerData = response.ComplianceSchemeMembersWithFees.GetIndividualProducers(feeCalculationDetails);

        var smallProducersFee = individualProducerData.smallProducers.GetFees();
        var smallProducersCount = individualProducerData.smallProducers.Count;

        var largeProducersFee = individualProducerData.largeProducers.GetFees();
        var largeProducersCount = individualProducerData.largeProducers.Count;

        var onlineMarketplaces = response.ComplianceSchemeMembersWithFees.GetOnlineMarketPlaces();
        var lateProducersFees = response.ComplianceSchemeMembersWithFees.GetLateProducers();

        var subsidiaryCompaniesFees = response.ComplianceSchemeMembersWithFees.GetSubsidiariesCompanies();
        var subsidiaryCompaniesCount = feeCalculationDetails.Sum(dto => dto.NumberOfSubsidiaries);

        return new ComplianceSchemeFeeCalculationBreakdownViewModel
        {
            RegistrationFee = response.ComplianceSchemeRegistrationFee,
            SmallProducersFee = smallProducersFee,
            SmallProducersCount = smallProducersCount,
            LargeProducersFee = largeProducersFee,
            LargeProducersCount = largeProducersCount,
            OnlineMarketplaceFee = onlineMarketplaces.Sum(),
            OnlineMarketplaceCount = onlineMarketplaces.Count,
            SubsidiaryCompanyFee = subsidiaryCompaniesFees.Sum(),
            SubsidiaryCompanyCount = subsidiaryCompaniesCount,
            LateProducerFee = lateProducersFees.Sum(),
            LateProducersCount = lateProducersFees.Count,
            TotalPreviousPayments = response.PreviousPayment,
            TotalFeeAmount = response.TotalFee,
            TotalAmountOutstanding = session.TotalAmountOutstanding,
            IsRegistrationFeePaid = session.IsRegistrationFeePaid,
            RegistrationApplicationSubmitted = session.RegistrationApplicationSubmitted
        };
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
            Reference = session.ApplicationReferenceNumber!,
            Description = "Registration fee",
            Regulator = session.RegulatorNation,
            Amount = session.TotalAmountOutstanding
        };

        return await paymentCalculationService.InitiatePayment(request);
    }

    private async Task<string> CreateApplicationReferenceNumber(ISession httpSession, string organisationNumber)
    {
        var session = await sessionManager.GetSessionAsync(httpSession);
        var referenceNumber = organisationNumber;
        var periodEnd = DateTime.Parse($"30 {session.Period.EndMonth} {session.Period.Year}", new CultureInfo("en-GB"));
        var periodNumber = DateTime.Today <= periodEnd ? 1 : 2;

        if (session.IsComplianceScheme)
        {
            referenceNumber += session.SelectedComplianceScheme.RowNumber.ToString("D3");
        }

        return $"PEPR{referenceNumber}{periodEnd.Year - 2000}P{periodNumber}";
    }

    public async Task CreateRegistrationApplicationEvent(ISession httpSession, string? comments, string? paymentMethod, SubmissionType submissionType)
    {
        var session = await sessionManager.GetSessionAsync(httpSession);

        await submissionService.CreateRegistrationApplicationEvent(
            session.SubmissionId.Value,
            session.SelectedComplianceScheme?.Id,
            comments,
            paymentMethod,
            session.ApplicationReferenceNumber,
            session.IsResubmission,
            submissionType
        );

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            session.RegistrationFeePaymentMethod = paymentMethod;
        }
        else
        {
            session.RegistrationApplicationSubmittedDate = DateTime.Now;
        }

        await sessionManager.SaveSessionAsync(httpSession, session);
    }

    public async Task SetRegistrationFileUploadSession(ISession httpSession, string organisationNumber, bool? isResubmission)
    {
        var frontEndSession = await frontEndSessionManager.GetSessionAsync(httpSession) ?? new FrontendSchemeRegistrationSession();

        //this is wrong needs fixing 
        var submissionYear = DateTime.Now.Year.ToString();
        var period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" };

        frontEndSession.RegistrationSession.SubmissionPeriod = period.DataPeriod;
        frontEndSession.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration = true;
        frontEndSession.RegistrationSession.IsResubmission = isResubmission ?? false;
        frontEndSession.RegistrationSession.ApplicationReferenceNumber = await CreateApplicationReferenceNumber(httpSession, organisationNumber);

        await frontEndSessionManager.SaveSessionAsync(httpSession, frontEndSession);
    }
}

public interface IRegistrationApplicationService
{
    Task<RegistrationApplicationSession> GetRegistrationApplicationSession(ISession httpSession, Organisation organisation, bool? isResubmission = null);

    Task<FeeCalculationBreakdownViewModel?> GetProducerRegistrationFees(ISession httpSession);

    Task<ComplianceSchemeFeeCalculationBreakdownViewModel?> GetComplianceSchemeRegistrationFees(ISession httpSession);

    Task<string> InitiatePayment(ClaimsPrincipal user, ISession httpSession);

    Task CreateRegistrationApplicationEvent(ISession httpSession, string? comments, string? paymentMethod, SubmissionType submissionType);

    Task SetRegistrationFileUploadSession(ISession httpSession, string organisationNumber, bool? isResubmission);
}
﻿using System.Globalization;
using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
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
    private void SetLateFeeFlag(RegistrationApplicationSession session, int registrationYear)
    {
        DateTime lateFeeDeadline;

        if (registrationYear == 2025)
            lateFeeDeadline = globalVariables.Value.LateFeeDeadline2025;
        else if (session.RegistrationFeeCalculationDetails is null)
            lateFeeDeadline = DateTime.MaxValue;
        else if (string.Equals(session.RegistrationFeeCalculationDetails?[0].OrganisationSize, "S", StringComparison.InvariantCultureIgnoreCase))
            lateFeeDeadline = globalVariables.Value.SmallProducerLateFeeDeadline2026;
        else
            lateFeeDeadline = globalVariables.Value.LargeProducerLateFeeDeadline2026;

        session.IsLateFeeApplicable = false;
        session.IsOriginalCsoSubmissionLate = false;

        //CSO logic
        if (session.IsComplianceScheme)
        {
            if (session is { HasAnyApprovedOrQueriedRegulatorDecision: true, IsLatestSubmittedEventAfterFileUpload: true })
            {
                session.IsLateFeeApplicable = session.LatestSubmittedEventCreatedDatetime.Value.Date > lateFeeDeadline;
            }
            else if (session.FirstApplicationSubmittedEventCreatedDatetime is not null)
            {
                session.IsLateFeeApplicable = session.FirstApplicationSubmittedEventCreatedDatetime > lateFeeDeadline;
            }
            else
            {
                session.IsLateFeeApplicable = DateTime.Today > lateFeeDeadline;
            }

            if (session.FirstApplicationSubmittedEventCreatedDatetime is not null)
            {
                session.IsOriginalCsoSubmissionLate = session.FirstApplicationSubmittedEventCreatedDatetime > lateFeeDeadline;
            }
        }
        else
        {
            //Producer Logic
            if (session.FirstApplicationSubmittedEventCreatedDatetime is not null)
            {
                session.IsLateFeeApplicable = session.FirstApplicationSubmittedEventCreatedDatetime > lateFeeDeadline;
            }
            else
            {
                session.IsLateFeeApplicable = DateTime.Today > lateFeeDeadline;
            }
        }
    }

    public async Task<RegistrationApplicationSession> GetRegistrationApplicationSession(ISession httpSession, Organisation organisation, int registrationYear, bool? isResubmission = null)
    {
        var session = await sessionManager.GetSessionAsync(httpSession) ?? new RegistrationApplicationSession();
        var frontEndSession = await frontEndSessionManager.GetSessionAsync(httpSession) ?? new FrontendSchemeRegistrationSession();

        session.Period = new SubmissionPeriod { DataPeriod = $"January to December {registrationYear}", StartMonth = "January", EndMonth = "December", Year = $"{registrationYear}" };
        session.SubmissionPeriod = session.Period.DataPeriod;

        var registrationApplicationDetails = await submissionService.GetRegistrationApplicationDetails(new GetRegistrationApplicationDetailsRequest
        {
            OrganisationNumber = int.Parse(organisation.OrganisationNumber),
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = frontEndSession.RegistrationSession.SelectedComplianceScheme?.Id,
            SubmissionPeriod = session.Period.DataPeriod
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
        session.HasAnyApprovedOrQueriedRegulatorDecision = registrationApplicationDetails.HasAnyApprovedOrQueriedRegulatorDecision;
        session.IsLatestSubmittedEventAfterFileUpload = registrationApplicationDetails.IsLatestSubmittedEventAfterFileUpload;
        session.LatestSubmittedEventCreatedDatetime = registrationApplicationDetails.LatestSubmittedEventCreatedDatetime;
        session.FirstApplicationSubmittedEventCreatedDatetime = registrationApplicationDetails.FirstApplicationSubmittedEventCreatedDatetime;
        session.IsResubmission = (registrationApplicationDetails.IsResubmission ?? isResubmission) ?? false;

        SetLateFeeFlag(session, registrationYear);

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
                    IsLateFeeApplicable = session.IsOriginalCsoSubmissionLate || session is { IsLateFeeApplicable: true, IsResubmission: false } || session.IsLateFeeApplicable && c.IsNewJoiner,
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

    public async Task SetRegistrationFileUploadSession(ISession httpSession, string organisationNumber, int registrationYear, bool? isResubmission)
    {
        var frontEndSession = await frontEndSessionManager.GetSessionAsync(httpSession) ?? new FrontendSchemeRegistrationSession();

        //this is wrong needs fixing 
        var submissionYear = registrationYear;
        var period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" };

        frontEndSession.RegistrationSession.SubmissionPeriod = period.DataPeriod;
        frontEndSession.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration = true;
        frontEndSession.RegistrationSession.IsResubmission = isResubmission ?? false;
        frontEndSession.RegistrationSession.ApplicationReferenceNumber = await CreateApplicationReferenceNumber(httpSession, organisationNumber);

        await frontEndSessionManager.SaveSessionAsync(httpSession, frontEndSession);
    }

    public async Task<List<RegistrationApplicationPerYearViewModel>> BuildRegistrationApplicationPerYearViewModels(ISession httpSession, Organisation organisation)
    {
        var years = globalVariables.Value.RegistrationYear.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(y => int.TryParse(y, out var year) ? year : throw new FormatException($"Invalid year: '{y}'"))
            .OrderByDescending(n => n).ToArray();

        var viewModels = new List<RegistrationApplicationPerYearViewModel>();

        foreach (var year in years)
        {
            var registrationApplicationSession = await GetRegistrationApplicationSession(httpSession, organisation, year);

            viewModels.Add(new RegistrationApplicationPerYearViewModel
            {
                ApplicationStatus = registrationApplicationSession.ApplicationStatus,
                FileUploadStatus = registrationApplicationSession.FileUploadStatus,
                PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
                AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
                ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
                RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
                IsResubmission = registrationApplicationSession.IsResubmission,
                RegistrationYear = year.ToString(),
                IsComplianceScheme = registrationApplicationSession.IsComplianceScheme,
                showLargeProducer = year == 2026,
                RegisterSmallProducersCS = DateTime.UtcNow.Date >= globalVariables.Value.SmallProducersRegStartTime2026
            });
        }

        return viewModels;
    }

    public int? ValidateRegistrationYear(string? registrationYear, bool isParamOptional = false)
    {
        if (string.IsNullOrWhiteSpace(registrationYear) && isParamOptional)
            return null;

        var years = globalVariables.Value.RegistrationYear.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(y => int.TryParse(y, out var year) ? year : throw new FormatException($"Invalid year: '{y}'"))
            .OrderByDescending(n => n).ToArray();

        if (string.IsNullOrWhiteSpace(registrationYear))
            throw new ArgumentException("Registration year missing");

        if (!int.TryParse(registrationYear, out int regYear))
            throw new ArgumentException("Registration year is not a valid number");

        if (!years.Contains(regYear))
            throw new ArgumentException("Invalid registration year");

        return regYear;
    }
}

public interface IRegistrationApplicationService
{
    Task<RegistrationApplicationSession> GetRegistrationApplicationSession(ISession httpSession, Organisation organisation, int registrationYear, bool? isResubmission = null);

    Task<FeeCalculationBreakdownViewModel?> GetProducerRegistrationFees(ISession httpSession);

    Task<ComplianceSchemeFeeCalculationBreakdownViewModel?> GetComplianceSchemeRegistrationFees(ISession httpSession);

    Task<string> InitiatePayment(ClaimsPrincipal user, ISession httpSession);

    Task CreateRegistrationApplicationEvent(ISession httpSession, string? comments, string? paymentMethod, SubmissionType submissionType);

    Task SetRegistrationFileUploadSession(ISession httpSession, string organisationNumber, int registrationYear, bool? isResubmission);

    Task<List<RegistrationApplicationPerYearViewModel>> BuildRegistrationApplicationPerYearViewModels(ISession httpSession, Organisation organisation);

    int? ValidateRegistrationYear(string? registrationYear, bool isParamOptional = false);
}
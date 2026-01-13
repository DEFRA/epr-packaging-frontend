using System.Globalization;
using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace FrontendSchemeRegistration.UI.Services;

using Application.Services;
using RegistrationPeriods;
using ViewModels.Shared;

public class RegistrationApplicationService : IRegistrationApplicationService
{
    private readonly ISubmissionService _submissionService;
    private readonly IPaymentCalculationService paymentCalculationService;
    private readonly ISessionManager<RegistrationApplicationSession> sessionManager;
    private readonly ISessionManager<FrontendSchemeRegistrationSession> frontEndSessionManager;
    private readonly ILogger<RegistrationApplicationService> logger;
    private readonly IFeatureManager featureManager;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IOptions<GlobalVariables> globalVariables;
    private readonly TimeProvider _timeProvider;
    private readonly IRegistrationPeriodProvider _registrationPeriodProvider;

    public RegistrationApplicationService(RegistrationApplicationServiceDependencies dependencies, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        ArgumentNullException.ThrowIfNull(dependencies);
        
        _submissionService = dependencies.SubmissionService
            ?? throw new InvalidOperationException($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(dependencies.SubmissionService)} cannot be null.");
        paymentCalculationService = dependencies.PaymentCalculationService
            ?? throw new InvalidOperationException($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(dependencies.PaymentCalculationService)} cannot be null.");
        sessionManager = dependencies.RegistrationSessionManager
            ?? throw new InvalidOperationException($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(dependencies.RegistrationSessionManager)} cannot be null.");
        frontEndSessionManager = dependencies.FrontendSessionManager
            ?? throw new InvalidOperationException($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(dependencies.FrontendSessionManager)} cannot be null.");
        logger = dependencies.Logger
            ?? throw new InvalidOperationException($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(dependencies.Logger)} cannot be null.");
        featureManager = dependencies.FeatureManager
            ?? throw new InvalidOperationException($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(dependencies.FeatureManager)} cannot be null.");
        httpContextAccessor = dependencies.HttpContextAccessor
            ?? throw new InvalidOperationException($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(dependencies.HttpContextAccessor)} cannot be null.");
        globalVariables = dependencies.GlobalVariables
            ?? throw new InvalidOperationException($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(dependencies.GlobalVariables)} cannot be null.");
        _registrationPeriodProvider = dependencies.RegistrationPeriodProvider;
    }
    private void SetLateFeeFlag(RegistrationApplicationSession session, int registrationYear)
    {
        DateTime lateFeeDeadline;

        if (registrationYear == 2025)
            lateFeeDeadline = globalVariables.Value.LateFeeDeadline2025;
        else if (session.RegistrationFeeCalculationDetails is null)
            lateFeeDeadline = DateTime.MaxValue;
        else if (string.Equals(session.RegistrationFeeCalculationDetails?[0].OrganisationSize, "Small", StringComparison.InvariantCultureIgnoreCase))
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
                var today = _timeProvider.GetLocalNow().Date;
                session.IsLateFeeApplicable = today > lateFeeDeadline;
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
                var today = _timeProvider.GetLocalNow().Date;
                session.IsLateFeeApplicable = today > lateFeeDeadline;
            }
        }
    }

    public async Task<RegistrationApplicationSession> GetRegistrationApplicationSession(ISession httpSession, Organisation organisation, int registrationYear, RegistrationJourney? registrationJourney, bool? isResubmission = null)
    {
        var session = await sessionManager.GetSessionAsync(httpSession) ?? new RegistrationApplicationSession();
        var frontEndSession = await frontEndSessionManager.GetSessionAsync(httpSession) ?? new FrontendSchemeRegistrationSession();

        session.Period = new SubmissionPeriod { DataPeriod = $"January to December {registrationYear}", StartMonth = "January", EndMonth = "December", Year = $"{registrationYear}" };
        session.SubmissionPeriod = session.Period.DataPeriod;

        var registrationApplicationDetails = await _submissionService.GetRegistrationApplicationDetails(new GetRegistrationApplicationDetailsRequest
        {
            OrganisationNumber = int.Parse(organisation.OrganisationNumber),
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = frontEndSession.RegistrationSession.SelectedComplianceScheme?.Id,
            SubmissionPeriod = session.Period.DataPeriod,
            RegistrationJourney = registrationJourney?.ToString()
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
        {
            nationId = session.SelectedComplianceScheme.NationId;
            session.RegistrationJourney = registrationApplicationDetails.RegistrationJourney ?? registrationJourney;
        }
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
        var useV2Flag = await featureManager.IsEnabledAsync(FeatureFlags.EnableRegistrationFeeV2);

        PaymentCalculationResponse? response;

        if (useV2Flag)
        {
            var org = httpContextAccessor.HttpContext?.User.GetUserData()?.Organisations?.FirstOrDefault();
            if (org?.Id is Guid extId &&
                !string.IsNullOrWhiteSpace(org.OrganisationNumber) &&
                int.TryParse(org.OrganisationNumber, out var payerId))
            {
                var v2 = new ProducerPaymentCalculationV2Request
                {
                    FileId = session.LastSubmittedFile.FileId!.Value,
                    ExternalId = extId,
                    InvoicePeriod = GetInvoicePeriodEnd(session),
                    PayerTypeId = 1,
                    PayerId = payerId,

                    Regulator = session.RegulatorNation,
                    ApplicationReferenceNumber = session.ApplicationReferenceNumber,
                    IsLateFeeApplicable = session.IsLateFeeApplicable,
                    IsProducerOnlineMarketplace = feeCalculationDetails.IsOnlineMarketplace,
                    NoOfSubsidiariesOnlineMarketplace = feeCalculationDetails.NumberOfSubsidiariesBeingOnlineMarketPlace,
                    NumberOfSubsidiaries = feeCalculationDetails.NumberOfSubsidiaries,
                    ProducerType = feeCalculationDetails.OrganisationSize,
                    SubmissionDate = session.LastSubmittedFile.SubmittedDateTime!.Value
                };

                response = await paymentCalculationService.GetProducerRegistrationFees(v2);
            }
            else
            {
                var v1 = new PaymentCalculationRequest
                {
                    Regulator = session.RegulatorNation,
                    ApplicationReferenceNumber = session.ApplicationReferenceNumber,
                    IsLateFeeApplicable = session.IsLateFeeApplicable,
                    IsProducerOnlineMarketplace = feeCalculationDetails.IsOnlineMarketplace,
                    NoOfSubsidiariesOnlineMarketplace = feeCalculationDetails.NumberOfSubsidiariesBeingOnlineMarketPlace,
                    NumberOfSubsidiaries = feeCalculationDetails.NumberOfSubsidiaries,
                    ProducerType = feeCalculationDetails.OrganisationSize,
                    SubmissionDate = session.LastSubmittedFile.SubmittedDateTime!.Value
                };

                response = await paymentCalculationService.GetProducerRegistrationFees(v1);
            }
        }
        else
        {
            var v1 = new PaymentCalculationRequest
            {
                Regulator = session.RegulatorNation,
                ApplicationReferenceNumber = session.ApplicationReferenceNumber,
                IsLateFeeApplicable = session.IsLateFeeApplicable,
                IsProducerOnlineMarketplace = feeCalculationDetails.IsOnlineMarketplace,
                NoOfSubsidiariesOnlineMarketplace = feeCalculationDetails.NumberOfSubsidiariesBeingOnlineMarketPlace,
                NumberOfSubsidiaries = feeCalculationDetails.NumberOfSubsidiaries,
                ProducerType = feeCalculationDetails.OrganisationSize,
                SubmissionDate = session.LastSubmittedFile.SubmittedDateTime!.Value
            };

            response = await paymentCalculationService.GetProducerRegistrationFees(v1);
        }

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
        var useV2 = await featureManager.IsEnabledAsync(FeatureFlags.EnableRegistrationFeeV2);

        ComplianceSchemePaymentCalculationResponse? response;

        if (useV2)
        {
            var cs = session.SelectedComplianceScheme!;
           
            var v2 = new ComplianceSchemePaymentCalculationV2Request
            {
                FileId = session.LastSubmittedFile.FileId!.Value,
                ExternalId = cs.Id,
                InvoicePeriod = GetInvoicePeriodEnd(session),
                PayerTypeId = 2,
                PayerId = cs.RowNumber,

                Regulator = session.RegulatorNation,
                ApplicationReferenceNumber = session.ApplicationReferenceNumber,
                SubmissionDate = session.LastSubmittedFile.SubmittedDateTime!.Value,
                ComplianceSchemeMembers = feeCalculationDetails.Select(c => new ComplianceSchemePaymentCalculationRequestMember
                {
                    // Apply late fee to all producers if original submission was late or
                    // not a single submission and current submission is late 
                    // if above two are not satisified that means file are submitted on time its new submission either due to queried
                    // check individual producer is new joiner so late fee applicable on producer level rather than file
                    IsLateFeeApplicable =
                        session.IsOriginalCsoSubmissionLate
                        || (session.FirstApplicationSubmittedEventCreatedDatetime is null && session.IsLateFeeApplicable)
                        || (session.IsLateFeeApplicable && c.IsNewJoiner),
                    IsOnlineMarketplace = c.IsOnlineMarketplace,
                    MemberId = c.OrganisationId,
                    MemberType = c.OrganisationSize,
                    NoOfSubsidiariesOnlineMarketplace = c.NumberOfSubsidiariesBeingOnlineMarketPlace,
                    NumberOfSubsidiaries = c.NumberOfSubsidiaries
                    
                }).ToList()
            };

            response = await paymentCalculationService.GetComplianceSchemeRegistrationFees(v2);
        }
        else
        {
            var complianceSchemeMembers = feeCalculationDetails.Select(c => new ComplianceSchemePaymentCalculationRequestMember
            {
                // Apply late fee to all producers if original submission was late or
                // not a single submission and current submission is late 
                // if above two are not satisified that means file are submitted on time its new submission either due to queried
                // check individual producer is new joiner so late fee applicable on producer level rather than file
                IsLateFeeApplicable =
                    session.IsOriginalCsoSubmissionLate
                    || (session.FirstApplicationSubmittedEventCreatedDatetime is null && session.IsLateFeeApplicable)
                    || (session.IsLateFeeApplicable && c.IsNewJoiner),
                IsOnlineMarketplace = c.IsOnlineMarketplace,
                MemberId = c.OrganisationId,
                MemberType = c.OrganisationSize,
                NoOfSubsidiariesOnlineMarketplace = c.NumberOfSubsidiariesBeingOnlineMarketPlace,
                NumberOfSubsidiaries = c.NumberOfSubsidiaries
            }).ToList();

            // TODO Temporary logic to exclude registration fee for CSO small (2026)
            // - currently a CSO registration with only Small Producer journey will incorrectly not be charged registration fee
            // (it is assumed to have been charged for the Large Producer journey)
            bool includeRegistrationFee = !(session.RegistrationJourney == RegistrationJourney.CsoSmallProducer);

            var v1 = new ComplianceSchemePaymentCalculationRequest
            {
                Regulator = session.RegulatorNation,
                ApplicationReferenceNumber = session.ApplicationReferenceNumber,
                SubmissionDate = session.LastSubmittedFile.SubmittedDateTime!.Value,
                ComplianceSchemeMembers = complianceSchemeMembers,
                IncludeRegistrationFee = includeRegistrationFee
            };

            response = await paymentCalculationService.GetComplianceSchemeRegistrationFees(v1);
        }

        if (response is null)
        {
            logger.LogWarning("Unable to GetComplianceSchemeRegistrationFees Details, paymentCalculationService.GetComplianceSchemeRegistrationFees is null");
            return null;
        }

        session.TotalAmountOutstanding = response.OutstandingPayment < 0 ? 0 : response.OutstandingPayment;
        await sessionManager.SaveSessionAsync(httpSession, session);

        var perProducer = response.ComplianceSchemeMembersWithFees.GetIndividualProducers(feeCalculationDetails);
        var smallFee = perProducer.smallProducers.GetFees();
        var largeFee = perProducer.largeProducers.GetFees();
        var onlineMarketplaces = response.ComplianceSchemeMembersWithFees.GetOnlineMarketPlaces();
        var lateFees = response.ComplianceSchemeMembersWithFees.GetLateProducers();
        var subsidiariesFees = response.ComplianceSchemeMembersWithFees.GetSubsidiariesCompanies();
        var subsidiariesCount = feeCalculationDetails.Sum(d => d.NumberOfSubsidiaries);

        return new ComplianceSchemeFeeCalculationBreakdownViewModel
        {
            RegistrationFee = response.ComplianceSchemeRegistrationFee,
            SmallProducersFee = smallFee,
            SmallProducersCount = perProducer.smallProducers.Count,
            LargeProducersFee = largeFee,
            LargeProducersCount = perProducer.largeProducers.Count,
            OnlineMarketplaceFee = onlineMarketplaces.Sum(),
            OnlineMarketplaceCount = onlineMarketplaces.Count,
            SubsidiaryCompanyFee = subsidiariesFees.Sum(),
            SubsidiaryCompanyCount = subsidiariesCount,
            LateProducerFee = lateFees.Sum(),
            LateProducersCount = lateFees.Count,
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

    public async Task CreateRegistrationApplicationEvent(ISession httpSession, string? comments, string? paymentMethod, SubmissionType submissionType)
    {
        var session = await sessionManager.GetSessionAsync(httpSession);

        var registrationApplicationData = new RegistrationApplicationData(
            session.SubmissionId.Value,
            session.SelectedComplianceScheme?.Id,
            comments,
            paymentMethod
        );
        
        await _submissionService.CreateRegistrationApplicationEvent(
            registrationApplicationData,
            session.ApplicationReferenceNumber,
            session.IsResubmission,
            submissionType, 
            session.RegistrationJourney);

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            session.RegistrationFeePaymentMethod = paymentMethod;
        }
        else
        {
            session.RegistrationApplicationSubmittedDate = _timeProvider.GetLocalNow().DateTime;
        }

        await sessionManager.SaveSessionAsync(httpSession, session);
    }

    public async Task SetRegistrationFileUploadSession(ISession httpSession, string organisationNumber, int registrationYear, bool? isResubmission)
    {
        var registrationSessionTask = sessionManager.GetSessionAsync(httpSession);
        var frontEndSession = await frontEndSessionManager.GetSessionAsync(httpSession) ?? new FrontendSchemeRegistrationSession();

        //this is wrong needs fixing 
        var submissionYear = registrationYear;
        var period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" };

        frontEndSession.RegistrationSession.SubmissionPeriod = period.DataPeriod;
        frontEndSession.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration = true;
        frontEndSession.RegistrationSession.IsResubmission = isResubmission ?? false;
        
        var registrationSession = await registrationSessionTask;
        
        frontEndSession.RegistrationSession.ApplicationReferenceNumber = ReferenceNumberBuilder.Build(
            registrationSession.Period,
            organisationNumber,
            _timeProvider,
            registrationSession.IsComplianceScheme,
            registrationSession.SelectedComplianceScheme?.RowNumber ?? 0,
            registrationSession.RegistrationJourney?.ToString() ?? "X"); 
        
        await frontEndSessionManager.SaveSessionAsync(httpSession, frontEndSession);
    }

    public async Task<List<RegistrationApplicationViewModel>> BuildRegistrationApplicationPerYearViewModels(ISession httpSession, Organisation organisation)
    {
        var years = globalVariables.Value.RegistrationYear.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(y => int.TryParse(y, out var year) ? year : throw new FormatException($"Invalid year: '{y}'"))
            .OrderByDescending(n => n).ToArray();

        var viewModels = new List<RegistrationApplicationViewModel>();

        foreach (var year in years)
        {
            var registrationApplicationSession = await GetRegistrationApplicationSession(httpSession, organisation, year, null);

            viewModels.Add(new RegistrationApplicationViewModel
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
                RegisterSmallProducersCS = _timeProvider.GetUtcNow().Date >= globalVariables.Value.SmallProducersRegStartTime2026,
                DeadlineDate = registrationApplicationSession.IsComplianceScheme ? globalVariables.Value.LargeProducerLateFeeDeadline2026 : DateTime.MinValue   // this is only displayed for CSOs 
            });
        }

        return viewModels;
    }

    public async Task<List<RegistrationYearApplicationsViewModel>> BuildRegistrationYearApplicationsViewModels(ISession httpSession, Organisation organisation)
    {
        var applications = new List<RegistrationApplicationViewModel>();

        var windows = _registrationPeriodProvider.GetRegistrationWindows(organisation.OrganisationRole == OrganisationRoles.ComplianceScheme).OrderByDescending(ra => ra.RegistrationYear);

        foreach (var window in windows)
        {
            var registrationApplicationSession = await GetRegistrationApplicationSession(httpSession, organisation, window.RegistrationYear, window.Journey);

            applications.Add(new RegistrationApplicationViewModel
            {
                ApplicationStatus = registrationApplicationSession.ApplicationStatus,
                FileUploadStatus = registrationApplicationSession.FileUploadStatus,
                PaymentViewStatus = registrationApplicationSession.PaymentViewStatus,
                AdditionalDetailsStatus = registrationApplicationSession.AdditionalDetailsStatus,
                ApplicationReferenceNumber = registrationApplicationSession.ApplicationReferenceNumber,
                RegistrationReferenceNumber = registrationApplicationSession.RegistrationReferenceNumber,
                IsResubmission = registrationApplicationSession.IsResubmission,
                RegistrationYear = window.RegistrationYear.ToString(),
                IsComplianceScheme = registrationApplicationSession.IsComplianceScheme,
                showLargeProducer = true,   // we can get rid of this once we delete the old configuration and the BuildRegistrationApplicationPerYearViewModels method
                RegisterSmallProducersCS = _timeProvider.GetUtcNow().Date >= globalVariables.Value.SmallProducersRegStartTime2026,
                RegistrationJourney = window.Journey,
                DeadlineDate = window.DeadlineDate
            });
        }

        return applications.GroupBy(a => a.RegistrationYear).Select(a => new RegistrationYearApplicationsViewModel(int.Parse(a.Key), a)).OrderByDescending(a => a.Year).ToList();
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

    private static DateTimeOffset GetInvoicePeriodEnd(dynamic session)
    {
        var periodEnd = DateTime.Parse($"30 {session.Period.EndMonth} {session.Period.Year}", new CultureInfo("en-GB"));
        return new DateTimeOffset(periodEnd, TimeSpan.Zero);
    }
}

public interface IRegistrationApplicationService
{
    Task<RegistrationApplicationSession> GetRegistrationApplicationSession(ISession httpSession, Organisation organisation, int registrationYear, RegistrationJourney? registrationJourney, bool? isResubmission = null);

    Task<FeeCalculationBreakdownViewModel?> GetProducerRegistrationFees(ISession httpSession);

    Task<ComplianceSchemeFeeCalculationBreakdownViewModel?> GetComplianceSchemeRegistrationFees(ISession httpSession);

    Task<string> InitiatePayment(ClaimsPrincipal user, ISession httpSession);

    Task CreateRegistrationApplicationEvent(ISession httpSession, string? comments, string? paymentMethod, SubmissionType submissionType);

    Task SetRegistrationFileUploadSession(ISession httpSession, string organisationNumber, int registrationYear, bool? isResubmission);

    Task<List<RegistrationApplicationViewModel>> BuildRegistrationApplicationPerYearViewModels(ISession httpSession, Organisation organisation);

    Task<List<RegistrationYearApplicationsViewModel>> BuildRegistrationYearApplicationsViewModels(
        ISession httpSession, Organisation organisation);
    
    int? ValidateRegistrationYear(string? registrationYear, bool isParamOptional = false);
}


public sealed class RegistrationApplicationServiceDependencies
{
    public required ISubmissionService SubmissionService { get; init; }
    public required IPaymentCalculationService PaymentCalculationService { get; init; }
    public required ISessionManager<RegistrationApplicationSession> RegistrationSessionManager { get; init; }
    public required ISessionManager<FrontendSchemeRegistrationSession> FrontendSessionManager { get; init; }
    public required ILogger<RegistrationApplicationService> Logger { get; init; }
    public required IFeatureManager FeatureManager { get; init; }
    public required IHttpContextAccessor HttpContextAccessor { get; init; }
    public required IOptions<GlobalVariables> GlobalVariables { get; init; }
    public required IRegistrationPeriodProvider RegistrationPeriodProvider { get; init; }
}
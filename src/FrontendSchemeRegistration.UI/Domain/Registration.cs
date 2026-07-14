using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using RegistrationJourneyEnum = FrontendSchemeRegistration.Application.Enums.RegistrationJourney;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Helpers;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;

namespace FrontendSchemeRegistration.UI.Domain;

public sealed class Registration
{
    private readonly RegistrationFeeCalculationDetails[]? _feeCalculationDetails;
    private readonly DateTime? _latestSubmit2Date;
    private readonly DateTime? _firstSubmit2Date;
    private readonly DateTimeOffset _now;
    private readonly bool _isRegistrationFeePaid;
    private readonly bool _hasAnyApprovedOrQueriedRegulatorDecision;
    private readonly bool _hasLatestFileBeenSubmittedForFeeCalculation;
    private readonly DateTime? _latestSubmit1Date;
    private readonly int _registrationYear;
    private readonly DateTime _lateFeeDeadlineDate;
    private readonly string _regulatorNation;

    internal Registration(
        ApplicationStatusType applicationStatus,
        bool? applicationIsResubmission,
        string? applicationReferenceNumber,
        string? registrationFeePaymentMethod,
        RegistrationJourney? applicationRegistrationJourney,
        DateTime? latestSubmit2Date,
        DateTime? firstSubmit2Date,
        bool hasAnyApprovedOrQueriedRegulatorDecision,
        bool hasLatestFileBeenSubmittedForFeeCalculation,
        DateTime? latestSubmit1Date,
        RegistrationFeeCalculationDetails[]? feeCalculationDetails,
        ComplianceSchemeDto? selectedComplianceScheme,
        RegistrationJourney? registrationJourney,
        bool? isResubmission,
        int? organisationNationId,
        int registrationYear,
        DateTime lateFeeDeadlineDate,
        DateTimeOffset now)
    {
        _feeCalculationDetails = feeCalculationDetails;
        _latestSubmit2Date = latestSubmit2Date;
        _firstSubmit2Date = firstSubmit2Date;
        _now = now;
        _hasAnyApprovedOrQueriedRegulatorDecision = hasAnyApprovedOrQueriedRegulatorDecision;
        _hasLatestFileBeenSubmittedForFeeCalculation = hasLatestFileBeenSubmittedForFeeCalculation;
        _latestSubmit1Date = latestSubmit1Date;
        _registrationYear = registrationYear;
        _lateFeeDeadlineDate = lateFeeDeadlineDate;

        var isComplianceScheme = selectedComplianceScheme is not null;

        var effectiveIsResubmission = (applicationIsResubmission ?? isResubmission) ?? false;
        if (applicationStatus is ApplicationStatusType.AcceptedByRegulator or ApplicationStatusType.ApprovedByRegulator
            && isResubmission is true)
        {
            effectiveIsResubmission = true;
            applicationStatus = ApplicationStatusType.NotStarted;
        }

        int? nationId;
        if (isComplianceScheme)
            nationId = selectedComplianceScheme!.NationId;
        else if (feeCalculationDetails is { Length: > 0 })
            nationId = feeCalculationDetails[0].NationId;
        else
            nationId = organisationNationId;

        var regulatorNation = nationId switch
        {
            (int)Nation.England => "GB-ENG",
            (int)Nation.Scotland => "GB-SCT",
            (int)Nation.Wales => "GB-WLS",
            (int)Nation.NorthernIreland => "GB-NIR",
            _ => "regulator"
        };

        var effectiveJourney = isComplianceScheme
            ? (applicationRegistrationJourney ?? registrationJourney)
            : registrationJourney;

        var readyToCalculateFees = RegistrationApplicationStatusCalculator.ReadyToCalculateFees(feeCalculationDetails);
        var fileUploadStatus = RegistrationApplicationStatusCalculator.CalculateFileUploadStatus(applicationStatus, readyToCalculateFees);
        var isRegistrationFeePaid = RegistrationApplicationStatusCalculator.IsRegistrationFeePaid(registrationFeePaymentMethod);
        var paymentViewStatus = RegistrationApplicationStatusCalculator.CalculatePaymentViewStatus(fileUploadStatus, isRegistrationFeePaid);
        var additionalDetailsStatus = RegistrationApplicationStatusCalculator.CalculateAdditionalDetailsStatus(paymentViewStatus, latestSubmit2Date is not null);

        ApplicationStatus = applicationStatus;
        IsResubmission = effectiveIsResubmission;
        IsComplianceScheme = isComplianceScheme;
        SelectedComplianceSchemeId = selectedComplianceScheme?.Id;
        RegistrationJourney = effectiveJourney;
        ApplicationReferenceNumber = applicationReferenceNumber;
        _regulatorNation = regulatorNation;
        FileUploadStatus = fileUploadStatus;
        PaymentViewStatus = paymentViewStatus;
        AdditionalDetailsStatus = additionalDetailsStatus;
        _isRegistrationFeePaid = isRegistrationFeePaid;
    }

    public ApplicationStatusType ApplicationStatus { get; }
    public bool IsResubmission { get; }
    public bool IsComplianceScheme { get; }
    public Guid? SelectedComplianceSchemeId { get; }
    public RegistrationJourney? RegistrationJourney { get; }
    public string? ApplicationReferenceNumber { get; }
    public RegistrationTaskListStatus FileUploadStatus { get; }
    public RegistrationTaskListStatus PaymentViewStatus { get; }
    public RegistrationTaskListStatus AdditionalDetailsStatus { get; }
    public bool CanViewFeeCalculations => FileUploadStatus == RegistrationTaskListStatus.Completed;

    public async Task<ProducerFeeCalculationDomainViewModel?> GetProducerRegistrationFees(
        IPaymentCalculationService paymentCalculationService,
        ILogger logger)
    {
        if (_feeCalculationDetails is not { Length: > 0 })
        {
            logger.LogWarning("Unable to GetProducerRegistrationFees Details, session.ReadyToCalculateFees is null");
            return null;
        }

        bool isLateFeeApplicable;
        if (_firstSubmit2Date is not null)
        {
            isLateFeeApplicable = _firstSubmit2Date >= _lateFeeDeadlineDate;
        }
        else
        {
            isLateFeeApplicable = _now.Date >= _lateFeeDeadlineDate;
        }

        var feeCalculationDetails = _feeCalculationDetails[0];
        var response = await paymentCalculationService.GetProducerRegistrationFees(new PaymentCalculationRequest
        {
            Regulator = _regulatorNation,
            ApplicationReferenceNumber = ApplicationReferenceNumber,
            IsLateFeeApplicable = isLateFeeApplicable,
            IsProducerOnlineMarketplace = feeCalculationDetails.IsOnlineMarketplace,
            IsClosedLoopRecycling = feeCalculationDetails.IsClosedLoopRecycling,
            NoOfSubsidiariesOnlineMarketplace = feeCalculationDetails.NumberOfSubsidiariesBeingOnlineMarketPlace,
            NoOfSubsidiariesClosedLoopRecycling = feeCalculationDetails.NumberOfSubsidiariesBeingClosedLoopRecycling,
            NumberOfSubsidiaries = feeCalculationDetails.NumberOfSubsidiaries,
            ProducerType = feeCalculationDetails.OrganisationSize,
            SubmissionDate = _latestSubmit2Date ?? _now.UtcDateTime
        });

        if (response is null)
        {
            logger.LogWarning("Unable to GetProducerRegistrationFees Details, paymentCalculationService.GetProducerRegistrationFees is null");
            return null;
        }

        return new ProducerFeeCalculationDomainViewModel
        {
            ApplicationStatus = ApplicationStatus,
            OrganisationSize = feeCalculationDetails.OrganisationSize,
            IsOnlineMarketplace = feeCalculationDetails.IsOnlineMarketplace,
            IsClosedLoopRecycling = feeCalculationDetails.IsClosedLoopRecycling,
            NumberOfSubsidiaries = feeCalculationDetails.NumberOfSubsidiaries,
            NumberOfSubsidiariesBeingOnlineMarketplace = feeCalculationDetails.NumberOfSubsidiariesBeingOnlineMarketPlace,
            NumberOfSubsidiariesBeingClosedLoopRecycling = feeCalculationDetails.NumberOfSubsidiariesBeingClosedLoopRecycling,
            IsLateFeeApplicable = isLateFeeApplicable,
            BaseFee = response.ProducerRegistrationFee,
            OnlineMarketplaceFee = response.ProducerOnlineMarketPlaceFee,
            ClosedLoopRecyclingFee = response.ProducerClosedLoopRecyclingFee,
            TotalSubsidiaryFee = response.SubsidiariesFee
                                 - response.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee
                                 - response.SubsidiariesFeeBreakdown.TotalSubsidiariesClosedLoopRecyclingFee,
            TotalSubsidiaryOnlineMarketplaceFee = response.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
            TotalSubsidiaryClosedLoopRecyclingFee = response.SubsidiariesFeeBreakdown.TotalSubsidiariesClosedLoopRecyclingFee,
            TotalPreviousPayments = response.PreviousPayment,
            TotalFeeAmount = response.TotalFee,
            TotalAmountOutstanding = response.OutstandingPayment < 0 ? 0 : response.OutstandingPayment,
            IsRegistrationFeePaid = _isRegistrationFeePaid,
            ProducerLateRegistrationFee = response.ProducerLateRegistrationFee,
            RegistrationApplicationSubmitted = _latestSubmit2Date is not null
        };
    }

    public async Task<ComplianceSchemeFeeCalculationDomainViewModel?> GetComplianceSchemeRegistrationFees(
        IPaymentCalculationService paymentCalculationService,
        ILogger logger)
    {
        if (_feeCalculationDetails is not { Length: > 0 })
        {
            logger.LogWarning("Unable to GetComplianceSchemeRegistrationFees Details, session.ReadyToCalculateFees is null");
            return null;
        }

        bool couldLateFeeApplyToMember;
        // has any CSO submission ever been approved or queried and the latest file been submitted for fees calculation? (and it's 2026+)
        // the approved/queried submission does not have to relate to the latest submitted file
        // then base fee on the last time it was submitted for fee calculation
        if (_hasAnyApprovedOrQueriedRegulatorDecision && _hasLatestFileBeenSubmittedForFeeCalculation && _registrationYear >= 2026)
        {
            couldLateFeeApplyToMember = _latestSubmit1Date!.Value.Date >= _lateFeeDeadlineDate;
        }
        // it must have been submitted for approval at least once
        // either it has never been approved/queried or the latest file upload hasn't been submitted for fees calculation or it's earlier than 2026
        // then base fee on the first time it was submitted for approval
        else if (_firstSubmit2Date is not null)
        {
            couldLateFeeApplyToMember = _firstSubmit2Date >= _lateFeeDeadlineDate;
        }
        // has never been submitted for approval - generate fees dynamically
        else
        {
            couldLateFeeApplyToMember = _now.Date >= _lateFeeDeadlineDate;
        }

        var isOriginalCsoSubmissionLate =
            _firstSubmit2Date is not null
            && _firstSubmit2Date >= _lateFeeDeadlineDate;

        var complianceSchemeMembers = _feeCalculationDetails.Select(c => new ComplianceSchemePaymentCalculationRequestMember
        {
            IsLateFeeApplicable = isOriginalCsoSubmissionLate || (couldLateFeeApplyToMember && (_firstSubmit2Date is null || c.IsNewJoiner)),
            IsOnlineMarketplace = c.IsOnlineMarketplace,
            IsClosedLoopRecycling = c.IsClosedLoopRecycling,
            MemberId = c.OrganisationId,
            MemberType = c.OrganisationSize,
            NoOfSubsidiariesOnlineMarketplace = c.NumberOfSubsidiariesBeingOnlineMarketPlace,
            NoOfSubsidiariesClosedLoopRecycling = c.NumberOfSubsidiariesBeingClosedLoopRecycling,
            NumberOfSubsidiaries = c.NumberOfSubsidiaries
        }).ToList();

        // TODO Temporary logic to exclude registration fee for CSO small (2026)
        // - currently a CSO registration with only Small Producer journey will incorrectly not be charged registration fee
        // (it is assumed to have been charged for the Large Producer journey)
        bool includeRegistrationFee = RegistrationJourney != RegistrationJourneyEnum.CsoSmallProducer;

        var response = await paymentCalculationService.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationRequest
        {
            Regulator = _regulatorNation,
            ApplicationReferenceNumber = ApplicationReferenceNumber,
            SubmissionDate = _latestSubmit2Date ?? _now.UtcDateTime,
            ComplianceSchemeMembers = complianceSchemeMembers,
            IncludeRegistrationFee = includeRegistrationFee
        });

        if (response is null)
        {
            logger.LogWarning("Unable to GetComplianceSchemeRegistrationFees Details, paymentCalculationService.GetComplianceSchemeRegistrationFees is null");
            return null;
        }

        var perProducer = response.ComplianceSchemeMembersWithFees.GetIndividualProducers(_feeCalculationDetails);
        var smallFee = perProducer.smallProducers.GetFees();
        var largeFee = perProducer.largeProducers.GetFees();
        var onlineMarketplaces = response.ComplianceSchemeMembersWithFees.GetOnlineMarketPlaces();
        var closedLoopRecyclers = response.ComplianceSchemeMembersWithFees.GetClosedLoopRecyclers();
        var lateFees = response.ComplianceSchemeMembersWithFees.GetLateProducers();
        var subsidiariesFees = response.ComplianceSchemeMembersWithFees.GetSubsidiariesCompanies();
        var subsidiariesCount = _feeCalculationDetails.Sum(d => d.NumberOfSubsidiaries);

        return new ComplianceSchemeFeeCalculationDomainViewModel
        {
            RegistrationFee = response.ComplianceSchemeRegistrationFee,
            SmallProducersFee = smallFee,
            SmallProducersCount = perProducer.smallProducers.Count,
            LargeProducersFee = largeFee,
            LargeProducersCount = perProducer.largeProducers.Count,
            OnlineMarketplaceFee = onlineMarketplaces.Sum(),
            OnlineMarketplaceCount = onlineMarketplaces.Count,
            ClosedLoopRecyclingFee = closedLoopRecyclers.Sum(),
            ClosedLoopRecyclingCount = closedLoopRecyclers.Count,
            SubsidiaryCompanyFee = subsidiariesFees.Sum(),
            SubsidiaryCompanyCount = subsidiariesCount,
            LateProducerFee = lateFees.Sum(),
            LateProducersCount = lateFees.Count,
            TotalPreviousPayments = response.PreviousPayment,
            TotalFeeAmount = response.TotalFee,
            TotalAmountOutstanding = response.OutstandingPayment < 0 ? 0 : response.OutstandingPayment,
            IsRegistrationFeePaid = _isRegistrationFeePaid,
            RegistrationApplicationSubmitted = _latestSubmit2Date is not null
        };
    }
}

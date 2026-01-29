using System.Globalization;
using System.Security.Claims;
using AutoFixture;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Services;

using Application.Options.RegistrationPeriodPatterns;
using Application.Services;
using Microsoft.Extensions.Time.Testing;
using UI.Services.RegistrationPeriods;

[TestFixture]
public class RegistrationApplicationServiceTests
{
    private readonly ISession _httpSession = new Mock<ISession>().Object;

    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IPaymentCalculationService> _paymentCalculationServiceMock;
    private Mock<ISessionManager<RegistrationApplicationSession>> _sessionManagerMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _frontEndSessionManagerMock;

    private Fixture _fixture;
    private RegistrationApplicationService _service;
    private Mock<IFeatureManager> _featureManagerMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<IRegistrationPeriodProvider> _mockRegistrationPeriodProvider;
    private FakeTimeProvider _dateTimeProvider;
    private int _validRegistrationYear;

    private RegistrationApplicationSession _session; 

    private Mock<ILogger<RegistrationApplicationService>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        _dateTimeProvider = new FakeTimeProvider();
        _validRegistrationYear = _dateTimeProvider.GetLocalNow().Year;
        _submissionServiceMock = new Mock<ISubmissionService>();
        _paymentCalculationServiceMock = new Mock<IPaymentCalculationService>();
        _sessionManagerMock = new Mock<ISessionManager<RegistrationApplicationSession>>();
        _frontEndSessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _loggerMock = new Mock<ILogger<RegistrationApplicationService>>();
        _featureManagerMock = new Mock<IFeatureManager>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _mockRegistrationPeriodProvider = new Mock<IRegistrationPeriodProvider>();
        
        var globalVariables = Options.Create(new GlobalVariables
        {
            LateFeeDeadline2025 = new(2025, 4, 1),
            LargeProducerLateFeeDeadline2026 = new(2025, 10, 1),
            SmallProducerLateFeeDeadline2026 = new(2026, 4, 1)
        });

        _fixture = new Fixture();

        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = globalVariables,
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        _service = new RegistrationApplicationService(deps, _dateTimeProvider);

        var submissionYear = DateTime.Now.Year.ToString();
        var dataPeriod = $"January to December {submissionYear}";
        _session = new()
        {
            SubmissionId = Guid.NewGuid(),
            LastSubmittedFile = new LastSubmittedFileDetails
            {
                FileId = Guid.NewGuid(),
                SubmittedByName = "John Doe",
                SubmittedDateTime = DateTime.Now
            },
            Period = new SubmissionPeriod
            {
                DataPeriod = dataPeriod,
                StartMonth = "January",
                EndMonth = "December",
                Year = $"{submissionYear}"
            },
            SubmissionPeriod = dataPeriod
        };
    }

    [Test]
    public async Task GetProducerRegistrationFees_ShouldReturnNull_WhenPaymentCalculationServiceReturnsNull()
    {
        // Arrange
        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(1).ToArray();

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync((PaymentCalculationResponse?) null);

        // Act
        var result = await _service.GetProducerRegistrationFees(_httpSession);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyLog(l => l.LogWarning("Unable to GetProducerRegistrationFees Details, paymentCalculationService.GetProducerRegistrationFees is null"));
    }

    [Test]
    public async Task GetProducerRegistrationFees_ShouldReturnNull_WhenFileReachedSynapse_Is_false()
    {
        // Arrange
        _session.RegistrationFeeCalculationDetails = null;

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync((PaymentCalculationResponse?) null);

        // Act
        var result = await _service.GetProducerRegistrationFees(_httpSession);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyLog(l => l.LogWarning("Unable to GetProducerRegistrationFees Details, session.FileReachedSynapse is null"));
    }

    [Test]
    public async Task GetProducerRegistrationFees_ShouldReturnViewModel_WhenPaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(1).ToArray();
        var feeCalculationDetail = _session.RegistrationFeeCalculationDetails[0];

        var paymentCalculationResponse = _fixture.Create<PaymentCalculationResponse>();

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(paymentCalculationResponse);

        // Act
        var result = await _service.GetProducerRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull();
        result!.OrganisationSize.Should().Be(feeCalculationDetail.OrganisationSize);
        result.BaseFee.Should().Be(paymentCalculationResponse.ProducerRegistrationFee);
        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(_httpSession, _session), Times.Once);
    }

    [Test]
    public async Task GetProducerRegistrationFees_ShouldSaveUpdatedSession_WhenPaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(1).ToArray();

        var paymentCalculationResponse = _fixture.Create<PaymentCalculationResponse>();
        paymentCalculationResponse.OutstandingPayment = -100;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(paymentCalculationResponse);

        // Act
        await _service.GetProducerRegistrationFees(_httpSession);

        // Assert
        _session.TotalAmountOutstanding.Should().Be(0);
        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(_httpSession, _session), Times.Once);
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldReturnNull_WhenPaymentCalculationServiceReturnsNull()
    {
        // Arrange
        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>().ToArray();

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync((ComplianceSchemePaymentCalculationResponse?) null);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyLog(l => l.LogWarning("Unable to GetComplianceSchemeRegistrationFees Details, paymentCalculationService.GetComplianceSchemeRegistrationFees is null"));
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldReturnNull_WhenFileReachedSynapse_Is_false()
    {
        // Arrange
        _session.RegistrationFeeCalculationDetails = null;

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync((ComplianceSchemePaymentCalculationResponse?) null);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().BeNull();
        _loggerMock.VerifyLog(l => l.LogWarning("Unable to GetComplianceSchemeRegistrationFees Details, session.FileReachedSynapse is null"));
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldReturnViewModel_WhenPaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>().ToArray();

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull();
        result!.RegistrationFee.Should().Be(response.ComplianceSchemeRegistrationFee);
        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(_httpSession, _session), Times.Once);
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldSaveUpdatedSession_WhenPaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        var session = _fixture.Build<RegistrationApplicationSession>()
            .Create();

        session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>().ToArray();

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();
        response.OutstandingPayment = -100;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        session.TotalAmountOutstanding.Should().Be(0);
        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(_httpSession, session), Times.Once);
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldUse_IsNewJoiner_For_LateFee_WhenPaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        var session = _fixture.Build<RegistrationApplicationSession>()
            .Create();

        session.IsLateFeeApplicable = true;
        session.IsOriginalCsoSubmissionLate = false;
        session.IsResubmission = true;

        session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(2).ToArray();
        session.RegistrationFeeCalculationDetails[0].IsNewJoiner = true;
        session.RegistrationFeeCalculationDetails[1].IsNewJoiner = false;

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();
        response.OutstandingPayment = -100;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull();
        _paymentCalculationServiceMock.Verify(x => x.GetComplianceSchemeRegistrationFees(It.Is<ComplianceSchemePaymentCalculationRequest>(r => r.ComplianceSchemeMembers[0].IsLateFeeApplicable == true &&
                                                                                                                                               r.ComplianceSchemeMembers[1].IsLateFeeApplicable == false)));
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldUse_IsLateFeeApplicable_AndNewJoiner_For_LateFee_When_Application_IsInQueriedStatus_PaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        var session = _fixture.Build<RegistrationApplicationSession>()
            .Create();

        session.IsLateFeeApplicable = true;
        session.HasAnyApprovedOrQueriedRegulatorDecision = true;
        session.IsOriginalCsoSubmissionLate = false;

        session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(2).ToArray();
        session.RegistrationFeeCalculationDetails[0].IsNewJoiner = false;
        session.RegistrationFeeCalculationDetails[1].IsNewJoiner = true;

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();
        response.OutstandingPayment = -100;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull();
        _paymentCalculationServiceMock.Verify(x => x.GetComplianceSchemeRegistrationFees(It.Is<ComplianceSchemePaymentCalculationRequest>(r => r.ComplianceSchemeMembers[0].IsLateFeeApplicable == false &&
                                                                                                                                               r.ComplianceSchemeMembers[1].IsLateFeeApplicable == true)));
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldUse_IsOriginalCsoSubmissionLate_For_LateFee_When_PaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        var session = _fixture.Build<RegistrationApplicationSession>()
            .Create();

        session.IsLateFeeApplicable = true;
        session.IsOriginalCsoSubmissionLate = true;
        session.IsResubmission = false;

        session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(2).ToArray();
        session.RegistrationFeeCalculationDetails[0].IsNewJoiner = true;
        session.RegistrationFeeCalculationDetails[1].IsNewJoiner = false;

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();
        response.OutstandingPayment = -100;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull();
        _paymentCalculationServiceMock.Verify(x => x.GetComplianceSchemeRegistrationFees(It.Is<ComplianceSchemePaymentCalculationRequest>(r => r.ComplianceSchemeMembers[0].IsLateFeeApplicable == true &&
                                                                                                                                               r.ComplianceSchemeMembers[1].IsLateFeeApplicable == true)));
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldSetLateFee_ToAllProducers_When_OriginalCSOSubmissionIsLate_PaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        var session = _fixture.Build<RegistrationApplicationSession>()
            .Create();

        session.IsLateFeeApplicable = true;
        session.IsOriginalCsoSubmissionLate = true;

        session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(2).ToArray();
        session.RegistrationFeeCalculationDetails[0].IsNewJoiner = true;
        session.RegistrationFeeCalculationDetails[1].IsNewJoiner = false;

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull();
        _paymentCalculationServiceMock.Verify(x => x.GetComplianceSchemeRegistrationFees(It.Is<ComplianceSchemePaymentCalculationRequest>(r => r.ComplianceSchemeMembers[0].IsLateFeeApplicable == true &&
                                                                                                                                               r.ComplianceSchemeMembers[1].IsLateFeeApplicable == true)));
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldSetLateFee_ToAllProducers_When_CurrentSubmissionIsLate_PaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        var session = _fixture.Build<RegistrationApplicationSession>()
            .Create();

        session.IsLateFeeApplicable = true;
        session.IsOriginalCsoSubmissionLate = false;
        session.FirstApplicationSubmittedEventCreatedDatetime = null;

        session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(2).ToArray();
        session.RegistrationFeeCalculationDetails[0].IsNewJoiner = true;
        session.RegistrationFeeCalculationDetails[1].IsNewJoiner = false;

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull();
        _paymentCalculationServiceMock.Verify(x => x.GetComplianceSchemeRegistrationFees(It.Is<ComplianceSchemePaymentCalculationRequest>(r => r.ComplianceSchemeMembers[0].IsLateFeeApplicable == true &&
                                                                                                                                               r.ComplianceSchemeMembers[1].IsLateFeeApplicable == true)));
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ShouldSetLateFee_OnlyToThoseProducersWhoAreNewJoiner_When_SubmissionWasOnTimeButQueried_PaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        var session = _fixture.Build<RegistrationApplicationSession>()
            .Create();

        session.IsLateFeeApplicable = true;
        session.IsOriginalCsoSubmissionLate = false;
        session.FirstApplicationSubmittedEventCreatedDatetime = DateTime.Now;

        session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(2).ToArray();
        session.RegistrationFeeCalculationDetails[0].IsNewJoiner = true;
        session.RegistrationFeeCalculationDetails[1].IsNewJoiner = false;

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull();
        _paymentCalculationServiceMock.Verify(x => x.GetComplianceSchemeRegistrationFees(It.Is<ComplianceSchemePaymentCalculationRequest>(r => r.ComplianceSchemeMembers[0].IsLateFeeApplicable == true &&
                                                                                                                                               r.ComplianceSchemeMembers[1].IsLateFeeApplicable == false)));
    }

    [Test]
    public async Task InitiatePayment_ShouldReturnPaymentId_WhenPaymentInitiationSucceeds()
    {
        // Arrange
        var user = _fixture.Create<ClaimsPrincipal>();
        _session.TotalAmountOutstanding = 100;
        _session.ApplicationReferenceNumber = "REF123";
        _session.RegulatorNation = "GB-EN";

        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = [new Organisation { Id = Guid.NewGuid() }]
        };

        var paymentId = _fixture.Create<string>();

        _paymentCalculationServiceMock.Setup(pcs => pcs.InitiatePayment(It.IsAny<PaymentInitiationRequest>()))
            .ReturnsAsync(paymentId);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        user.SetupClaimsPrincipal(userData);

        // Act
        var result = await _service.InitiatePayment(user, _httpSession);

        // Assert
        result.Should().Be(paymentId);
        _paymentCalculationServiceMock.Verify(pcs => pcs.InitiatePayment(It.Is<PaymentInitiationRequest>(r =>
            r.UserId == userData.Id.Value &&
            r.OrganisationId == userData.Organisations.First().Id.Value &&
            r.Reference == _session.ApplicationReferenceNumber &&
            r.Amount == _session.TotalAmountOutstanding &&
            r.Regulator == _session.RegulatorNation
        )), Times.Once);
    }

    [Test]
    public void InitiatePayment_ShouldThrowException_WhenUserHasNoOrganisations()
    {
        // Arrange
        var user = _fixture.Create<ClaimsPrincipal>();
        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = []
        };

        user.SetupClaimsPrincipal(userData);

        // Act
        Func<Task> act = async () => await _service.InitiatePayment(user, _httpSession);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("User has no associated organisations.");
    }

    [Test]
    public void InitiatePayment_ShouldThrowException_WhenApplicationReferenceNumberIsNull()
    {
        // Arrange
        var user = _fixture.Create<ClaimsPrincipal>();
        _session.ApplicationReferenceNumber = null;

        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = [new Organisation { Id = Guid.NewGuid() }]
        };

        user.SetupClaimsPrincipal(userData);

        // Act
        Func<Task> act = async () => await _service.InitiatePayment(user, _httpSession);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Application reference number is required.");
    }

    [Test]
    [TestCase(false, 0)]
    [TestCase(true, 34)]
    public async Task CreateApplicationReferenceNumber_Returns_CorrectFormat(bool isComplianceScheme, int csRowNumber)
    {
        // Arrange
        _dateTimeProvider.SetUtcNow(new DateTime(2026, 1, 10));
        const string organisationId = "100082";
        SubmissionPeriod[] submissionPeriods =
        [
            new SubmissionPeriod { StartMonth = "January", EndMonth = "June", Year = "2024" },
            new SubmissionPeriod { StartMonth = "April", EndMonth = "September", Year = "2030" }
        ];

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        var frontEndSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession()
        };

        _frontEndSessionManagerMock
            .Setup(m => m.GetSessionAsync(_httpSession))
            .ReturnsAsync(frontEndSession);

        foreach (var period in submissionPeriods)
        {
            _session.Period = period;
            _session.SelectedComplianceScheme = new ComplianceSchemeDto { RowNumber = csRowNumber };

            // Act
            await _service.SetRegistrationFileUploadSession(_httpSession, organisationId, It.IsAny<int>(), false);

            switch (int.Parse(period.Year))
            {
                // Assert
                case 2024:
                    _frontEndSessionManagerMock.Verify(
                        m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                            session.RegistrationSession.ApplicationReferenceNumber.Contains(isComplianceScheme ? $"{csRowNumber.ToString()}{period.Year.Remove(0, 2)}P2" : "P2"))),
                        Times.Once);
                    _frontEndSessionManagerMock.Reset();
                    break;
                case 2030:
                    _frontEndSessionManagerMock.Verify(
                        m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                            session.RegistrationSession.ApplicationReferenceNumber.Contains(isComplianceScheme ? $"{csRowNumber.ToString()}{period.Year.Remove(0, 2)}P1" : "P1"))),
                        Times.Once);
                    _frontEndSessionManagerMock.Reset();
                    break;
            }
        }
    }

    [Test]
    public async Task CreateApplicationReferenceNumber_ShouldGenerateReferenceNumber_WhenSessionIsValid()
    {
        // Arrange
        _dateTimeProvider.SetUtcNow(new DateTime(2026, 1, 10));
        var organisationNumber = "123";
        string expectedApplicationReferenceNumber = $"PEPR{organisationNumber}{_dateTimeProvider.GetUtcNow().Year - 2000}P1S";

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        var frontEndSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession()
        };

        _frontEndSessionManagerMock
            .Setup(m => m.GetSessionAsync(_httpSession))
            .ReturnsAsync(frontEndSession);

        // Act
        await _service.SetRegistrationFileUploadSession(_httpSession, organisationNumber, It.IsAny<int>(), false);

        // Assert
        _frontEndSessionManagerMock.Verify(
            m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                session.RegistrationSession.ApplicationReferenceNumber == expectedApplicationReferenceNumber)),
            Times.Once);
    }

    [Test]
    public async Task CreateApplicationReferenceNumber_ShouldUsePeriodNumber2_WhenCurrentDateIsAfterPeriodEnd()
    {
        // Arrange
        var organisationNumber = "123";
        
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        var frontEndSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession()
        };

        _frontEndSessionManagerMock
            .Setup(m => m.GetSessionAsync(_httpSession))
            .ReturnsAsync(frontEndSession);

        //this is wrong needs fixing 
        var submissionYear = _dateTimeProvider.GetUtcNow().AddYears(-1).Year;
        _session.Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" };
        
        var expectedApplicationReferenceNumber = $"PEPR{organisationNumber}{submissionYear - 2000}P2S";
        // Act
        await _service.SetRegistrationFileUploadSession(_httpSession, organisationNumber, It.IsAny<int>(), false);

        // Assert
        _frontEndSessionManagerMock.Verify(
            m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                session.RegistrationSession.ApplicationReferenceNumber == expectedApplicationReferenceNumber)),
            Times.Once);
    }

    [Test]
    public async Task CreateApplicationReferenceNumber_ShouldCreateNewSession_WithExpectedApplicationReferenceNumber_WhenNoSessionExists()
    {
        // Arrange
        var organisationNumber = "123";
        _session.SelectedComplianceScheme = null!;
        var periodEnd = DateTime.Parse("31 December" + _session.Period.Year, new CultureInfo("en-GB"));
        var periodNumber = DateTime.Today <= periodEnd ? 1 : 2;
        var expectedApplicationReferenceNumber = $"PEPR{organisationNumber}{periodEnd.Year - 2000}P{periodNumber}S";

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        var frontEndSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession()
        };

        _frontEndSessionManagerMock
            .Setup(m => m.GetSessionAsync(_httpSession))
            .ReturnsAsync(frontEndSession);

        // Act
        await _service.SetRegistrationFileUploadSession(_httpSession, organisationNumber, It.IsAny<int>(), false);

        // Assert
        _frontEndSessionManagerMock.Verify(
            m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                session.RegistrationSession.ApplicationReferenceNumber == expectedApplicationReferenceNumber)),
            Times.Once);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ShouldPopulateSession_WhenDetailsAreReturned()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId };
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, Name = "test", RowNumber = 1, NationId = 1, CreatedOn = DateTime.Now }
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var applicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null);

        // Assert
        applicationDetails.Should().NotBeNull();
        applicationDetails.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        applicationDetails.ApplicationReferenceNumber.Should().Be(registrationApplicationDetails.ApplicationReferenceNumber);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_2025Registration_BeforeDeadline_SetsLateFeeToFalse()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Parse("2025/03/31"),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession());

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession()
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2025, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeFalse();
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_2025Registration_AfterDeadline_SetsLateFeeToTrue()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Parse("2025/04/02"),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession());

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession()
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2025, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeTrue();
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_2026SmallProducer_BeforeDeadline_SetsLateFeeToFalse()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;
        var lastSubmittedFileDetails = new LastSubmittedFileDetails
        {
            FileId = Guid.NewGuid(),
            SubmittedByName = "test",
            SubmittedDateTime = DateTime.Now
        };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Parse("2025/09/30"),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            LastSubmittedFile = lastSubmittedFileDetails,
            RegistrationFeeCalculationDetails =
            [
                new RegistrationFeeCalculationDetails
                {
                    OrganisationSize = "Small",
                    NationId = 1
                }
            ]
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                LastSubmittedFile = lastSubmittedFileDetails
            });

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession()
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2026, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeFalse();
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_2026SmallProducer_AfterDeadline_SetsLateFeeToTrue()
    {
        var dateTime2025 = new DateTime(2025,12, 30);
        var globalVariables = Options.Create(new GlobalVariables
        {
            LateFeeDeadline2025 = new(dateTime2025.Year, 4, 1),
            LargeProducerLateFeeDeadline2026 = new(dateTime2025.Year, 10, 1),
            SmallProducerLateFeeDeadline2026 = dateTime2025.AddDays(-1)
        });

        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = globalVariables,
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        _service = new RegistrationApplicationService(deps, _dateTimeProvider);

        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var lastSubmittedFileDetails = new LastSubmittedFileDetails
        {
            FileId = Guid.NewGuid(),
            SubmittedByName = "test",
            SubmittedDateTime = dateTime2025
        };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Today.AddDays(-1).AddMinutes(1),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            LastSubmittedFile = lastSubmittedFileDetails,
            RegistrationFeeCalculationDetails =
            [
                new RegistrationFeeCalculationDetails
                {
                    OrganisationSize = "Small",
                    NationId = 1
                }
            ]
        };

        _submissionServiceMock
            .Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock
            .Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                LastSubmittedFile = lastSubmittedFileDetails
            });

        _frontEndSessionManagerMock
            .Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession()
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2026, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeTrue();
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_2026LargeProducer_BeforeDeadline_SetsLateFeeToFalse()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;
        var lastSubmittedFileDetails = new LastSubmittedFileDetails
        {
            FileId = Guid.NewGuid(),
            SubmittedByName = "test",
            SubmittedDateTime = _dateTimeProvider.GetUtcNow().DateTime
        };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Parse("2025/09/30"),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            LastSubmittedFile = lastSubmittedFileDetails,
            RegistrationFeeCalculationDetails =
            [
                new RegistrationFeeCalculationDetails
                {
                    OrganisationSize = "L",
                    NationId = 1
                }
            ]
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                LastSubmittedFile = lastSubmittedFileDetails
            });

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession()
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2026, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeFalse();
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_2026LargeProducer_AfterDeadline_SetsLateFeeToTrue()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;
        var lastSubmittedFileDetails = new LastSubmittedFileDetails
        {
            FileId = Guid.NewGuid(),
            SubmittedByName = "test",
            SubmittedDateTime = _dateTimeProvider.GetUtcNow().DateTime
        };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Parse("2025/10/02"),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            LastSubmittedFile = lastSubmittedFileDetails,
            RegistrationFeeCalculationDetails =
            [
                new RegistrationFeeCalculationDetails
                {
                    OrganisationSize = "L",
                    NationId = 1
                }
            ]
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                LastSubmittedFile = lastSubmittedFileDetails
            });

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession()
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2026, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeTrue();
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_ComplianceScheme_FirstSubmission_BeforeDeadline_SetsCorrectFlags()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Test CS", NationId = 1 };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Parse("2025/03/31"),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SelectedComplianceScheme = selectedComplianceScheme
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession());

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = selectedComplianceScheme
                }
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2025, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeFalse();
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_ComplianceScheme_FirstSubmission_AfterDeadline_SetsCorrectFlags()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Test CS", NationId = 1 };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Parse("2025/04/02"),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SelectedComplianceScheme = selectedComplianceScheme
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession());

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = selectedComplianceScheme
                }
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2025, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeTrue();
        result.IsOriginalCsoSubmissionLate.Should().BeTrue();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_ComplianceScheme_WithApprovedOrQueriedRegulatorDecision_SetsCorrectFlags()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var selectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), Name = "Test CS", NationId = 1 };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            HasAnyApprovedOrQueriedRegulatorDecision = true,
            IsLatestSubmittedEventAfterFileUpload = true,
            LatestSubmittedEventCreatedDatetime = DateTime.Parse("2025/04/02"),
            FirstApplicationSubmittedEventCreatedDatetime = DateTime.Parse("2025/03/31"),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SelectedComplianceScheme = selectedComplianceScheme
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession());

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = selectedComplianceScheme
                }
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2025, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeTrue();
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_SetLateFeeFlag_NoSubmissionDate_UsesTodayDate()
    {
        // Arrange
        _dateTimeProvider.SetUtcNow(new DateTime(2026, 1, 10));
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            FirstApplicationSubmittedEventCreatedDatetime = null,
            ApplicationStatus = ApplicationStatusType.NotStarted
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession());

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession()
            });

        var globalVariables = Options.Create(new GlobalVariables
        {
            LateFeeDeadline2025 = DateTime.Today.AddDays(-1), // Set the deadline yesterday to make today's date trigger late fee
            SmallProducerLateFeeDeadline2026 = DateTime.Today.AddDays(-1),
            LargeProducerLateFeeDeadline2026 = DateTime.Today.AddDays(-1)
        });

     
        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, 2025, null);

        // Assert
        result.Should().NotBeNull();
        result.IsLateFeeApplicable.Should().BeTrue(); // Should be true because today is after yesterday's deadline
        result.IsOriginalCsoSubmissionLate.Should().BeFalse();
    }


    [Test]
    [TestCase(1, "GB-ENG")]
    [TestCase(2, "GB-NIR")]
    [TestCase(3, "GB-SCT")]
    [TestCase(4, "GB-WLS")]
    [TestCase(5, "regulator")]
    public async Task GetRegistrationApplicationSession_ShouldPopulate_NationId_From_File_WhenFileReachedSynapse_is_true(int nationId, string expectedNationName)
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", OrganisationSize = "L", NationId = nationId }];
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId };
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var applicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null);

        // Assert
        applicationDetails.Should().NotBeNull();
        applicationDetails.RegulatorNation.Should().Be(expectedNationName);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ShouldPopulate_NationId_From_Organisation_WhenFileReachedSynapse_is_false()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;
        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = null;
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId };
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var applicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null);

        // Assert
        applicationDetails.Should().NotBeNull();
        applicationDetails.RegulatorNation.Should().Be("GB-ENG");
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ShouldCreateNewSession_WhenDetailsAreNull()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var selectedComplianceSchemeId = Guid.NewGuid();
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync((RegistrationApplicationSession) null);

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId }
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync((RegistrationApplicationDetails) null);

        // Act
        var registrationApplicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null);

        // Assert
        registrationApplicationDetails.Should().NotBeNull();
        registrationApplicationDetails.SubmissionId.Should().BeNull();
        registrationApplicationDetails.SelectedComplianceScheme.Should().NotBeNull();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ShouldCall_GetProducerRegistrationFees_When_File_Upload_Is_Completed_And_Payment_Is_Not_Completed()
    {
        // Arrange
        _dateTimeProvider.SetUtcNow(new DateTime(2026, 1, 10));
        var submissionYear = DateTime.Now.Year;
        var submissionId = Guid.NewGuid();
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";

        RegistrationFeeCalculationDetails[] feeCalculationDetails =
        [
            new RegistrationFeeCalculationDetails
            {
                NumberOfSubsidiariesBeingOnlineMarketPlace = 1,
                OrganisationSize = "Large",
                NumberOfSubsidiaries = 1,
                OrganisationId = "123",
                NationId = 1
            }
        ];

        var lastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SubmissionId = submissionId,
            IsSubmitted = true,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _paymentCalculationServiceMock.Setup(x => x.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(new PaymentCalculationResponse { OutstandingPayment = 10, TotalFee = 10, PreviousPayment = 0, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown() })
            .Verifiable();

        _sessionManagerMock.Setup(x => x.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = submissionId,
                IsSubmitted = true,
                RegistrationFeeCalculationDetails = feeCalculationDetails,
                LastSubmittedFile = lastSubmittedFile
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, submissionYear, null);

        // Assert
       
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = $"January to December {submissionYear}",
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = null,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-ENG",
            ApplicationReferenceNumber = "",
            TotalAmountOutstanding = 10,
            IsLateFeeApplicable = true,
            RegistrationJourney = null
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(
            It.IsAny<RegistrationApplicationData>(), It.IsAny<string>(), false,
            It.IsAny<SubmissionType>(), It.IsAny<RegistrationJourney>()),
            Times.Never);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_For_CSO_ShouldCall_GetComplianceSchemeRegistrationFees_When_File_Upload_Is_Completed_And_Payment_Is_Not_Completed()
    {
        // Arrange
        _dateTimeProvider.SetUtcNow(new DateTime(2026, 1, 10));
        var submissionYear = DateTime.Now.Year;
        var submissionId = Guid.NewGuid();
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var cso = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 2, Name = "test", RowNumber = 1, CreatedOn = DateTime.Now };
        RegistrationFeeCalculationDetails[] feeCalculationDetails =
        [
            new RegistrationFeeCalculationDetails
            {
                NumberOfSubsidiariesBeingOnlineMarketPlace = 1,
                OrganisationSize = "Large",
                NumberOfSubsidiaries = 1,
                OrganisationId = organisation.OrganisationNumber,
                NationId = 2,
                IsOnlineMarketplace = false
            }
        ];
        var lastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            SelectedComplianceScheme = cso,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SubmissionId = submissionId,
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test",
            IsSubmitted = true,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _paymentCalculationServiceMock.Setup(x => x.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(new ComplianceSchemePaymentCalculationResponse { OutstandingPayment = 10, TotalFee = 10, PreviousPayment = 0, ComplianceSchemeMembersWithFees = [new ComplianceSchemePaymentCalculationResponseMember { MemberId = organisation.OrganisationNumber, MemberRegistrationFee = 10, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown(), SubsidiariesFee = 10, MemberLateRegistrationFee = 100, MemberOnlineMarketPlaceFee = 1, TotalMemberFee = 1 }], })
            .Verifiable();

        _sessionManagerMock.Setup(x => x.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                SelectedComplianceScheme = cso,
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = submissionId,
                ApplicationReferenceNumber = "Test",
                RegistrationReferenceNumber = "Test",
                IsSubmitted = true,
                RegistrationFeeCalculationDetails = feeCalculationDetails,
                LastSubmittedFile = lastSubmittedFile
            });

        _frontEndSessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = cso
                }
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, submissionYear, RegistrationJourney.CsoSmallProducer, null);

        // Assert
       
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = $"January to December {submissionYear}",
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = null,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-NIR",
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test",
            SelectedComplianceScheme = cso,
            TotalAmountOutstanding = 10,
            RegistrationJourney = RegistrationJourney.CsoSmallProducer,
            IsLateFeeApplicable = true
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(
                It.IsAny<RegistrationApplicationData>(), It.IsAny<string>(), false,
            It.IsAny<SubmissionType>(), It.IsAny<RegistrationJourney>()),
            Times.Never);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ShouldCall_SubmitRegistrationApplication_When_Outstanding_Payment_Amount_Is_Zero()
    {
        // Arrange
        _dateTimeProvider.SetUtcNow(new DateTime(2026, 1, 10));
        var submissionId = Guid.NewGuid();
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";

        RegistrationFeeCalculationDetails[] feeCalculationDetails =
        [
            new RegistrationFeeCalculationDetails
            {
                NumberOfSubsidiariesBeingOnlineMarketPlace = 1,
                OrganisationSize = "Large",
                NumberOfSubsidiaries = 1,
                OrganisationId = organisation.OrganisationNumber,
                NationId = 1
            }
        ];
        var lastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SubmissionId = submissionId,
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test",
            IsSubmitted = true,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _paymentCalculationServiceMock.Setup(x => x.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(new PaymentCalculationResponse { OutstandingPayment = 0, TotalFee = 0, PreviousPayment = 0, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown() })
            .Verifiable();

        _sessionManagerMock.Setup(x => x.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = submissionId,
                ApplicationReferenceNumber = "Test",
                RegistrationReferenceNumber = "Test",
                IsSubmitted = true,
                RegistrationFeeCalculationDetails = feeCalculationDetails,
                LastSubmittedFile = lastSubmittedFile
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null);

        // Assert
        var submissionYear = DateTime.Now.Year.ToString();
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = $"January to December {submissionYear}",
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = "No-Outstanding-Payment",
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-ENG",
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test",
            IsLateFeeApplicable = true,
            RegistrationJourney = null
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(
                It.Is<RegistrationApplicationData>(data => data.PaymentMethod == "No-Outstanding-Payment"),
             "Test", false,
            SubmissionType.RegistrationFeePayment, It.IsAny<RegistrationJourney?>()),
            Times.Once);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_For_CSO_ShouldCall_SubmitRegistrationApplication_When_Outstanding_Payment_Amount_Is_Zero()
    {
        // Arrange
        _dateTimeProvider.SetUtcNow(new DateTime(2026, 1, 10));
        var submissionYear = DateTime.Now.Year;
        var submissionId = Guid.NewGuid();
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.OrganisationRole = "Compliance Scheme";
        var cso = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 2, Name = "test", RowNumber = 1, CreatedOn = DateTime.Now };

        RegistrationFeeCalculationDetails[] feeCalculationDetails =
        [
            new RegistrationFeeCalculationDetails
            {
                NumberOfSubsidiariesBeingOnlineMarketPlace = 1,
                OrganisationSize = "Large",
                NumberOfSubsidiaries = 1,
                OrganisationId = organisation.OrganisationNumber,
                NationId = 2,
                IsOnlineMarketplace = false
            }
        ];

        var lastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            SelectedComplianceScheme = cso,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SubmissionId = submissionId,
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test",
            IsSubmitted = true,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _paymentCalculationServiceMock.Setup(x => x.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(new ComplianceSchemePaymentCalculationResponse { OutstandingPayment = 0, TotalFee = 0, PreviousPayment = 0, ComplianceSchemeMembersWithFees = [new ComplianceSchemePaymentCalculationResponseMember { MemberId = organisation.OrganisationNumber, MemberRegistrationFee = 10, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown(), SubsidiariesFee = 10, MemberLateRegistrationFee = 100, MemberOnlineMarketPlaceFee = 1, TotalMemberFee = 1 }], })
            .Verifiable();

        _sessionManagerMock.Setup(x => x.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                SelectedComplianceScheme = cso,
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = submissionId,
                ApplicationReferenceNumber = "Test",
                RegistrationReferenceNumber = "Test",
                IsSubmitted = true,
                RegistrationFeeCalculationDetails = feeCalculationDetails,
                LastSubmittedFile = lastSubmittedFile,
            });

        _frontEndSessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = cso
                }
            });
        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, submissionYear, null);

        // Assert
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = $"January to December {submissionYear}",
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = "No-Outstanding-Payment",
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-NIR",
            SelectedComplianceScheme = cso,
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test",
            RegistrationJourney = null,
            IsLateFeeApplicable = true
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(
            It.Is<RegistrationApplicationData>(data => data.PaymentMethod == "No-Outstanding-Payment"),
            It.IsAny<string>(),
            false, SubmissionType.RegistrationFeePayment, null),
            Times.Once);
    }

    [Test]
    [TestCase(ApplicationStatusType.AcceptedByRegulator)]
    [TestCase(ApplicationStatusType.ApprovedByRegulator)]
    public async Task GetRegistrationApplicationSession_ReSubmission_ReturnsCorrectViewAndModel(ApplicationStatusType applicationStatusType)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            ApplicationStatus = applicationStatusType,
            SubmissionId = submissionId,
            IsSubmitted = true,
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _paymentCalculationServiceMock.Setup(x => x.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(new PaymentCalculationResponse { OutstandingPayment = 10, TotalFee = 10, PreviousPayment = 0, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown() })
            .Verifiable();

        _sessionManagerMock.Setup(x => x.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.ApprovedByRegulator,
                SubmissionId = submissionId,
                IsSubmitted = true,
                IsLateFeeApplicable = false
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null, true);

        // Assert
        var submissionYear = DateTime.Now.Year.ToString();
        result.Should().BeEquivalentTo(new
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = $"January to December {submissionYear}",
            SubmissionId = submissionId,
            IsResubmission = true,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegulatorNation = "GB-ENG",
            ApplicationReferenceNumber = "",
            ShowRegistrationCaption = false
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(
            It.IsAny<RegistrationApplicationData>(),
            It.IsAny<string>(), false,
            It.IsAny<SubmissionType>(), It.IsAny<RegistrationJourney>()),
            Times.Never);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ReSubmission_Does_Not_Reset_Status_On_Second_Pass_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var submissionYear = DateTime.Now.Year;
        var submissionId = Guid.NewGuid();
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";

        RegistrationFeeCalculationDetails[] feeCalculationDetails =
        [
            new RegistrationFeeCalculationDetails
            {
                NumberOfSubsidiariesBeingOnlineMarketPlace = 1,
                OrganisationSize = "Large",
                NumberOfSubsidiaries = 1,
                OrganisationId = "123",
                NationId = 1
            }
        ];

        var lastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now };

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SubmissionId = submissionId,
            IsSubmitted = true,
            IsResubmission = true,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            ApplicationReferenceNumber = "Test",
            RegistrationFeePaymentMethod = "Online",
            RegistrationApplicationSubmittedDate = DateTime.Now.Date,
            RegistrationApplicationSubmittedComment = "Test"
        };

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        _paymentCalculationServiceMock.Setup(x => x.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(new PaymentCalculationResponse { OutstandingPayment = 100, TotalFee = 100, PreviousPayment = 0, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown() })
            .Verifiable();

        _sessionManagerMock.Setup(x => x.GetSessionAsync(_httpSession))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = submissionId,
                IsSubmitted = true,
                IsResubmission = true,
                RegistrationFeeCalculationDetails = feeCalculationDetails,
                LastSubmittedFile = lastSubmittedFile,
                ApplicationReferenceNumber = "Test",
                RegistrationFeePaymentMethod = "Online",
                RegistrationApplicationSubmittedDate = DateTime.Now.Date,
                RegistrationApplicationSubmittedComment = "Test",
                IsLateFeeApplicable = false
            });

        // Act
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null, true);

        // Assert
        result.Should().BeEquivalentTo(new
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = $"January to December {submissionYear}",
            SubmissionId = submissionId,
            IsSubmitted = true,
            IsResubmission = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-ENG",
            ApplicationReferenceNumber = "Test",
            RegistrationFeePaymentMethod = "Online",
            RegistrationApplicationSubmittedDate = DateTime.Now.Date,
            RegistrationApplicationSubmittedComment = "Test",
            TotalAmountOutstanding = 100,
            ShowRegistrationCaption = false
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(
                It.IsAny<RegistrationApplicationData>(),
            It.IsAny<string>(), false,
            It.IsAny<SubmissionType>(), It.IsAny<RegistrationJourney>()),
            Times.Never);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_Producer_Should_Get_NationID_From_File_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var submissionYear = DateTime.Now.Year;
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", NationId = 2, OrganisationSize = "Large" }];
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId };
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, Name = "test", RowNumber = 1, NationId = 1, CreatedOn = DateTime.Now }
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, submissionYear, null);

        // Assert
        session.Should().NotBeNull();
        session.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        session.ApplicationReferenceNumber.Should().Be(registrationApplicationDetails.ApplicationReferenceNumber);
        session.RegulatorNation.Should().Be("GB-ENG");
    }

    [Test]
    public async Task GetRegistrationApplicationSession_Producer_Should_Get_NationID_From_HttpContext_UserData_When_FeeCalculationDetails_Are_Null_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        organisation.NationId = 1;

        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = null;
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId };
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, Name = "test", RowNumber = 1, NationId = 1, CreatedOn = DateTime.Now }
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null);

        // Assert
        session.Should().NotBeNull();
        session.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        session.ApplicationReferenceNumber.Should().Be(registrationApplicationDetails.ApplicationReferenceNumber);
        session.RegulatorNation.Should().Be("GB-ENG");
    }

    [Test]
    public async Task GetRegistrationApplicationSession_CSO_Should_Get_NationID_From_HttpContext_UserData_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", NationId = 2, OrganisationSize = "Large" }];
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, NationId = 1 };

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, Name = "test", RowNumber = 1, NationId = 1, CreatedOn = DateTime.Now }
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null);

        // Assert
        session.Should().NotBeNull();
        session.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        session.ApplicationReferenceNumber.Should().Be(registrationApplicationDetails.ApplicationReferenceNumber);
        session.RegulatorNation.Should().Be("GB-ENG");
    }
    
    [Test]
    public async Task GetRegistrationApplicationSession_CSO_Sets_Producer_Size_In_Session_And_ReturnsCorrectViewAndModel_When_Not_In_Submission_Service()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", NationId = 2, OrganisationSize = "Large" }];
        registrationApplicationDetails.RegistrationJourney = null;
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, NationId = 1 };

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, Name = "test", RowNumber = 1, NationId = 1, CreatedOn = DateTime.Now }
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, RegistrationJourney.CsoLargeProducer, null);

        // Assert
        session.Should().NotBeNull();
        session.RegistrationJourney.Should().Be(RegistrationJourney.CsoLargeProducer);
        session.ShowRegistrationCaption.Should().BeTrue();
    }
    
    [Test]
    public async Task GetRegistrationApplicationSession_SmallProducer_CSO_Sets_Producer_Size_In_Session_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", NationId = 2, OrganisationSize = "Large" }];
        registrationApplicationDetails.RegistrationJourney = null;
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, NationId = 1 };

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, Name = "test", RowNumber = 1, NationId = 1, CreatedOn = DateTime.Now }
                }
            });

        _submissionServiceMock.Setup(ss =>
                ss.GetRegistrationApplicationDetails(It.Is<GetRegistrationApplicationDetailsRequest>(c =>
                    c.RegistrationJourney == nameof(RegistrationJourney.CsoSmallProducer))))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, RegistrationJourney.CsoSmallProducer, null);

        // Assert
        session.Should().NotBeNull();
        session.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        session.ApplicationReferenceNumber.Should().Be(registrationApplicationDetails.ApplicationReferenceNumber);
        session.RegistrationJourney.Should().Be(RegistrationJourney.CsoSmallProducer);
        session.ShowRegistrationCaption.Should().BeTrue();
    }
    
    [Test]
    public async Task GetRegistrationApplicationSession_Producer_Cannot_Set_Producer_Size_In_Session_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", NationId = 2, OrganisationSize = "Large" }];
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, NationId = 1 };
        _session.RegistrationJourney = null;

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        // Act
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, RegistrationJourney.CsoSmallProducer, null);

        // Assert
        session.Should().NotBeNull();
        session.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        session.RegistrationJourney.Should().BeNull();
        session.ShowRegistrationCaption.Should().Be(session.ShowRegistrationCaption);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_CSO_Should_Get_OutstandingPaymentAmount_When_FileUpload_Is_Completed()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var selectedComplianceSchemeId = Guid.NewGuid();
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", NationId = 2, OrganisationSize = "Large" }];
        registrationApplicationDetails.ApplicationStatus = ApplicationStatusType.SubmittedToRegulator;
        _session.SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, NationId = 1 };

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);
        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = selectedComplianceSchemeId, Name = "test", RowNumber = 1, NationId = 1, CreatedOn = DateTime.Now }
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        var response = _fixture.Create<ComplianceSchemePaymentCalculationResponse>();
        response.ComplianceSchemeMembersWithFees = [new ComplianceSchemePaymentCalculationResponseMember { MemberId = "123", MemberRegistrationFee = 100 }];
        response.OutstandingPayment = -100;
        _paymentCalculationServiceMock.Setup(x => x.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, RegistrationJourney.CsoLargeProducer, null);

        // Assert
        session.Should().NotBeNull();
        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(
                It.IsAny<RegistrationApplicationData>(),
                It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<SubmissionType>(), It.IsAny<RegistrationJourney>()),
            Times.Once);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_Producer_Should_Get_OutstandingPaymentAmount_When_FileUpload_Is_Completed()
    {
        // Arrange
        var organisation = _fixture.Create<Organisation>();
        organisation.OrganisationNumber = "123";
        var registrationApplicationDetails = _fixture.Create<RegistrationApplicationDetails>();
        registrationApplicationDetails.RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", NationId = 2, OrganisationSize = "Large" }];
        registrationApplicationDetails.ApplicationStatus = ApplicationStatusType.SubmittedToRegulator;
        registrationApplicationDetails.RegistrationJourney = null;
        _session.SelectedComplianceScheme = null;
        _session.RegistrationJourney = null;

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        _frontEndSessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = null
                }
            });

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(registrationApplicationDetails);

        var response = _fixture.Create<PaymentCalculationResponse>();
        response.OutstandingPayment = -100;
        _paymentCalculationServiceMock.Setup(x => x.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, null);

        // Assert
        session.Should().NotBeNull();
        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(
            It.IsAny<RegistrationApplicationData>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<SubmissionType>(),
            null),
            Times.Once);
    }

    [Test]
    public async Task SubmitRegistrationApplication_ShouldCallSubmissionServiceWithCorrectParameters()
    {
        // Arrange
        var comments = _fixture.Create<string>();
        var paymentMethod = _fixture.Create<string>();
        var submissionType = _fixture.Create<SubmissionType>();
        var complianceSchemeId = _session.SelectedComplianceScheme?.Id;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        // Act
        await _service.CreateRegistrationApplicationEvent(_httpSession, comments, paymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.CreateRegistrationApplicationEvent(
            It.Is<RegistrationApplicationData>(data => 
                data.ComplianceSchemeId == complianceSchemeId &&
                data.Comments == comments &&
                data.PaymentMethod == paymentMethod &&
                data.SubmissionId == _session.SubmissionId.Value),
            _session.ApplicationReferenceNumber, false,
            submissionType, 
            It.IsAny<RegistrationJourney?>()), Times.Once);
    }

    [Test]
    public async Task SubmitRegistrationApplication_ShouldSaveFeePaymentMethod()
    {
        // Arrange
        var paymentMethod = _fixture.Create<string>();
        var submissionType = _fixture.Create<SubmissionType>();
        var complianceSchemeId = _session.SelectedComplianceScheme?.Id;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        // Act
        await _service.CreateRegistrationApplicationEvent(_httpSession, null, paymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.CreateRegistrationApplicationEvent(
            It.Is<RegistrationApplicationData>(data => 
                data.ComplianceSchemeId == complianceSchemeId &&
                data.Comments == null &&
                data.PaymentMethod == paymentMethod &&
                data.SubmissionId == _session.SubmissionId.Value),
            _session.ApplicationReferenceNumber,
            false,
            submissionType, 
            It.IsAny<RegistrationJourney?>()), Times.Once);

        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(It.IsAny<ISession>(), It.Is<RegistrationApplicationSession>(s => !string.IsNullOrWhiteSpace(s.RegistrationFeePaymentMethod))), Times.Once);
    }

    [Test]
    public async Task SubmitRegistrationApplication_ShouldSaveSubmittedDate()
    {
        // Arrange
        var comments = _fixture.Create<string>();
        var submissionType = _fixture.Create<SubmissionType>();
        var complianceSchemeId = _session.SelectedComplianceScheme?.Id;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        // Act
        await _service.CreateRegistrationApplicationEvent(_httpSession, comments, null, submissionType);

        // Assert
        
        _submissionServiceMock.Verify(ss => ss.CreateRegistrationApplicationEvent(
            It.Is<RegistrationApplicationData>(data => 
                data.ComplianceSchemeId == complianceSchemeId &&
                data.Comments == comments &&
                data.PaymentMethod == null &&
                data.SubmissionId == _session.SubmissionId.Value),
            _session.ApplicationReferenceNumber,
            false,
            submissionType, 
            It.IsAny<RegistrationJourney?>()), Times.Once);

        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(It.IsAny<ISession>(), It.Is<RegistrationApplicationSession>(s => 
            s.RegistrationApplicationSubmittedDate.Value > _dateTimeProvider.GetUtcNow().AddSeconds(-5))), Times.Once);
    }

    [Test]
    public async Task SubmitRegistrationApplication_ShouldHandleNullComments()
    {
        // Arrange
        var paymentMethod = _fixture.Create<string>();
        var submissionType = _fixture.Create<SubmissionType>();
        var complianceSchemeId = _session.SelectedComplianceScheme?.Id;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        // Act
        await _service.CreateRegistrationApplicationEvent(_httpSession, null, paymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.CreateRegistrationApplicationEvent(
            It.Is<RegistrationApplicationData>(data => 
                data.ComplianceSchemeId == complianceSchemeId &&
                data.Comments == null &&
                data.PaymentMethod == paymentMethod &&
                data.SubmissionId == _session.SubmissionId.Value),
           _session.ApplicationReferenceNumber,
            false,
            submissionType, 
            It.IsAny<RegistrationJourney?>()), Times.Once);
    }

    [Test]
    public async Task SubmitRegistrationApplication_ShouldHandleNullPaymentMethod()
    {
        // Arrange
        var comments = _fixture.Create<string>();
        var submissionType = _fixture.Create<SubmissionType>();
        var complianceSchemeId = _session.SelectedComplianceScheme?.Id;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        // Act
        await _service.CreateRegistrationApplicationEvent(_httpSession, comments, null, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.CreateRegistrationApplicationEvent(
            It.Is<RegistrationApplicationData>(data => 
                data.ComplianceSchemeId == complianceSchemeId &&
                data.Comments == comments &&
                data.PaymentMethod == null &&
                data.SubmissionId == _session.SubmissionId.Value),
            _session.ApplicationReferenceNumber,
            false,
            submissionType, 
            It.IsAny<RegistrationJourney?>()), Times.Once);
    }

    [TestCase(SubmissionType.Producer)]
    [TestCase(SubmissionType.Registration)]
    [TestCase(SubmissionType.Subsidiary)]
    [TestCase(SubmissionType.CompaniesHouse)]
    [TestCase(SubmissionType.RegistrationFeePayment)]
    [TestCase(SubmissionType.RegistrationApplicationSubmitted)]
    public async Task SubmitRegistrationApplication_ShouldSupportSpecificSubmissionTypes(SubmissionType submissionType)
    {
        // Arrange
        var comments = _fixture.Create<string>();
        var paymentMethod = _fixture.Create<string>();
        var complianceSchemeId = _session.SelectedComplianceScheme?.Id;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        // Act
        await _service.CreateRegistrationApplicationEvent(_httpSession, comments, paymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.CreateRegistrationApplicationEvent(
            It.Is<RegistrationApplicationData>(data => 
                data.ComplianceSchemeId == complianceSchemeId &&
                data.Comments == comments &&
                data.PaymentMethod == paymentMethod &&
                data.SubmissionId == _session.SubmissionId.Value),
            _session.ApplicationReferenceNumber,
            false,
            submissionType, 
            It.IsAny<RegistrationJourney?>()), Times.Once);
    }

    [Test]
    public async Task SetRegistrationFileUploadSession_ShouldUpdateSessionAndSave()
    {
        // Arrange
        var frontEndSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession()
        };

        _frontEndSessionManagerMock
            .Setup(m => m.GetSessionAsync(_httpSession))
            .ReturnsAsync(frontEndSession);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        // Act
        await _service.SetRegistrationFileUploadSession(_httpSession, "123", DateTime.Now.Year, false);

        // Assert
        frontEndSession.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration.Should().BeTrue();
        var expectedYear = DateTime.Now.Year.ToString();
        frontEndSession.RegistrationSession.SubmissionPeriod.Should().Be($"January to December {expectedYear}");

        _frontEndSessionManagerMock.Verify(
            m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                session.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration == true &&
                session.RegistrationSession.SubmissionPeriod == $"January to December {expectedYear}" &&
                session.RegistrationSession.ApplicationReferenceNumber.Contains("PEPR123") &&
                session.RegistrationSession.IsResubmission == false
            )),
            Times.Once);
    }

    [Test]
    public async Task BuildRegistrationYearApplicationsViewModels_ShouldReturnEmptyList_WhenNoActiveRegistrationWindows()
    {
        // Arrange
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test Organisation",
            OrganisationNumber = "123456",
            OrganisationRole = OrganisationRoles.Producer
        };

        _mockRegistrationPeriodProvider.Setup(x => x.GetAllRegistrationWindows(false))
            .Returns(new List<RegistrationWindow>());

        // Act
        var result = await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, organisation);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task BuildRegistrationYearApplicationsViewModels_ShouldMergeRegistrations_WhenMultipleWindowsInSameYearForDirectProducer()
    {
        // Arrange
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test Organisation",
            OrganisationNumber = "123456",
            OrganisationRole = OrganisationRoles.Producer
        };

        var registrationApplicationSession = new RegistrationApplicationSession
        {
            ApplicationStatus = ApplicationStatusType.NotStarted,
            ApplicationReferenceNumber = "REF001",
            RegistrationReferenceNumber = "REG001",
            IsResubmission = false
        };

        IReadOnlyCollection<RegistrationWindow> registrationWindows =
        [
            CreateRegistrationWindow(WindowType.DirectLargeProducer, 2026, new DateTime(2026, 1, 1), new DateTime(2026, 3, 1), new DateTime(2026, 5, 1)),
            CreateRegistrationWindow(WindowType.DirectLargeProducer, 2026, new DateTime(2026, 6, 1), new DateTime(2026, 8, 1), new DateTime(2026, 10, 1))
        ];

        _dateTimeProvider.SetUtcNow(new DateTime(2026, 6, 1));

        _mockRegistrationPeriodProvider.Setup(x => x.GetAllRegistrationWindows(false))
            .Returns(registrationWindows);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(new RegistrationApplicationDetails());

        // Act
        var result = await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, organisation);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Year.Should().Be(2026);
        result[0].Applications.Should().HaveCount(1); // Single merged window for producers
    }

    [Test]
    public async Task BuildRegistrationYearApplicationsViewModels_ShouldCreateSeparateEntries_ForCso_WithDifferentJourneys()
    {
        // Arrange
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test CSO",
            OrganisationNumber = "999999",
            OrganisationRole = OrganisationRoles.ComplianceScheme
        };

        IReadOnlyCollection<RegistrationWindow> registrationWindows =
        [
            CreateRegistrationWindow(WindowType.CsoLargeProducer, 2026),
            CreateRegistrationWindow(WindowType.CsoSmallProducer, 2026)
        ];

        _mockRegistrationPeriodProvider.Setup(x => x.GetActiveRegistrationWindows(true))
            .Returns(registrationWindows);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(new RegistrationApplicationDetails());

        // Act
        var result = await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, organisation);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Year.Should().Be(2026);
        result[0].Applications.Should().HaveCount(2);
    }

    [Test]
    public async Task BuildRegistrationYearApplicationsViewModels_ShouldReturnOrderedByYearDescending()
    {
        // Arrange
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test Organisation",
            OrganisationNumber = "123456",
            OrganisationRole = OrganisationRoles.Producer
        };

        IReadOnlyCollection<RegistrationWindow> registrationWindows =
        [
            CreateRegistrationWindow(WindowType.DirectLargeProducer, 2024),
            CreateRegistrationWindow(WindowType.DirectLargeProducer, 2026),
            CreateRegistrationWindow(WindowType.DirectLargeProducer, 2025)
        ];

        _dateTimeProvider.SetUtcNow(new DateTime(2026, 6, 1));

        _mockRegistrationPeriodProvider.Setup(x => x.GetAllRegistrationWindows(false))
            .Returns(registrationWindows);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(new RegistrationApplicationDetails());

        // Act
        var result = await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, organisation);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].Year.Should().Be(2026);
        result[1].Year.Should().Be(2025);
        result[2].Year.Should().Be(2024);
    }

    [Test]
    public async Task BuildRegistrationYearApplicationsViewModels_ShouldMapRegistrationWindowData_ToViewModel()
    {
        // Arrange
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test Organisation",
            OrganisationNumber = "123456",
            OrganisationRole = OrganisationRoles.Producer
        };

        var deadlineDate = new DateTime(2026, 7, 15);

        IReadOnlyCollection<RegistrationWindow> registrationWindows =
        [
            CreateRegistrationWindow(WindowType.DirectLargeProducer, 2026, new DateTime(2026, 1, 1), deadlineDate, new DateTime(2026, 9, 1))
        ];

        var registrationApplicationSession = new RegistrationApplicationSession
        {
            ApplicationStatus = ApplicationStatusType.AcceptedByRegulator,
            ApplicationReferenceNumber = "REF-2026-001",
            RegistrationReferenceNumber = "REG-2026-001",
            IsResubmission = true
        };

        _mockRegistrationPeriodProvider.Setup(x => x.GetAllRegistrationWindows(false))
            .Returns(registrationWindows);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(registrationApplicationSession);

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(new RegistrationApplicationDetails());

        _dateTimeProvider.SetUtcNow(new DateTime(2026, 6, 1));

        // Act
        var result = await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, organisation);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var yearViewModel = result[0];
        yearViewModel.Year.Should().Be(2026);
        
        var appViewModel = yearViewModel.Applications.First();
        appViewModel.ApplicationStatus.Should().Be(ApplicationStatusType.NotStarted);
        appViewModel.FileUploadStatus.Should().Be(RegistrationTaskListStatus.NotStarted);
        appViewModel.ApplicationReferenceNumber.Should().Be(string.Empty);
        appViewModel.RegistrationReferenceNumber.Should().BeNull();
        appViewModel.IsResubmission.Should().BeFalse();
        appViewModel.RegistrationYear.Should().Be("2026");
        appViewModel.IsComplianceScheme.Should().BeFalse();
    }

    [Test]
    public async Task BuildRegistrationYearApplicationsViewModels_ShouldSetRegistrationJourney_ForCso()
    {
        // Arrange
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test CSO",
            OrganisationNumber = "999999",
            OrganisationRole = OrganisationRoles.ComplianceScheme
        };

        IReadOnlyCollection<RegistrationWindow> registrationWindows =
        [
            CreateRegistrationWindow(WindowType.CsoLargeProducer, 2026)
        ];

        _mockRegistrationPeriodProvider.Setup(x => x.GetActiveRegistrationWindows(true))
            .Returns(registrationWindows);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(new RegistrationApplicationDetails());

        // Act
        var result = await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, organisation);

        // Assert
        result.Should().NotBeNull();
        result[0].Applications.Should().HaveCount(1);
        result[0].Applications.First().RegistrationJourney.Should().Be(RegistrationJourney.CsoLargeProducer);
    }

    [Test]
    public async Task BuildRegistrationYearApplicationsViewModels_ShouldCallGetRegistrationApplicationSession_WithCorrectParameters()
    {
        // Arrange
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test Organisation",
            OrganisationNumber = "123456",
            OrganisationRole = OrganisationRoles.Producer
        };

        _dateTimeProvider.SetUtcNow(new DateTime(2026, 6, 1));
        var journey = RegistrationJourney.DirectLargeProducer;
        var registrationYear = 2026;

        IReadOnlyCollection<RegistrationWindow> registrationWindows =
        [
            CreateRegistrationWindow(WindowType.DirectLargeProducer, registrationYear)
        ];

        _mockRegistrationPeriodProvider.Setup(x => x.GetAllRegistrationWindows(false))
            .Returns(registrationWindows);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        _submissionServiceMock.Setup(ss => ss.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>()))
            .ReturnsAsync(new RegistrationApplicationDetails());

        // Act
        await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, organisation);

        // Assert
        _submissionServiceMock.Verify(
            x => x.GetRegistrationApplicationDetails(It.Is<GetRegistrationApplicationDetailsRequest>(r =>
                r.OrganisationId == organisation.Id &&
                r.OrganisationNumber == int.Parse(organisation.OrganisationNumber)
            )),
            Times.Once);
    }

    [Test]
    public async Task BuildRegistrationYearApplicationsViewModels_ShouldCheckIfCsoUsingOrganisationRole()
    {
        // Arrange
        var producerOrganisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test Producer",
            OrganisationNumber = "123456",
            OrganisationRole = OrganisationRoles.Producer
        };

        IReadOnlyCollection<RegistrationWindow> producerWindows = [];

        _mockRegistrationPeriodProvider.Setup(x => x.GetAllRegistrationWindows(false))
            .Returns(producerWindows);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        // Act
        await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, producerOrganisation);

        // Assert - Verify GetActiveRegistrationWindows was called with false for producer
        _mockRegistrationPeriodProvider.Verify(x => x.GetAllRegistrationWindows(false), Times.Once);

        // Now test with CSO
        var csoOrganisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test CSO",
            OrganisationNumber = "999999",
            OrganisationRole = OrganisationRoles.ComplianceScheme
        };

        IReadOnlyCollection<RegistrationWindow> csoWindows = [];

        _mockRegistrationPeriodProvider.Setup(x => x.GetActiveRegistrationWindows(true))
            .Returns(csoWindows);

        // Act
        await _service.BuildRegistrationYearApplicationsViewModels(_httpSession, csoOrganisation);

        // Assert - Verify GetActiveRegistrationWindows was called with true for CSO
        _mockRegistrationPeriodProvider.Verify(x => x.GetActiveRegistrationWindows(true), Times.Once);
    }

 
    [Test]
    public async Task GetProducerRegistrationFees_WhenV2Enabled_SendsV2Request_With_All_New_Fields()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync("EnableRegistrationFeeV2")).ReturnsAsync(true);

        _session.Period = new SubmissionPeriod { StartMonth = "January", EndMonth = "June", Year = "2025" };
        _session.LastSubmittedFile ??= new LastSubmittedFileDetails();
        _session.LastSubmittedFile.FileId = Guid.NewGuid();
        _session.LastSubmittedFile.SubmittedDateTime = DateTime.UtcNow;

        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(1).ToArray();
        _session.RegistrationFeeCalculationDetails[0].OrganisationSize = "Large";
        _session.RegistrationFeeCalculationDetails[0].NumberOfSubsidiaries = 3;
        _session.RegistrationFeeCalculationDetails[0].NumberOfSubsidiariesBeingOnlineMarketPlace = 1;
        _session.RegistrationFeeCalculationDetails[0].IsOnlineMarketplace = true;

        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new List<Organisation> { new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "222" } }
        };
        var principal = new ClaimsPrincipal();
        principal.SetupClaimsPrincipal(userData);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(httpContext);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        ProducerPaymentCalculationV2Request? captured = null;
        _paymentCalculationServiceMock
            .Setup(p => p.GetProducerRegistrationFees(It.IsAny<ProducerPaymentCalculationV2Request>()))
            .Callback<ProducerPaymentCalculationV2Request>(r => captured = r)
            .ReturnsAsync(_fixture.Build<PaymentCalculationResponse>()
                .With(x => x.SubsidiariesFeeBreakdown, new SubsidiariesFeeBreakdown())
                .Create());

        // Act
        var result = await _service.GetProducerRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull("V2 call still returns mapped breakdown");

        captured.Should().NotBeNull();
        captured!.FileId.Should().Be(_session.LastSubmittedFile.FileId!.Value);
        captured.ExternalId.Should().Be(userData.Organisations.First().Id!.Value);
        captured.PayerId.Should().Be(222);
        captured.PayerTypeId.Should().Be(1);

        var expectedEarliest = new DateTimeOffset(new DateTime(int.Parse(_session.Period.Year), 06, 30), TimeSpan.Zero);
        captured.InvoicePeriod.Should().BeOnOrAfter(expectedEarliest);

        captured.Regulator.Should().Be(_session.RegulatorNation);
        captured.ApplicationReferenceNumber.Should().Be(_session.ApplicationReferenceNumber);
        captured.IsLateFeeApplicable.Should().Be(_session.IsLateFeeApplicable);
        captured.IsProducerOnlineMarketplace.Should().BeTrue();
        captured.NoOfSubsidiariesOnlineMarketplace.Should().Be(1);
        captured.NumberOfSubsidiaries.Should().Be(3);
        captured.ProducerType.Should().Be("Large");
        captured.SubmissionDate.Should().Be(_session.LastSubmittedFile.SubmittedDateTime!.Value);
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_WhenV2Enabled_SendsV2Request_With_All_New_Fields()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync("EnableRegistrationFeeV2")).ReturnsAsync(true);

        _session.Period = new SubmissionPeriod { StartMonth = "January", EndMonth = "June", Year = "2025" };
        _session.LastSubmittedFile ??= new LastSubmittedFileDetails();
        _session.LastSubmittedFile.FileId = Guid.NewGuid();
        _session.LastSubmittedFile.SubmittedDateTime = DateTime.UtcNow;

        _session.SelectedComplianceScheme = new ComplianceSchemeDto
        {
            Id = Guid.NewGuid(),
            NationId = 2,
            Name = "Test CS",
            RowNumber = 123
        };
        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(2).ToArray();

        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = new List<Organisation> { new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "123" } }
        };
        var principal = new ClaimsPrincipal();
        principal.SetupClaimsPrincipal(userData);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(httpContext);

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        ComplianceSchemePaymentCalculationV2Request? captured = null;
        _paymentCalculationServiceMock
            .Setup(p => p.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationV2Request>()))
            .Callback<ComplianceSchemePaymentCalculationV2Request>(r => captured = r)
            .ReturnsAsync(_fixture.Build<ComplianceSchemePaymentCalculationResponse>()
                .With(x => x.ComplianceSchemeMembersWithFees, new List<ComplianceSchemePaymentCalculationResponseMember>())
                .Create());

        // Act
        var result = await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull("V2 call still returns mapped breakdown");

        captured.Should().NotBeNull();
        captured!.FileId.Should().Be(_session.LastSubmittedFile.FileId!.Value);
        captured.ExternalId.Should().Be(_session.SelectedComplianceScheme.Id!);
        captured.PayerTypeId.Should().Be(2);
        captured.PayerId.Should().Be(123);

        var expectedEarliest = new DateTimeOffset(new DateTime(int.Parse(_session.Period.Year), 06, 30), TimeSpan.Zero);
        captured.InvoicePeriod.Should().BeOnOrAfter(expectedEarliest);

        captured.ApplicationReferenceNumber.Should().Be(_session.ApplicationReferenceNumber);
        captured.Regulator.Should().Be(_session.RegulatorNation);
        captured.SubmissionDate.Should().Be(_session.LastSubmittedFile.SubmittedDateTime!.Value);
    }

    [Test]
    public async Task GetProducerRegistrationFees_WhenV2Disabled_SendsV1Request()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync("EnableRegistrationFeeV2")).ReturnsAsync(false);

        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(1).ToArray();
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        PaymentCalculationRequest? captured = null;
        _paymentCalculationServiceMock
            .Setup(p => p.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .Callback<PaymentCalculationRequest>(r => captured = r)
            .ReturnsAsync(_fixture.Build<PaymentCalculationResponse>()
                .With(x => x.SubsidiariesFeeBreakdown, new SubsidiariesFeeBreakdown())
                .Create());

        var result = await _service.GetProducerRegistrationFees(_httpSession);

        result.Should().NotBeNull();
        captured.Should().NotBeNull();
        captured!.GetType().Should().Be(typeof(PaymentCalculationRequest)); 

        captured.Regulator.Should().Be(_session.RegulatorNation);
        captured.ApplicationReferenceNumber.Should().Be(_session.ApplicationReferenceNumber);
        captured.IsLateFeeApplicable.Should().Be(_session.IsLateFeeApplicable);
        captured.ProducerType.Should().Be(_session.RegistrationFeeCalculationDetails[0].OrganisationSize);
        captured.SubmissionDate.Should().Be(_session.LastSubmittedFile.SubmittedDateTime!.Value);
    }

    [Test]
    public async Task GetProducerRegistrationFees_WhenV2Enabled_ButNoOrganisations_FallsBackToV1()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync("EnableRegistrationFeeV2")).ReturnsAsync(true);

        _session.RegistrationFeeCalculationDetails = _fixture.CreateMany<RegistrationFeeCalculationDetails>(1).ToArray();
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        var userData = new UserData { Id = Guid.NewGuid(), Organisations = new List<Organisation>() };
        var principal = new ClaimsPrincipal();
        principal.SetupClaimsPrincipal(userData);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(httpContext);

        PaymentCalculationRequest? captured = null;
        _paymentCalculationServiceMock
            .Setup(p => p.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .Callback<PaymentCalculationRequest>(r => captured = r)
            .ReturnsAsync(_fixture.Build<PaymentCalculationResponse>()
                .With(x => x.SubsidiariesFeeBreakdown, new SubsidiariesFeeBreakdown())
                .Create());

        // Act
        var result = await _service.GetProducerRegistrationFees(_httpSession);

        // Assert
        result.Should().NotBeNull("we still compute using v1 when no org is available for v2");
        captured.Should().NotBeNull();
        captured!.GetType().Should().Be(typeof(PaymentCalculationRequest), "v1 request should be used when there is no first organisation to index");
    }
    
    [Test]
    public void Constructor_ShouldThrow_ArgumentNull_When_Dependencies_IsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RegistrationApplicationService(null!, _dateTimeProvider));
    }
    
    [Test]
    public void Constructor_ShouldThrow_InvalidOperation_When_SubmissionService_IsNull()
    {
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = null!,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = Options.Create(new GlobalVariables()),
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationApplicationService(deps, _dateTimeProvider));
        ex.Message.Should().Be($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(RegistrationApplicationServiceDependencies.SubmissionService)} cannot be null.");
    }

    [Test]
    public void Constructor_ShouldThrow_InvalidOperation_When_PaymentCalculationService_IsNull()
    {
      
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = null!,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = Options.Create(new GlobalVariables()),
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationApplicationService(deps, _dateTimeProvider));
        ex.Message.Should().Be($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(RegistrationApplicationServiceDependencies.PaymentCalculationService)} cannot be null.");
    }

    [Test]
    public void Constructor_ShouldThrow_InvalidOperation_When_RegistrationSessionManager_IsNull()
    {
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = null!,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = Options.Create(new GlobalVariables()),
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationApplicationService(deps, _dateTimeProvider));
        ex.Message.Should().Be($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(RegistrationApplicationServiceDependencies.RegistrationSessionManager)} cannot be null.");
    }

    [Test]
    public void Constructor_ShouldThrow_InvalidOperation_When_FrontendSessionManager_IsNull()
    {
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = null!,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = Options.Create(new GlobalVariables()),
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationApplicationService(deps, _dateTimeProvider));
        ex.Message.Should().Be($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(RegistrationApplicationServiceDependencies.FrontendSessionManager)} cannot be null.");
    }

    [Test]
    public void Constructor_ShouldThrow_InvalidOperation_When_Logger_IsNull()
    {
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = null!,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = Options.Create(new GlobalVariables()),
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationApplicationService(deps, _dateTimeProvider));
        ex.Message.Should().Be($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(RegistrationApplicationServiceDependencies.Logger)} cannot be null.");
    }

    [Test]
    public void Constructor_ShouldThrow_InvalidOperation_When_FeatureManager_IsNull()
    {
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = null!,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = Options.Create(new GlobalVariables()),
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationApplicationService(deps, _dateTimeProvider));
        ex.Message.Should().Be($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(RegistrationApplicationServiceDependencies.FeatureManager)} cannot be null.");
    }

    [Test]
    public void Constructor_ShouldThrow_InvalidOperation_When_HttpContextAccessor_IsNull()
    {
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = null!,
            GlobalVariables = Options.Create(new GlobalVariables()),
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationApplicationService(deps, _dateTimeProvider));
        ex.Message.Should().Be($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(RegistrationApplicationServiceDependencies.HttpContextAccessor)} cannot be null.");
    }

    [Test]
    public void Constructor_ShouldThrow_InvalidOperation_When_GlobalVariables_IsNull()
    {
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = null!,
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        var ex = Assert.Throws<InvalidOperationException>(() => new RegistrationApplicationService(deps, _dateTimeProvider));
        ex.Message.Should().Be($"{nameof(RegistrationApplicationServiceDependencies)}.{nameof(RegistrationApplicationServiceDependencies.GlobalVariables)} cannot be null.");
    }

    [Test]
    public void Constructor_Should_NotThrow_When_All_Dependencies_Are_Provided()
    {
        var deps = new RegistrationApplicationServiceDependencies
        {
            SubmissionService = _submissionServiceMock.Object,
            PaymentCalculationService = _paymentCalculationServiceMock.Object,
            RegistrationSessionManager = _sessionManagerMock.Object,
            FrontendSessionManager = _frontEndSessionManagerMock.Object,
            Logger = _loggerMock.Object,
            FeatureManager = _featureManagerMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            GlobalVariables = Options.Create(new GlobalVariables()),
            RegistrationPeriodProvider = _mockRegistrationPeriodProvider.Object
        };

        Assert.DoesNotThrow(() => new RegistrationApplicationService(deps, _dateTimeProvider));
    }

    private RegistrationWindow CreateRegistrationWindow(
        WindowType windowType,
        int? registrationYear = 2026,
        DateTime? openingDateOffset = null,
        DateTime? deadlineDateOffset = null,
        DateTime? closingDateOffset = null)
    {
        var opening = openingDateOffset ?? new DateTime(2026, 6, 1);
        var deadline = deadlineDateOffset ?? new DateTime(2026, 7, 1);
        var closing = closingDateOffset ?? new DateTime(2026, 8, 1);
        return new RegistrationWindow(_dateTimeProvider, windowType, registrationYear.GetValueOrDefault(2026),
                opening, deadline, closing);
    }

}

internal static class ClaimsPrincipalExtensions
{
    public static void SetupClaimsPrincipal(this ClaimsPrincipal user, UserData userData)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata", JsonConvert.SerializeObject(userData)));
        user.AddIdentity(identity);
    }
}


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
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Services;

[TestFixture]
public class RegistrationApplicationServiceTests
{
    private readonly ISession _httpSession = new Mock<ISession>().Object;

    private Mock<ISubmissionService> _submissionServiceMock;
    private Mock<IPaymentCalculationService> _paymentCalculationServiceMock;
    private Mock<ISessionManager<RegistrationApplicationSession>> _sessionManagerMock;
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _frontEndSessionManagerMock;
    private Mock<IRegistrationApplicationService> _registrationApplicationServiceMock;
    private Fixture _fixture;
    private RegistrationApplicationService _service;

    private readonly RegistrationApplicationSession _session = new RegistrationApplicationSession
    {
        SubmissionId = Guid.NewGuid(),
        LastSubmittedFile = new LastSubmittedFileDetails
        {
            FileId = Guid.NewGuid(),
            SubmittedByName = "John Doe",
            SubmittedDateTime = DateTime.Now
        }
    };

    private Mock<ILogger<RegistrationApplicationService>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _paymentCalculationServiceMock = new Mock<IPaymentCalculationService>();
        _sessionManagerMock = new Mock<ISessionManager<RegistrationApplicationSession>>();
        _frontEndSessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _registrationApplicationServiceMock = new Mock<IRegistrationApplicationService>();
        _loggerMock = new Mock<ILogger<RegistrationApplicationService>>();
        var globalVariables = Options.Create(new GlobalVariables { LateFeeDeadline2025 = new(DateTime.Today.Year, 4, 1), RegistrationYear = $"{DateTime.Now.Year}, {DateTime.Now.AddYears(1).Year}" });

        _fixture = new Fixture();
        _service = new RegistrationApplicationService(_submissionServiceMock.Object, _paymentCalculationServiceMock.Object, _sessionManagerMock.Object, _frontEndSessionManagerMock.Object, _loggerMock.Object, globalVariables);

        //this is wrong needs fixing 
        var submissionYear = DateTime.Now.Year.ToString();
        _session.Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" };
        _session.SubmissionPeriod = _session.Period.DataPeriod;
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
    public async Task GetComplianceSchemeRegistrationFees_ShouldUserIsNewJoiner_For_LateFee_WhenPaymentCalculationServiceReturnsResponse()
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
    public async Task GetComplianceSchemeRegistrationFees_ShouldUserIsLateFeeApplicable_For_LateFee_When_IsResubmission_False_PaymentCalculationServiceReturnsResponse()
    {
        // Arrange
        var session = _fixture.Build<RegistrationApplicationSession>()
            .Create();

        session.IsLateFeeApplicable = true;
        session.IsOriginalCsoSubmissionLate = false;
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
    public async Task GetComplianceSchemeRegistrationFees_ShouldUserIsOriginalCsoSubmissionLate_For_LateFee_When_PaymentCalculationServiceReturnsResponse()
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

        // Act
        await _service.SetRegistrationFileUploadSession(_httpSession, organisationNumber, It.IsAny<int>(), false);

        // Assert
        _frontEndSessionManagerMock.Verify(
            m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                session.RegistrationSession.ApplicationReferenceNumber == "PEPR12325P1")),
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
        var submissionYear = DateTime.Now.AddYears(-1).Year.ToString();
        _session.Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" };

        // Act
        await _service.SetRegistrationFileUploadSession(_httpSession, organisationNumber, It.IsAny<int>(), false);

        // Assert
        _frontEndSessionManagerMock.Verify(
            m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                session.RegistrationSession.ApplicationReferenceNumber == "PEPR12324P2")),
            Times.Once);
    }

    [Test]
    public async Task CreateApplicationReferenceNumber_ShouldCreateNewSession_WhenNoSessionExists()
    {
        // Arrange
        var organisationNumber = "123";
        _session.SelectedComplianceScheme = null!;

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

        var periodEnd = DateTime.Parse($"30 {_session.Period.EndMonth} {_session.Period.Year}", new CultureInfo("en-GB"));
        var periodNumber = DateTime.Today <= periodEnd ? 1 : 2;
        var applicationReferenceNumber = $"PEPR{organisationNumber}{(periodEnd.Year - 2000)}P{periodNumber}";

        // Assert
        _frontEndSessionManagerMock.Verify(
            m => m.SaveSessionAsync(_httpSession, It.Is<FrontendSchemeRegistrationSession>(session =>
                session.RegistrationSession.ApplicationReferenceNumber == applicationReferenceNumber)),
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
        var applicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        applicationDetails.Should().NotBeNull();
        applicationDetails.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        applicationDetails.ApplicationReferenceNumber.Should().Be(registrationApplicationDetails.ApplicationReferenceNumber);
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
        var applicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

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
        var applicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        applicationDetails.Should().NotBeNull();
        applicationDetails.RegulatorNation.Should().Be("GB-ENG");
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ShouldPopulate_Default_LateFee()
    {
        // Arrange
        var globalVariables = Options.Create(new GlobalVariables { LateFeeDeadline2025 = new(DateTime.Today.Year, 4, 1) });

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
        _service = new RegistrationApplicationService(_submissionServiceMock.Object, _paymentCalculationServiceMock.Object, _sessionManagerMock.Object, _frontEndSessionManagerMock.Object, _loggerMock.Object, globalVariables);

        var applicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        applicationDetails.Should().NotBeNull();
        _submissionServiceMock.Verify(s => s.GetRegistrationApplicationDetails(It.Is<GetRegistrationApplicationDetailsRequest>(r => r.LateFeeDeadline == new DateTime(DateTime.Today.Year, 4, 1))));
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
        var registrationApplicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        registrationApplicationDetails.Should().NotBeNull();
        registrationApplicationDetails.SubmissionId.Should().BeNull();
        registrationApplicationDetails.SelectedComplianceScheme.Should().NotBeNull();
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ShouldCall_GetProducerRegistrationFees_When_File_Upload_Is_Completed_And_Payment_Is_Not_Completed()
    {
        // Arrange
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        var submissionYear = DateTime.Now.Year.ToString();
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = "January to December 2025",
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = null,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-ENG",
            ApplicationReferenceNumber = "",
            TotalAmountOutstanding = 10
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), false, It.IsAny<SubmissionType>()), Times.Never);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_For_CSO_ShouldCall_GetComplianceSchemeRegistrationFees_When_File_Upload_Is_Completed_And_Payment_Is_Not_Completed()
    {
        // Arrange
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        var submissionYear = DateTime.Now.Year.ToString();
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = "January to December 2025",
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
            TotalAmountOutstanding = 10
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), false, It.IsAny<SubmissionType>()), Times.Never);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ShouldCall_SubmitRegistrationApplication_When_Outstanding_Payment_Amount_Is_Zero()
    {
        // Arrange
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        var submissionYear = DateTime.Now.Year.ToString();
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = "January to December 2025",
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = "No-Outstanding-Payment",
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-ENG",
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test"
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), "No-Outstanding-Payment", "Test", false, SubmissionType.RegistrationFeePayment), Times.Once);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_For_CSO_ShouldCall_SubmitRegistrationApplication_When_Outstanding_Payment_Amount_Is_Zero()
    {
        // Arrange
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        var submissionYear = DateTime.Now.Year.ToString();
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = "January to December 2025",
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = "No-Outstanding-Payment",
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-NIR",
            SelectedComplianceScheme = cso,
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test"
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), "No-Outstanding-Payment", It.IsAny<string>(), false, SubmissionType.RegistrationFeePayment), Times.Once);
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
            IsLateFeeApplicable = false
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, true);

        // Assert
        var submissionYear = DateTime.Now.Year.ToString();
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = "January to December 2025",
            SubmissionId = submissionId,
            IsResubmission = true,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegulatorNation = "GB-ENG",
            ApplicationReferenceNumber = ""
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), false, It.IsAny<SubmissionType>()), Times.Never);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_ReSubmission_Does_Not_Reset_Status_On_Second_Pass_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
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
            RegistrationApplicationSubmittedComment = "Test",
            IsLateFeeApplicable = false
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year, true);

        // Assert
        var submissionYear = DateTime.Now.Year.ToString();
        result.Should().BeEquivalentTo(new RegistrationApplicationSession
        {
            Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" },
            SubmissionPeriod = "January to December 2025",
            SubmissionId = submissionId,
            IsSubmitted = true,
            IsResubmission = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeeCalculationDetails = feeCalculationDetails,
            LastSubmittedFile = lastSubmittedFile,
            RegulatorNation = "GB-ENG",
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = null,
            RegistrationFeePaymentMethod = "Online",
            RegistrationApplicationSubmittedDate = DateTime.Now.Date,
            RegistrationApplicationSubmittedComment = "Test",
            TotalAmountOutstanding = 100
        });

        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), false, It.IsAny<SubmissionType>()), Times.Never);
    }

    [Test]
    public async Task GetRegistrationApplicationSession_Producer_Should_Get_NationID_From_File_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
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
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

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
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

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
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        session.Should().NotBeNull();
        session.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        session.ApplicationReferenceNumber.Should().Be(registrationApplicationDetails.ApplicationReferenceNumber);
        session.RegulatorNation.Should().Be("GB-ENG");
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
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        session.Should().NotBeNull();
        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<SubmissionType>()), Times.Once);
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
        _session.SelectedComplianceScheme = null;

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
        var session = await _service.GetRegistrationApplicationSession(_httpSession, organisation, DateTime.Now.Year);

        // Assert
        session.Should().NotBeNull();
        _submissionServiceMock.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<SubmissionType>()), Times.Once);
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
            _session.SubmissionId.Value,
            complianceSchemeId,
            comments,
            paymentMethod,
            _session.ApplicationReferenceNumber, false,
            submissionType
        ), Times.Once);
    }

    [Test]
    public async Task SubmitRegistrationApplication_ShouldSaveFeePaymentMethod()
    {
        // Arrange
        var feePaymentMethod = _fixture.Create<string>();
        var submissionType = _fixture.Create<SubmissionType>();
        var complianceSchemeId = _session.SelectedComplianceScheme?.Id;
        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        // Act
        await _service.CreateRegistrationApplicationEvent(_httpSession, null, feePaymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.CreateRegistrationApplicationEvent(
            _session.SubmissionId.Value,
            complianceSchemeId,
            null,
            feePaymentMethod,
            _session.ApplicationReferenceNumber, false,
            submissionType
        ), Times.Once);

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
            _session.SubmissionId.Value,
            complianceSchemeId,
            comments,
            null,
            _session.ApplicationReferenceNumber, false,
            submissionType
        ), Times.Once);

        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(It.IsAny<ISession>(), It.Is<RegistrationApplicationSession>(s => s.RegistrationApplicationSubmittedDate.Value > DateTime.Now.AddSeconds(-5))), Times.Once);
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
            _session.SubmissionId.Value,
            complianceSchemeId,
            null,
            paymentMethod,
            _session.ApplicationReferenceNumber, false,
            submissionType
        ), Times.Once);
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
            _session.SubmissionId.Value,
            complianceSchemeId,
            comments,
            null,
            _session.ApplicationReferenceNumber, false,
            submissionType
        ), Times.Once);
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
            _session.SubmissionId.Value,
            complianceSchemeId,
            comments,
            paymentMethod,
            _session.ApplicationReferenceNumber, false,
            submissionType
        ), Times.Once);
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
    public async Task BuildRegistrationApplicationPerYearViewModels_ShouldReturnApplicationSessions()
    {
        //Arrange
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            OrganisationNumber = "552555"
        };

        var registrationApplicationSession = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = null,
            ApplicationReferenceNumber = "",
            RegistrationReferenceNumber = "",
            SubmissionId = Guid.NewGuid(),
            RegistrationFeePaymentMethod = null,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };
        _registrationApplicationServiceMock.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationSession);

        // Act
        var applicationPerYearViewModel = await _service.BuildRegistrationApplicationPerYearViewModels(_httpSession, organisation);

        //Assert
        applicationPerYearViewModel.Should().NotBeNull();
        applicationPerYearViewModel.Where(vm => vm.RegistrationYear == DateTime.Now.Year.ToString()).Should().NotBeNull();
        applicationPerYearViewModel.Where(vm => vm.RegistrationYear == DateTime.Now.AddYears(1).Year.ToString()).Should().NotBeNull();
    }

    [Test]
    public async Task ValidateRegistrationYear_ShouldReturnNull_WhenYearIsEmpty_AndParamOptionalIsTrue()
    {
        // Act
        var result = await _service.validateRegistrationYear("", true);

        // Assert
        result.Should().BeNull();

    }

    [Test]
    public async Task ValidateRegistrationYear_ShouldThrowArgumentException_WhenYearIsEmpty_AndParamOptionalIsFalse()
    {
        var act = async () => await _service.validateRegistrationYear("", false);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Registration year missing");
    }

    [Test]
    public async Task ValidateRegistrationYear_ShouldThrowArgumentException_WhenYearIsNotANumber()
    {
        var act = async () => await _service.validateRegistrationYear("abcd", false);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Registration year is not a valid number");
    }

    [Test]
    public async Task ValidateRegistrationYear_ShouldReturnYear_WhenValid()
    {
        var result = await _service.validateRegistrationYear("2025", false);
        result.Should().Be(2025);
    }

}

internal static class ClaimsPrincipalExtensions
{
    public static void SetupClaimsPrincipal(this ClaimsPrincipal user, UserData userData)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata", Newtonsoft.Json.JsonConvert.SerializeObject(userData)));
        user.AddIdentity(identity);
    }
}
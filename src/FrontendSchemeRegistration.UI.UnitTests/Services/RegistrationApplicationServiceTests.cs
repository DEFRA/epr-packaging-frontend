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

    [SetUp]
    public void Setup()
    {
        _submissionServiceMock = new Mock<ISubmissionService>();
        _paymentCalculationServiceMock = new Mock<IPaymentCalculationService>();
        _sessionManagerMock = new Mock<ISessionManager<RegistrationApplicationSession>>();
        _frontEndSessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        var globalVariables = Options.Create(new GlobalVariables { LateFeeDeadline = DateTime.Today });

        _fixture = new Fixture();
        _service = new RegistrationApplicationService(_submissionServiceMock.Object, _paymentCalculationServiceMock.Object, _sessionManagerMock.Object, _frontEndSessionManagerMock.Object, globalVariables);

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

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetProducerRegistrationFees(It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(paymentCalculationResponse);

        // Act
        await _service.GetProducerRegistrationFees(_httpSession);

        // Assert
        _session.TotalAmountOutstanding.Should().Be(paymentCalculationResponse.OutstandingPayment);
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

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(session);
        _paymentCalculationServiceMock.Setup(pcs => pcs.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        await _service.GetComplianceSchemeRegistrationFees(_httpSession);

        // Assert
        session.TotalAmountOutstanding.Should().Be(response.OutstandingPayment);
        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(_httpSession, session), Times.Once);
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
            new SubmissionPeriod { StartMonth = "April", EndMonth = "September", Year = "2025" }
        ];

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        foreach (var period in submissionPeriods)
        {
            _session.Period = period;
            _session.IsComplianceScheme = isComplianceScheme;
            _session.SelectedComplianceScheme = new ComplianceSchemeDto { RowNumber = csRowNumber };

            // Act
            await _service.CreateApplicationReferenceNumber(_httpSession, organisationId);

            switch (int.Parse(period.Year))
            {
                // Assert
                case 2024:
                    _session.ApplicationReferenceNumber.Should().EndWith("P2");
                    break;
                case 2025:
                    _session.ApplicationReferenceNumber.Should().EndWith("P1");
                    break;
            }

            _session.ApplicationReferenceNumber.Should().Contain(organisationId);


            if (isComplianceScheme)
            {
                _session.ApplicationReferenceNumber.Should().Contain(csRowNumber.ToString());
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

        _submissionServiceMock.Setup(ss => ss.SubmitAsync(
            _session.SubmissionId.Value,
            _session.LastSubmittedFile.FileId.Value,
            _session.LastSubmittedFile.SubmittedByName,
            It.IsAny<string>()
        )).Verifiable();

        // Act
        await _service.CreateApplicationReferenceNumber(_httpSession, organisationNumber);

        // Assert
        _session.ApplicationReferenceNumber.Should().Be("PEPR12325P1");
        _submissionServiceMock.Verify();
        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(_httpSession, _session), Times.Once);
    }

    [Test]
    public async Task CreateApplicationReferenceNumber_ShouldUsePeriodNumber2_WhenCurrentDateIsAfterPeriodEnd()
    {
        // Arrange
        var organisationNumber = "123";

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        //this is wrong needs fixing 
        var submissionYear = DateTime.Now.AddYears(-1).Year.ToString();
        _session.Period = new SubmissionPeriod { DataPeriod = $"January to December {submissionYear}", StartMonth = "January", EndMonth = "December", Year = $"{submissionYear}" };

        _submissionServiceMock.Setup(ss => ss.SubmitAsync(
            _session.SubmissionId.Value,
            _session.LastSubmittedFile.FileId.Value,
            _session.LastSubmittedFile.SubmittedByName,
            It.IsAny<string>()
        )).Verifiable();

        // Act
        await _service.CreateApplicationReferenceNumber(_httpSession, organisationNumber);

        // Assert
        _session.ApplicationReferenceNumber.Should().Be("PEPR12324P2");
        _submissionServiceMock.Verify();
        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(_httpSession, _session), Times.Once);
    }

    [Test]
    public async Task CreateApplicationReferenceNumber_ShouldCreateNewSession_WhenNoSessionExists()
    {
        // Arrange
        var organisationNumber = "123";
        _session.IsComplianceScheme = false;
        _session.SelectedComplianceScheme = null!;

        _sessionManagerMock.Setup(sm => sm.GetSessionAsync(_httpSession))
            .ReturnsAsync(_session);

        _submissionServiceMock.Setup(ss => ss.SubmitAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).Verifiable();

        // Act
        await _service.CreateApplicationReferenceNumber(_httpSession, organisationNumber);

        var periodEnd = DateTime.Parse($"30 {_session.Period.EndMonth} {_session.Period.Year}", new CultureInfo("en-GB"));
        var periodNumber = DateTime.Today <= periodEnd ? 1 : 2;
        var applicationReferenceNumber = $"PEPR{organisationNumber}{(periodEnd.Year - 2000)}P{periodNumber}";

        // Assert
        _submissionServiceMock.Verify();
        _sessionManagerMock.Verify(sm => sm.SaveSessionAsync(_httpSession, It.Is<RegistrationApplicationSession>(s => s.ApplicationReferenceNumber == applicationReferenceNumber)), Times.Once);
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
        var applicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation);

        // Assert
        applicationDetails.Should().NotBeNull();
        applicationDetails.SubmissionId.Should().Be(registrationApplicationDetails.SubmissionId);
        applicationDetails.ApplicationReferenceNumber.Should().Be(registrationApplicationDetails.ApplicationReferenceNumber);
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
        var registrationApplicationDetails = await _service.GetRegistrationApplicationSession(_httpSession, organisation);

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
            .ReturnsAsync(new PaymentCalculationResponse { TotalFee = 10, PreviousPayment = 0, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown() })
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation);

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
            ApplicationReferenceNumber = "",
        });

        _submissionServiceMock.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);
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
            .ReturnsAsync(new ComplianceSchemePaymentCalculationResponse { TotalFee = 10, PreviousPayment = 0 })
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation);

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
            SelectedComplianceScheme = cso
        });

        _submissionServiceMock.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);
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
            .ReturnsAsync(new PaymentCalculationResponse { TotalFee = 0, PreviousPayment = 0, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown() })
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation);

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

        _submissionServiceMock.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), "No-Outstanding-Payment", "Test", SubmissionType.RegistrationFeePayment), Times.Once);
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
            .ReturnsAsync(new ComplianceSchemePaymentCalculationResponse { TotalFee = 0, PreviousPayment = 0, ComplianceSchemeMembersWithFees  = [new ComplianceSchemePaymentCalculationResponseMember{MemberId = organisation.OrganisationNumber, MemberRegistrationFee = 10, SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown(), SubsidiariesFee = 10, MemberLateRegistrationFee = 100, MemberOnlineMarketPlaceFee = 1, TotalMemberFee = 1}],  })
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
                IsComplianceScheme = true
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
        var result = await _service.GetRegistrationApplicationSession(_httpSession, organisation);

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
            IsComplianceScheme = true,
            ApplicationReferenceNumber = "Test",
            RegistrationReferenceNumber = "Test"
        });

        _submissionServiceMock.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), "No-Outstanding-Payment", It.IsAny<string>(), SubmissionType.RegistrationFeePayment), Times.Once);
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
        await _service.SubmitRegistrationApplication(_httpSession, comments, paymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.SubmitRegistrationApplicationAsync(
            _session.SubmissionId.Value,
            complianceSchemeId,
            comments,
            paymentMethod,
            _session.ApplicationReferenceNumber,
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
        await _service.SubmitRegistrationApplication(_httpSession, null, feePaymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.SubmitRegistrationApplicationAsync(
            _session.SubmissionId.Value,
            complianceSchemeId,
            null,
            feePaymentMethod,
            _session.ApplicationReferenceNumber,
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
        await _service.SubmitRegistrationApplication(_httpSession, comments, null, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.SubmitRegistrationApplicationAsync(
            _session.SubmissionId.Value,
            complianceSchemeId,
            comments,
            null,
            _session.ApplicationReferenceNumber,
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
        await _service.SubmitRegistrationApplication(_httpSession, null, paymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.SubmitRegistrationApplicationAsync(
            _session.SubmissionId.Value,
            complianceSchemeId,
            null,
            paymentMethod,
            _session.ApplicationReferenceNumber,
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
        await _service.SubmitRegistrationApplication(_httpSession, comments, null, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.SubmitRegistrationApplicationAsync(
            _session.SubmissionId.Value,
            complianceSchemeId,
            comments,
            null,
            _session.ApplicationReferenceNumber,
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
        await _service.SubmitRegistrationApplication(_httpSession, comments, paymentMethod, submissionType);

        // Assert
        _submissionServiceMock.Verify(ss => ss.SubmitRegistrationApplicationAsync(
            _session.SubmissionId.Value,
            complianceSchemeId,
            comments,
            paymentMethod,
            _session.ApplicationReferenceNumber,
            submissionType
        ), Times.Once);
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
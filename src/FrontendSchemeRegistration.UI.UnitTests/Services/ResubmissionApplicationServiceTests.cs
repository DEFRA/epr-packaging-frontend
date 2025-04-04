using AutoFixture;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Globalization;
using System.Security.Claims;

namespace FrontendSchemeRegistration.UI.UnitTests.Services;

[TestFixture]
public class ResubmissionApplicationServiceTests
{
    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _mockSessionManager;
    private Mock<ISubmissionService> _mockSubmissionService;
	private Mock<IPaymentCalculationService> _mockPaymentCalculationService;
    private ResubmissionApplicationServices _service;
    private Fixture _fixture;

    private readonly FrontendSchemeRegistrationSession _session = new FrontendSchemeRegistrationSession
    {
        PomResubmissionSession = new PackagingReSubmissionSession
        {
            RegulatorNation = "GB-EN",
            PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails
                {
                    FileId = Guid.NewGuid(),
                    SubmittedByName = "John Doe",
                    SubmittedDateTime = DateTime.Now
                }
            }
        }
    };


    [SetUp]
    public void SetUp()
    {
        _mockSessionManager = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _mockSubmissionService = new Mock<ISubmissionService>();
        _mockPaymentCalculationService = new Mock<IPaymentCalculationService>();
        _fixture = new Fixture();
        _service = new ResubmissionApplicationServices(_mockSessionManager.Object, _mockPaymentCalculationService.Object, _mockSubmissionService.Object);
    }

    [Test]
    public async Task CreatePomResubmissionReferenceNumberProducer_GeneratesCorrectReferenceNumber()
    {
        // Arrange
        var httpSession = Mock.Of<ISession>();
        var submissionPeriod = new SubmissionPeriod { EndMonth = "March", Year = "2025", DataPeriod = "January to June 2025" };
        var organisationNumber = "12345";
        var submittedByName = "Test User";
        var submissionId = Guid.NewGuid();

        var mockPomSubmission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            LastSubmittedFile = new SubmittedFileInformation { FileId = Guid.NewGuid() },
            IsSubmitted = true
        };

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { mockPomSubmission },
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                }
            }
        };

        _mockSubmissionService
            .Setup(x => x.SubmitAsync(mockPomSubmission.Id, mockPomSubmission.LastSubmittedFile.FileId, submittedByName, It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);

        _mockSessionManager
            .Setup(x => x.SaveSessionAsync(httpSession, session))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreatePomResubmissionReferenceNumberForProducer(session, submissionPeriod, organisationNumber, submittedByName, submissionId);

        // Assert
        var periodEnd = DateTime.Parse("30 March 2025", new CultureInfo("en-GB"));
        var expectedReferenceNumbers = new List<KeyValuePair<string, string>>();
        var expectedReferenceNumber = new KeyValuePair<string, string>("January to June 2025", $"PEPR1234525S02");
        expectedReferenceNumbers.Add(expectedReferenceNumber);
        session.PomResubmissionSession.PomResubmissionReferences.FirstOrDefault(kvp => kvp.Key == submissionPeriod.DataPeriod).Value.Should().Be(expectedReferenceNumber.Value);
    }

    [Test]
    public async Task CreatePomResubmissionReferenceNumberProducer_MultipleSubmissions_IncrementsResubmissionCount()
    {
        // Arrange
        var httpSession = Mock.Of<ISession>();
        var submissionPeriod = new SubmissionPeriod { EndMonth = "April", Year = "2025", DataPeriod = "January to June 2025" };
        var organisationNumber = "67890";
        var submittedByName = "Test User1";
		var submissionId = Guid.NewGuid();

		var mockPomSubmissions = new List<PomSubmission>
        {
            new PomSubmission { Id = Guid.NewGuid(), LastSubmittedFile = new SubmittedFileInformation { FileId = Guid.NewGuid() }, IsSubmitted = true },
            new PomSubmission { Id = Guid.NewGuid(), LastSubmittedFile = new SubmittedFileInformation { FileId = Guid.NewGuid() } }
        };

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = mockPomSubmissions,
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                }
            },
        };

        _mockSubmissionService
            .Setup(x => x.SubmitAsync(mockPomSubmissions.First().Id, mockPomSubmissions.First().LastSubmittedFile.FileId, submittedByName, It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);

        _mockSessionManager
            .Setup(x => x.SaveSessionAsync(httpSession, session))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreatePomResubmissionReferenceNumberForProducer(session, submissionPeriod, organisationNumber, submittedByName, submissionId);

        // Assert
        var periodEnd = DateTime.Parse("30 April 2025", new CultureInfo("en-GB"));
        var expectedReferenceNumbers = new List<KeyValuePair<string, string>>();
        var expectedReferenceNumber = new KeyValuePair<string, string>("January to June 2025", $"PEPR6789025S02");
        expectedReferenceNumbers.Add(expectedReferenceNumber);
        session.PomResubmissionSession.PomResubmissionReferences.FirstOrDefault(kvp => kvp.Key == submissionPeriod.DataPeriod).Value.Should().Be(expectedReferenceNumber.Value);
    }

    [Test]
    public async Task CreatePomResubmissionReferenceNumberForCSO_GeneratesCorrectReferenceNumber_JanToJune()
    {
        // Arrange
        var httpSession = Mock.Of<ISession>();
        var submissionPeriod = new SubmissionPeriod { StartMonth = "January" , EndMonth = "June", Year = "2025", DataPeriod = "January to June 2025" };
        var organisationNumber = "12345";
        var submittedByName = "Test User";
        var submissionId = Guid.NewGuid();

        var mockPomSubmission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            LastSubmittedFile = new SubmittedFileInformation { FileId = Guid.NewGuid() },
            IsSubmitted = true
        };

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { mockPomSubmission },
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                }
            },
            RegistrationSession=new RegistrationSession { SelectedComplianceScheme = new Application.DTOs.ComplianceScheme.ComplianceSchemeDto { RowNumber = 6 } }
        };

        _mockSubmissionService
            .Setup(x => x.SubmitAsync(mockPomSubmission.Id, mockPomSubmission.LastSubmittedFile.FileId, submittedByName, It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);

        _mockSessionManager
            .Setup(x => x.SaveSessionAsync(httpSession, session))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreatePomResubmissionReferenceNumberForCSO(session, submissionPeriod, organisationNumber, submittedByName, submissionId);

        // Assert
        var expectedReferenceNumbers = new List<KeyValuePair<string, string>>();
        var expectedReferenceNumber = new KeyValuePair<string, string>("January to June 2025", $"PEPR12345625S01");
        expectedReferenceNumbers.Add(expectedReferenceNumber);
        session.PomResubmissionSession.PomResubmissionReferences.FirstOrDefault(kvp => kvp.Key == submissionPeriod.DataPeriod).Value.Should().Be(expectedReferenceNumber.Value);
    }

    [Test]
    public async Task CreatePomResubmissionReferenceNumberForCSO_GeneratesCorrectReferenceNumber_JulToDec()
    {
        // Arrange
        var httpSession = Mock.Of<ISession>();
        var submissionPeriod = new SubmissionPeriod { StartMonth = "July", EndMonth = "December", Year = "2025", DataPeriod = "July to December 2025" };
        var organisationNumber = "12345";
        var submittedByName = "Test User";
        var submissionId = Guid.NewGuid();

        var mockPomSubmission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            LastSubmittedFile = new SubmittedFileInformation { FileId = Guid.NewGuid() },
            IsSubmitted = true
        };

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { mockPomSubmission },
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                }
            },
            RegistrationSession = new RegistrationSession { SelectedComplianceScheme = new Application.DTOs.ComplianceScheme.ComplianceSchemeDto { RowNumber = 6 } }
        };

        _mockSubmissionService
            .Setup(x => x.SubmitAsync(mockPomSubmission.Id, mockPomSubmission.LastSubmittedFile.FileId, submittedByName, It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);

        _mockSessionManager
            .Setup(x => x.SaveSessionAsync(httpSession, session))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreatePomResubmissionReferenceNumberForCSO(session, submissionPeriod, organisationNumber, submittedByName, submissionId);

        // Assert
        var expectedReferenceNumbers = new List<KeyValuePair<string, string>>();
        var expectedReferenceNumber = new KeyValuePair<string, string>("July to December 2025", $"PEPR12345625S02");
        expectedReferenceNumbers.Add(expectedReferenceNumber);
        session.PomResubmissionSession.PomResubmissionReferences.FirstOrDefault(kvp => kvp.Key == submissionPeriod.DataPeriod).Value.Should().Be(expectedReferenceNumber.Value);
    }

    [Test]
    public async Task CreatePomResubmissionReferenceNumber_ShouldCallCSOMethod_WhenOrganisationIsComplianceScheme()
    {
        // Arrange
        var httpSession = Mock.Of<ISession>();
        var submissionPeriod = new SubmissionPeriod { StartMonth = "July", EndMonth = "December", Year = "2025", DataPeriod = "July to December 2025" };
        var organisationNumber = "12345";
        var submittedByName = "Test User";
        var submissionId = Guid.NewGuid();

        var mockPomSubmission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            LastSubmittedFile = new SubmittedFileInformation { FileId = Guid.NewGuid() },
            IsSubmitted = true
        };

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { mockPomSubmission },
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                    Organisation = new Organisation { OrganisationRole = OrganisationRoles.ComplianceScheme, OrganisationNumber = "CSO123" }
                },
                Period = submissionPeriod
            },
            RegistrationSession = new RegistrationSession { SelectedComplianceScheme = new Application.DTOs.ComplianceScheme.ComplianceSchemeDto { RowNumber = 6 } }
        };

        _mockSubmissionService
            .Setup(x => x.SubmitAsync(mockPomSubmission.Id, mockPomSubmission.LastSubmittedFile.FileId, submittedByName, It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);

        _mockSessionManager
            .Setup(x => x.SaveSessionAsync(httpSession, session))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePomResubmissionReferenceNumber(session, submittedByName, submissionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("PEPRCSO123625S02");
    }

    [Test]
    public async Task CreatePomResubmissionReferenceNumber_ShouldCallProducerMethod_WhenOrganisationIsNotComplianceScheme()
    {
        // Arrange
        var httpSession = Mock.Of<ISession>();
        var submissionPeriod = new SubmissionPeriod { EndMonth = "March", Year = "2025", DataPeriod = "January to June 2025" };
        var organisationNumber = "12345";
        var submittedByName = "Test User";
        var submissionId = Guid.NewGuid();

        var mockPomSubmission = new PomSubmission
        {
            Id = Guid.NewGuid(),
            LastSubmittedFile = new SubmittedFileInformation { FileId = Guid.NewGuid() },
            IsSubmitted = true
        };

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { mockPomSubmission },
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                    Organisation = new Organisation { OrganisationRole = OrganisationRoles.Producer, OrganisationNumber = "PRD123" }
                },
                Period = submissionPeriod
            }
        };

        _mockSubmissionService
            .Setup(x => x.SubmitAsync(mockPomSubmission.Id, mockPomSubmission.LastSubmittedFile.FileId, submittedByName, It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);

        _mockSessionManager
            .Setup(x => x.SaveSessionAsync(httpSession, session))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePomResubmissionReferenceNumber(session, submittedByName, submissionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("PEPRPRD12325S02");
    }

    [Test]
    public async Task InitiatePayment_ShouldReturnPaymentId_WhenPaymentInitiationSucceeds()
    {
        // Arrange
        var _httpSession = Mock.Of<ISession>();
        var user = _fixture.Create<ClaimsPrincipal>();
        _session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding = 100;
        _session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber = "REF123";
        _session.PomResubmissionSession.RegulatorNation = "GB-EN";

        var userData = new UserData
        {
            Id = Guid.NewGuid(),
            Organisations = [new Organisation { Id = Guid.NewGuid() }]
        };

        var paymentId = _fixture.Create<string>();

        _mockPaymentCalculationService.Setup(pcs => pcs.InitiatePayment(It.IsAny<PaymentInitiationRequest>()))
            .ReturnsAsync(paymentId);

        _mockSessionManager.Setup(sm => sm.GetSessionAsync(_httpSession)).ReturnsAsync(_session);

        user.SetupClaimsPrincipal(userData);

        // Act
        var result = await _service.InitiatePayment(user, _httpSession);

        // Assert
        result.Should().Be(paymentId);
        _mockPaymentCalculationService.Verify(pcs => pcs.InitiatePayment(It.Is<PaymentInitiationRequest>(r =>
            r.UserId == userData.Id.Value &&
            r.OrganisationId == userData.Organisations.First().Id.Value &&
            r.Reference == _session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber &&
            r.Amount == _session.PomResubmissionSession.FeeBreakdownDetails.TotalAmountOutstanding &&
            r.Regulator == _session.PomResubmissionSession.RegulatorNation
        )), Times.Once);
    }

    [Test]
    public void InitiatePayment_ShouldThrowException_WhenUserHasNoOrganisations()
    {
        // Arrange
        var _httpSession = Mock.Of<ISession>();
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
        var _httpSession = Mock.Of<ISession>();
        var user = _fixture.Create<ClaimsPrincipal>();
        _session.PomResubmissionSession.PackagingResubmissionApplicationSession.ApplicationReferenceNumber = null;

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
    public async Task GetPackagingDataResubmissionApplicationDetails_Return_List_Of_GetPackagingDataResubmissionApplicationDetailsObjects_WithMatchingParameters()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriods = new List<string> { "January to June 2024", "January to June 2025" };
        var complianceSchemeId = Guid.NewGuid();

        var getPackagingResubmissionApplicationDetailsRequest = new GetPackagingResubmissionApplicationDetailsRequest
        {
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = complianceSchemeId,
            SubmissionPeriods = submissionPeriods
        };

        var expectedResult = new List<PackagingResubmissionApplicationDetails>()
        {
            new PackagingResubmissionApplicationDetails { IsResubmitted = true, ApplicationReferenceNumber = "abc" }
        };

        _mockSubmissionService
            .Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<GetPackagingResubmissionApplicationDetailsRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetPackagingDataResubmissionApplicationDetails(organisation, submissionPeriods, complianceSchemeId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().ApplicationReferenceNumber.Should().Be("abc");
    }

    [Test]
    public async Task GetPackagingDataResubmissionApplicationDetails_Return_EmptyList_WithNonMatchingParameters()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriods = new List<string> { "January to June 2024", "January to June 2025" };
        var complianceSchemeId = Guid.NewGuid();

        var getPackagingResubmissionApplicationDetailsRequest = new GetPackagingResubmissionApplicationDetailsRequest
        {
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = complianceSchemeId,
            SubmissionPeriods = submissionPeriods
        };

        List<PackagingResubmissionApplicationDetails> expected = new List<PackagingResubmissionApplicationDetails>();

        _mockSubmissionService
            .Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<GetPackagingResubmissionApplicationDetailsRequest>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.GetPackagingDataResubmissionApplicationDetails(organisation, submissionPeriods, complianceSchemeId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
    }

    [Test]
    public async Task GetPackagingDataResubmissionApplicationDetails_Return_Null_WithInvalidParameters()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriods = new List<string> { "January to June 2024", "January to June 2025" };
        var complianceSchemeId = Guid.NewGuid();

        var getPackagingResubmissionApplicationDetailsRequest = new GetPackagingResubmissionApplicationDetailsRequest
        {
            OrganisationId = organisation.Id.Value,
            ComplianceSchemeId = complianceSchemeId,
            SubmissionPeriods = submissionPeriods
        };

        List<PackagingResubmissionApplicationDetails> expected = null;

        _mockSubmissionService
            .Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<GetPackagingResubmissionApplicationDetailsRequest>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.GetPackagingDataResubmissionApplicationDetails(organisation, submissionPeriods, complianceSchemeId);

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public async Task GetPackagingDataResubmissionApplicationDetails_Return_GetPackagingDataResubmissionApplicationDetailsObject_WithMatchingParameters()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriod = new List<string> { "January to June 2025" };
        var complianceSchemeId = Guid.NewGuid();

        var expectedResult = new List<PackagingResubmissionApplicationDetails>()
        {
            new PackagingResubmissionApplicationDetails { IsResubmitted = true, ApplicationReferenceNumber = "abc" }
        };

        _mockSubmissionService
            .Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<GetPackagingResubmissionApplicationDetailsRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetPackagingDataResubmissionApplicationDetails(organisation, submissionPeriod, complianceSchemeId);

        // Assert
        result.Should().NotBeNull();
        result.First().ApplicationReferenceNumber.Should().Be("abc");
    }

    [Test]
    public async Task GetPackagingDataResubmissionApplicationDetails_Return_EmptyObject_WithInvalidParameters()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriod = new List<string> { "January to June 2025" };
        var complianceSchemeId = Guid.NewGuid();

        List<PackagingResubmissionApplicationDetails> expected = null;

        _mockSubmissionService
            .Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<GetPackagingResubmissionApplicationDetailsRequest>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.GetPackagingDataResubmissionApplicationDetails(organisation, submissionPeriod, complianceSchemeId);

        // Assert
        result.Should().NotBeNull();
    }

    [Test]
    public async Task GetPackagingResubmissionApplicationSession_Return_ListOf_PackagingResubmissionApplicationSession_WithMatchingParameters()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriods = new List<string> { "January to June 2025" };
        var complianceSchemeId = Guid.NewGuid();

        var expectedResult = new List<PackagingResubmissionApplicationDetails>()
        {
            new PackagingResubmissionApplicationDetails { IsResubmitted = true, ApplicationReferenceNumber = "abc" }
        };

        _mockSubmissionService
            .Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<GetPackagingResubmissionApplicationDetailsRequest>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetPackagingResubmissionApplicationSession(organisation, submissionPeriods, complianceSchemeId);

        // Assert
        result.Should().NotBeNull();
        result.First().ApplicationReferenceNumber.Should().Be("abc");
    }

    [Test]
    public async Task GetPackagingResubmissionApplicationSession_Return_EmptyObject_WithInvalidParameters()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriods = new List<string> { "January to June 2025" };
        var complianceSchemeId = Guid.NewGuid();

        List<PackagingResubmissionApplicationDetails> expected = null;

        _mockSubmissionService
            .Setup(x => x.GetPackagingDataResubmissionApplicationDetails(It.IsAny<GetPackagingResubmissionApplicationDetailsRequest>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.GetPackagingResubmissionApplicationSession(organisation, submissionPeriods, complianceSchemeId);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
    }
}
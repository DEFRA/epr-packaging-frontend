using AutoFixture;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using EPR.SubmissionMicroservice.API.Contracts.Submissions.Get;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
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
    private Mock<IOptions<GlobalVariables>> _mockGlobalVariables;
    private Mock<IFeatureManager> _mockFeatureManager;
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

    private readonly List<SubmissionPeriod> _submissionPeriods = new()
    {
        new SubmissionPeriod
        {
            DataPeriod = "Data period 1",
            ActiveFrom = DateTime.Today,
            Deadline = DateTime.Parse("2023-12-31"),
            Year = "2023",
            StartMonth = "September",
            EndMonth = "December",
        },
        new SubmissionPeriod
        {
            DataPeriod = "Data period 2",
            Deadline = DateTime.Parse("2024-03-31"),
            ActiveFrom = DateTime.Today.AddDays(5),
            Year = "2024",
            StartMonth = "January",
            EndMonth = "March"
        },
        new SubmissionPeriod
        {
            DataPeriod = "January to June 2025",
            /* This will be excluded because it is after the latest allowed period ending June 2024 */
            Deadline = DateTime.Parse("2025-10-01"),
            ActiveFrom = DateTime.Parse("2025-07-01"),
            Year = "2025",
            StartMonth = "January",
            EndMonth = "June"
        }
    };


    [SetUp]
    public void SetUp()
    {
        _mockGlobalVariables = new Mock<IOptions<GlobalVariables>>();
        _mockGlobalVariables.Setup(o => o.Value).Returns(new GlobalVariables { BasePath = "path", SubmissionPeriods = _submissionPeriods });
        _mockFeatureManager = new Mock<IFeatureManager>();

        _mockSessionManager = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _mockSubmissionService = new Mock<ISubmissionService>();
        _mockPaymentCalculationService = new Mock<IPaymentCalculationService>();
        _fixture = new Fixture();
        _service = new ResubmissionApplicationServices(_mockSessionManager.Object, _mockPaymentCalculationService.Object, _mockSubmissionService.Object, _mockGlobalVariables.Object, _mockFeatureManager.Object);
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
        var response = await _service.CreatePomResubmissionReferenceNumberForProducer(session, submissionPeriod, organisationNumber, submittedByName, submissionId, 1);

        // Assert
        var periodEnd = DateTime.Parse("30 March 2025", new CultureInfo("en-GB"));
        var expectedReferenceNumbers = new List<KeyValuePair<string, string>>();
        var expectedReferenceNumber = new KeyValuePair<string, string>("January to June 2025", $"PEPR123452502S02");
        expectedReferenceNumbers.Add(expectedReferenceNumber);
        response.Should().Be(expectedReferenceNumber.Value);
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
        var response = await _service.CreatePomResubmissionReferenceNumberForProducer(session, submissionPeriod, organisationNumber, submittedByName, submissionId, 1);

        // Assert
        var periodEnd = DateTime.Parse("30 April 2025", new CultureInfo("en-GB"));
        var expectedReferenceNumbers = new List<KeyValuePair<string, string>>();
        var expectedReferenceNumber = new KeyValuePair<string, string>("January to June 2025", "PEPR678902502S02");
        expectedReferenceNumbers.Add(expectedReferenceNumber);
        response.Should().Be(expectedReferenceNumber.Value);
    }

    [Test]
    public async Task CreatePomResubmissionReferenceNumberForCSO_GeneratesCorrectReferenceNumber_JanToJune()
    {
        // Arrange
        var httpSession = Mock.Of<ISession>();
        var submissionPeriod = new SubmissionPeriod { StartMonth = "January", EndMonth = "June", Year = "2025", DataPeriod = "January to June 2025" };
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
                },
                RegulatorNation = "GB-ENG"
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
        var response = await _service.CreatePomResubmissionReferenceNumberForCSO(session, submissionPeriod, organisationNumber, submittedByName, submissionId, 1);

        // Assert
        var expectedReferenceNumbers = new List<KeyValuePair<string, string>>();
        var expectedReferenceNumber = new KeyValuePair<string, string>("January to June 2025", $"PEPR12345E250102");
        expectedReferenceNumbers.Add(expectedReferenceNumber);
        response.Should().Be(expectedReferenceNumber.Value);
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
                },
                RegulatorNation = "GB-ENG"
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
        var response = await _service.CreatePomResubmissionReferenceNumberForCSO(session, submissionPeriod, organisationNumber, submittedByName, submissionId, 1);

        // Assert
        var expectedReferenceNumbers = new List<KeyValuePair<string, string>>();
        var expectedReferenceNumber = new KeyValuePair<string, string>("July to December 2025", $"PEPR12345E250202");
        expectedReferenceNumbers.Add(expectedReferenceNumber);
        response.Should().Be(expectedReferenceNumber.Value);
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
                Period = submissionPeriod,
                RegulatorNation = "GB-ENG"
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
        var result = await _service.CreatePomResubmissionReferenceNumber(session, submittedByName, submissionId, 1);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("PEPRCSO123E250202");
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
                Period = submissionPeriod,
                RegulatorNation = "GB-ENG"
            }
        };

        _mockSubmissionService
            .Setup(x => x.SubmitAsync(mockPomSubmission.Id, mockPomSubmission.LastSubmittedFile.FileId, submittedByName, It.IsAny<string>(), null))
            .Returns(Task.CompletedTask);

        _mockSessionManager
            .Setup(x => x.SaveSessionAsync(httpSession, session))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePomResubmissionReferenceNumber(session, submittedByName, submissionId, 1);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("PEPRPRD1232502S02");
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

    [Test]
    public async Task GetSubmissionIds_Return_ListOf_SubmissionIds()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriods = new List<string> { "January to June 2025" };
        var complianceSchemeId = Guid.NewGuid();

        var expectedResult = new List<SubmissionPeriodId>()
        {
            new SubmissionPeriodId()
        };

        _mockSubmissionService
            .Setup(x => x.GetSubmissionIdsAsync(organisation.Id.Value, SubmissionType.Producer, null, null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetSubmissionIdsAsync(organisation.Id.Value, SubmissionType.Producer, null, null);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(expectedResult.Count);
    }

    [Test]
    public async Task GetSubmissionHistoryAsync_Return_ListOf_PreviousSubmissions()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriods = new List<string> { "January to June 2025" };
        var submssionId = Guid.NewGuid();

        var expectedResult = new List<SubmissionHistory>()
        {
            new SubmissionHistory()
        };

        _mockSubmissionService
            .Setup(x => x.GetSubmissionHistoryAsync(submssionId, new DateTime()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetSubmissionHistoryAsync(submssionId, new DateTime());

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(expectedResult.Count);
    }

    [Test]
    public async Task GetPackagingResubmissionMemberDetails_Return_MemberDetails()
    {
        // Arrange
        var organisation = new Organisation { Id = Guid.NewGuid(), OrganisationNumber = "12345" };
        var submissionPeriods = new List<string> { "January to June 2025" };
        var submssionId = Guid.NewGuid();

        var request = new PackagingResubmissionMemberRequest
        {
            SubmissionId = submssionId
        };
        var expectedResult = new PackagingResubmissionMemberDetails()
        {
            MemberCount = 1,
        };

        _mockSubmissionService
            .Setup(x => x.GetPackagingResubmissionMemberDetails(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetPackagingResubmissionMemberDetails(request);

        // Assert
        result.Should().NotBeNull();
        result.MemberCount.Should().Be(expectedResult.MemberCount);
    }

    [Test]
    public async Task GetResubmissionFees_Return_PackagingPaymentResponse()
    {
        // Arrange
        var expectedResult = new PackagingPaymentResponse();

        _mockPaymentCalculationService
            .Setup(x => x.GetResubmissionFees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetResubmissionFees(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<DateTime?>());

        // Assert
        result.Should().NotBeNull();
        result.MemberCount.Should().Be(expectedResult.MemberCount);
    }

    [Test]
    public async Task CreatePackagingResubmissionFeeViewEvent_Calls_SubmissionService_CreatePackagingResubmissionFeeViewEvent_Once()
    {
        // Arrange
        var submssionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        // Act
        await _service.CreatePackagingResubmissionFeeViewEvent(submssionId, fileId);

        // Assert
        _mockSubmissionService.Verify(x => x.CreatePackagingResubmissionFeeViewEvent(submssionId, fileId), Times.Once);
    }

    [Test]
    public async Task GetActiveSubmissionPeriod_ReturnsCorrectActiveDataPeriod()
    {
        // Arrange
        var expectedResult = new SubmissionPeriod() { DataPeriod = "January to June 2025" };

        // Act
        var result = await _service.GetActiveSubmissionPeriod();

        // Assert
        result.Should().NotBeNull();
        result.DataPeriod.Should().Be(expectedResult.DataPeriod);
    }

    [Test]
    public async Task GetActiveSubmissionPeriod_ReturnsCorrectActiveFrom()
    {
        // Arrange
        var expectedResult = new SubmissionPeriod() { DataPeriod = "January to June 2025", ActiveFrom = new DateTime(2025, 07, 01) };

        // Act
        var result = await _service.GetActiveSubmissionPeriod();

        // Assert
        result.Should().NotBeNull();
        result.ActiveFrom.Should().Be(expectedResult.ActiveFrom);
    }

    [Test]
    public async Task GetActualSubmissionPeriod_ShouldReturn_SubmissionPeriod()
    {
        // Arrange
        var expectedResult = "January to December 2025";
        var submissionId = Guid.NewGuid();
        var submissionPeriod = "July to December 2025";

        _mockSubmissionService
            .Setup(x => x.GetActualSubmissionPeriod(submissionId, submissionPeriod))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetActualSubmissionPeriod(submissionId, submissionPeriod);

        // Assert
        result.Should().Be(expectedResult);
        _mockSubmissionService.Verify(x => x.GetActualSubmissionPeriod(submissionId, submissionPeriod), Times.Once);
    }

    [Test]
    public async Task GetFeatureFlagForProducersFeebreakdown_ShouldReturnTrueIfFeatureIsEnabled()
    {
        // Arrange
        _mockFeatureManager.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.IncludeSubsidariesInFeeCalculationsForProducers))).ReturnsAsync(true);
        var expectedResult = true;

        // Act
        var result = await _service.GetFeatureFlagForProducersFeebreakdown();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task GetFeatureFlagForProducersFeebreakdown_ShouldReturnFalseIfFeatureIsDisabled()
    {
        // Arrange
        _mockFeatureManager.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.IncludeSubsidariesInFeeCalculationsForProducers))).ReturnsAsync(false);
        var expectedResult = true;

        // Act
        var result = await _service.GetFeatureFlagForProducersFeebreakdown();

        // Assert
        result.Should().BeFalse();
    }

    //[Test]
    //public async Task GetCurrentMonthAndYearForRecyclingObligations_ShouldReturn_DesiredMonth_AndYear()
    //{
    //    // Arrange
    //    _mockFeatureManager.Setup(x => x.IsEnabledAsync(nameof(FeatureFlags.IncludeSubsidariesInFeeCalculationsForProducers))).ReturnsAsync(true);
    //    var expectedResult = true;

    //    // Act
    //    var result = await _service.GetFeatureFlagForProducersFeebreakdown();

    //    // Assert
    //    result.Should().BeTrue();
    //}
}
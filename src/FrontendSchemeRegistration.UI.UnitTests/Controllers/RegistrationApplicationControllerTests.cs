using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Controllers.Error;
using FrontendSchemeRegistration.UI.Controllers.RegistrationApplication;
using FrontendSchemeRegistration.UI.Extensions;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Newtonsoft.Json;
using Organisation = EPR.Common.Authorization.Models.Organisation;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class RegistrationApplicationControllerTests
{
    private const string OrganisationName = "Acme Org Ltd";
    private const string SubmissionPeriod = "Jul to Dec 23";

    private static readonly RegistrationFeeCalculationDetails[] _feeCalculationDetails =
    [
        new RegistrationFeeCalculationDetails
        {
            OrganisationSize = "Large",
            IsOnlineMarketplace = true,
            NumberOfSubsidiaries = 54,
            NumberOfSubsidiariesBeingOnlineMarketPlace = 29
        }
    ];

    private static readonly PaymentCalculationResponse CalculationResponse = new()
    {
        ProducerRegistrationFee = 262000,
        ProducerOnlineMarketPlaceFee = 257900,
        ProducerLateRegistrationFee = 33200,
        SubsidiariesFee = 9071100,
        TotalFee = 9591000,
        PreviousPayment = 150000,
        SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown
        {
            TotalSubsidiariesOnlineMarketplaceFee = 7479100,
            CountOfOnlineMarketplaceSubsidiaries = 29,
            UnitOnlineMarketplaceFee = 257900
        }
    };

    private static readonly ComplianceSchemePaymentCalculationResponse _complianceSchemeCalculationResponse = new()
    {
        ComplianceSchemeMembersWithFees =
        [
            new ComplianceSchemePaymentCalculationResponseMember
            {
                MemberId = "123",
                MemberLateRegistrationFee = 5000,
                MemberOnlineMarketPlaceFee = 7000,
                MemberRegistrationFee = 9000,
                SubsidiariesFee = 11000,
                SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown
                {
                    CountOfOnlineMarketplaceSubsidiaries = 1,
                    TotalSubsidiariesOnlineMarketplaceFee = 2000,
                    UnitOnlineMarketplaceFee = 3000,
                    FeeBreakdowns =
                    [
                        new FeeBreakdown
                        {
                            BandNumber = 5,
                            TotalPrice = 6000,
                            UnitCount = 7,
                            UnitPrice = 8000
                        }
                    ]
                },
                TotalMemberFee = 15000
            }
        ],
        TotalFee = 12345,
        PreviousPayment = 23456,
        ComplianceSchemeRegistrationFee = 20000,
        OutstandingPayment = 30000
    };

    private UserData _userData;
    private readonly Guid _organisationId = Guid.NewGuid();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<HttpRequest> _httpRequestMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();

    private Mock<ISessionManager<RegistrationApplicationSession>> SessionManagerMock { get; set; }

    private RegistrationApplicationController SystemUnderTest { get; set; }

    private Mock<IRegistrationApplicationService> RegistrationApplicationService { get; set; }

    private Mock<ILogger<RegistrationApplicationController>> LoggerMock { get; set; }

    private RegistrationApplicationSession Session { get; set; }

    private void SetupBase(UserData userData)
    {
        SetupUserData(userData);

        var tempDataDictionaryMock = new Mock<ITempDataDictionary>();

        SessionManagerMock = new Mock<ISessionManager<RegistrationApplicationSession>>();
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new RegistrationApplicationSession()));

        LoggerMock = new Mock<ILogger<RegistrationApplicationController>>();
        RegistrationApplicationService = new Mock<IRegistrationApplicationService>();

        SystemUnderTest = new RegistrationApplicationController(
            SessionManagerMock.Object,
            LoggerMock.Object,
            RegistrationApplicationService.Object);
        SystemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;
        SystemUnderTest.ControllerContext.HttpContext.Session = new Mock<ISession>().Object;
        SystemUnderTest.TempData = tempDataDictionaryMock.Object;

        var queryStrings = new QueryCollection(new Dictionary<string, StringValues> { { "registrationyear", DateTime.Now.Year.ToString() } });

        _httpRequestMock.Setup(x => x.Query).Returns(queryStrings);
        _httpContextMock.Setup(x => x.Features.Get<IRequestCultureFeature>())
            .Returns(new RequestCultureFeature(new RequestCulture(Language.English), null));
        _httpContextMock.Setup(x => x.Request).Returns(_httpRequestMock.Object);
    }

    private void SetupUserData(UserData userData)
    {
        var claims = new List<Claim>();
        if (userData != null)
        {
            claims.Add(new Claim(ClaimTypes.UserData, JsonConvert.SerializeObject(userData)));
        }

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
    }

    [SetUp]
    public void SetUp()
    {
        _userData = GetUserData("Producer");

        SetupBase(_userData);
    }

    private UserData GetUserData(string organisationRole)
    {
        return new UserData
        {
            Id = Guid.NewGuid(),
            Organisations =
            [
                new Organisation
                {
                    Name = OrganisationName,
                    OrganisationNumber = "123456",
                    Id = _organisationId,
                    OrganisationRole = organisationRole,
                    NationId = 1
                }
            ]
        };
    }

    [Test]
    public async Task RegistrationTaskList_DoesNot_Call_SubmitRegistrationApplication_for_Compliance_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
        SetupBase(GetUserData("Compliance Scheme"));

        var details = new RegistrationApplicationSession
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationFeePaymentMethod = null,
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "test", RowNumber = 1 },
            RegistrationJourney = null
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>())).ReturnsAsync(details);
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationFeePaymentMethod = null,
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "test", RowNumber = 1 },
        });

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be(PagePaths.ProducerRegistrationGuidance);
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        RegistrationApplicationService.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name!,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.NotStarted,
            IsComplianceScheme = true,
            ShowRegistrationCaption = false,
        });
    }
    
    [Test]
    public async Task RegistrationTaskList_Defaults_To_UserData_Organisation_Name_When_Not_In_Session_And_ReturnsCorrectViewAndModel()
    {
        // Arrange
        SetupBase(GetUserData("Compliance Scheme"));

        var details = new RegistrationApplicationSession
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationFeePaymentMethod = null,
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "test", RowNumber = 1 },
            RegistrationJourney = null
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>())).ReturnsAsync(details);
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationFeePaymentMethod = null,
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "test", RowNumber = 1 },
        });

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be(PagePaths.ProducerRegistrationGuidance);
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        RegistrationApplicationService.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name!,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.NotStarted,
            IsComplianceScheme = true
        });
    }

    [TestCase("CsoLargeProducer", RegistrationJourney.CsoLargeProducer)]
    [TestCase("CsoSmallProducer", RegistrationJourney.CsoSmallProducer)]
    [TestCase("Medium", null)]
    public async Task Then_Parses_ProducerSize_Query_String_Value_To_ApplicationService_And_Defaults(string registrationJourney, RegistrationJourney? expectedRegistrationJourney)
    {
        // Arrange
        SetupBase(GetUserData("Compliance Scheme"));
        var queryStrings = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "registrationyear", DateTime.Now.Year.ToString() },
            { "registrationjourney", registrationJourney}
        });
        _httpRequestMock.Setup(x => x.Query).Returns(queryStrings);

        var details = new RegistrationApplicationSession
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationFeePaymentMethod = null,
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "test", RowNumber = 1 },
            RegistrationJourney = null
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), expectedRegistrationJourney, It.IsAny<bool?>())).ReturnsAsync(details);
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationFeePaymentMethod = null,
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "test", RowNumber = 1 },
        });

        // Act
        RegistrationJourney? parsedRegistrationJourney = null;
        if (Enum.TryParse<RegistrationJourney>(registrationJourney, true, out var parsedJourney))
        {
            parsedRegistrationJourney = parsedJourney;
        }
        var result = await SystemUnderTest.RegistrationTaskList(parsedRegistrationJourney) as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        if (expectedRegistrationJourney == null)
        {
            pageBackLink.Should().Be(PagePaths.ProducerRegistrationGuidance);
        }
        else
        {
            pageBackLink.Should().Be($"{PagePaths.ProducerRegistrationGuidance}?registrationjourney={expectedRegistrationJourney.ToString()}");    
        }
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        RegistrationApplicationService.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);
        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            ShowRegistrationCaption = false,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.NotStarted,
            IsComplianceScheme = true
        });
    }

    [Test]
    public async Task RegistrationTaskList_SubmitRegistrationData_When_FileUploaded_Is_PendingState_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = null,
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = null,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.FileUploaded,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null,
            RegistrationJourney = RegistrationJourney.DirectLargeProducer
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession
        {
            SubmissionId = registrationApplicationDetails.SubmissionId,
            IsSubmitted = registrationApplicationDetails.IsSubmitted,
            ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationDetails.RegistrationReferenceNumber,
            LastSubmittedFile = registrationApplicationDetails.LastSubmittedFile,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate,
            RegistrationApplicationSubmittedComment = registrationApplicationDetails.RegistrationApplicationSubmittedComment,
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails,
            IsLateFeeApplicable = false
        });

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name!,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Pending,
            PaymentViewStatus = RegistrationTaskListStatus.CanNotStartYet,
            AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
            ApplicationStatus = ApplicationStatusType.FileUploaded,
            ShowRegistrationCaption = true,
            RegistrationJourney = RegistrationJourney.DirectLargeProducer
        });
    }

    [Test]
    public async Task RegistrationTaskList_SetsSession_RegistrationFeePaid_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }],
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null,
            RegistrationJourney = null
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession
        {
            SubmissionId = registrationApplicationDetails.SubmissionId,
            IsSubmitted = registrationApplicationDetails.IsSubmitted,
            ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationDetails.RegistrationReferenceNumber,
            LastSubmittedFile = registrationApplicationDetails.LastSubmittedFile,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate,
            RegistrationApplicationSubmittedComment = registrationApplicationDetails.RegistrationApplicationSubmittedComment,
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails,
            IsLateFeeApplicable = false
        });

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be(PagePaths.HomePageSelfManaged);
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Completed,
            PaymentViewStatus = RegistrationTaskListStatus.Completed,
            AdditionalDetailsStatus = RegistrationTaskListStatus.NotStarted,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
        });
    }

    [Test]
    public async Task RegistrationTaskList_SetsSession_RegistrationApplicationSubmitted_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }],
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = "Test",
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(),It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession
        {
            SubmissionId = registrationApplicationDetails.SubmissionId,
            IsSubmitted = registrationApplicationDetails.IsSubmitted,
            ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationDetails.RegistrationReferenceNumber,
            LastSubmittedFile = registrationApplicationDetails.LastSubmittedFile,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate,
            RegistrationApplicationSubmittedComment = registrationApplicationDetails.RegistrationApplicationSubmittedComment,
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails,
            IsLateFeeApplicable = false
        });

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        Session.IsLateFeeApplicable.Should().BeFalse();

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Completed,
            PaymentViewStatus = RegistrationTaskListStatus.Completed,
            AdditionalDetailsStatus = RegistrationTaskListStatus.Completed,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
        });
    }

    [Test]
    public async Task RegistrationTaskList_SetsSession_RegistrationApplicationApproved_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.RegistrationTaskList],
            ApplicationStatus = ApplicationStatusType.ApprovedByRegulator,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeePaymentMethod = null,
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ApplicationReferenceNumber = reference
        };


        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }],
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.ApprovedByRegulator,
            RegistrationApplicationSubmittedComment = "Test",
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession
        {
            SubmissionId = registrationApplicationDetails.SubmissionId,
            IsSubmitted = registrationApplicationDetails.IsSubmitted,
            ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationDetails.RegistrationReferenceNumber,
            LastSubmittedFile = registrationApplicationDetails.LastSubmittedFile,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate,
            RegistrationApplicationSubmittedComment = registrationApplicationDetails.RegistrationApplicationSubmittedComment,
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails,
            IsLateFeeApplicable = false
        });
        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        Session.IsLateFeeApplicable.Should().BeFalse();

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Completed,
            PaymentViewStatus = RegistrationTaskListStatus.Completed,
            AdditionalDetailsStatus = RegistrationTaskListStatus.Completed,
            ApplicationStatus = ApplicationStatusType.ApprovedByRegulator
        });
    }

    [Test]
    public async Task RegistrationTaskList_SetsSession_With_ComplianceScheme_RegistrationFeePaid_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() },
            SubmissionId = submissionId,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationFeePaymentMethod = null
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession
        {
            SubmissionId = registrationApplicationDetails.SubmissionId,
            SelectedComplianceScheme = registrationApplicationDetails.SelectedComplianceScheme,
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod
        });

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.NotStarted,
            PaymentViewStatus = RegistrationTaskListStatus.CanNotStartYet,
            AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
            IsComplianceScheme = true
        });
    }

    [Test]
    public async Task WhenRegistrationDataHasBeenSubmitted_RegistrationFeeCalculations_ReturnsFeeCalculationBreakdownViewModel()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.RegistrationFeeCalculations],
            SubmissionId = Guid.NewGuid(),
            ApplicationReferenceNumber = "TestRef",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = null,
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid(), SubmittedDateTime = DateTime.Now },
            RegistrationFeeCalculationDetails = _feeCalculationDetails,
            RegistrationJourney = null,
            IsSubmitted = true
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        RegistrationApplicationService.Setup(s => s.GetProducerRegistrationFees(It.IsAny<ISession>()))
            .ReturnsAsync(new FeeCalculationBreakdownViewModel
            {
                OrganisationSize = _feeCalculationDetails[0].OrganisationSize,
                IsOnlineMarketplace = _feeCalculationDetails[0].IsOnlineMarketplace,
                NumberOfSubsidiaries = _feeCalculationDetails[0].NumberOfSubsidiaries,
                NumberOfSubsidiariesBeingOnlineMarketplace = _feeCalculationDetails[0].NumberOfSubsidiariesBeingOnlineMarketPlace,
                IsLateFeeApplicable = Session.IsLateFeeApplicable,
                BaseFee = CalculationResponse.ProducerRegistrationFee,
                OnlineMarketplaceFee = CalculationResponse.ProducerOnlineMarketPlaceFee,
                TotalSubsidiaryFee = CalculationResponse.SubsidiariesFee - CalculationResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
                TotalSubsidiaryOnlineMarketplaceFee = CalculationResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
                TotalPreviousPayments = CalculationResponse.PreviousPayment,
                TotalFeeAmount = CalculationResponse.TotalFee,
                IsRegistrationFeePaid = Session.IsRegistrationFeePaid,
                ProducerLateRegistrationFee = CalculationResponse.ProducerLateRegistrationFee,
            });

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<FeeCalculationBreakdownViewModel>();
        result.Model.As<FeeCalculationBreakdownViewModel>().Should().BeEquivalentTo(new FeeCalculationBreakdownViewModel
        {
            OrganisationSize = _feeCalculationDetails[0].OrganisationSize,
            IsOnlineMarketplace = _feeCalculationDetails[0].IsOnlineMarketplace,
            ProducerLateRegistrationFee = CalculationResponse.ProducerLateRegistrationFee,
            NumberOfSubsidiaries = _feeCalculationDetails[0].NumberOfSubsidiaries,
            NumberOfSubsidiariesBeingOnlineMarketplace = _feeCalculationDetails[0].NumberOfSubsidiariesBeingOnlineMarketPlace,
            BaseFee = CalculationResponse.ProducerRegistrationFee,
            OnlineMarketplaceFee = CalculationResponse.ProducerOnlineMarketPlaceFee,
            TotalSubsidiaryFee = CalculationResponse.SubsidiariesFee - CalculationResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
            TotalPreviousPayments = CalculationResponse.PreviousPayment,
            TotalFeeAmount = CalculationResponse.TotalFee,
            IsRegistrationFeePaid = Session.IsRegistrationFeePaid,
            TotalSubsidiaryOnlineMarketplaceFee = CalculationResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee
        });
    }

    [Test]
    public void WhenProducerNotFound_RegistrationFeeCalculations_RedirectsTo_HandleThrownExceptions()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.RegistrationFeeCalculations],
            SubmissionId = Guid.NewGuid(),
            ApplicationReferenceNumber = "TestRef",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now },
            RegistrationJourney = null
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        RegistrationApplicationService.Setup(x => x.GetProducerRegistrationFees(It.IsAny<ISession>())).ReturnsAsync((FeeCalculationBreakdownViewModel) null);

        // Act
        var result = SystemUnderTest.RegistrationFeeCalculations().Result;

        // Assert
        var selectedCs = Session.SelectedComplianceScheme?.Id;

        result.Should().BeOfType<RedirectToActionResult>();
        var viewResult = result as RedirectToActionResult;
        viewResult.ActionName.Should().Be(nameof(ErrorController.HandleThrownExceptions));
        LoggerMock.VerifyLog(x => x.LogWarning("Error in Getting Registration Fees Details for SubmissionId {SubmissionId}, OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", Session.SubmissionId, _userData.Organisations[0].OrganisationNumber!, Session.ApplicationReferenceNumber, selectedCs));
    }

    [Test]
    public async Task WhenRegistrationDataIsNotSubmitted_RegistrationFeeCalculations_RedirectsToPreviousView()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.RegistrationFeeCalculations],
            SubmissionId = null,
            ApplicationReferenceNumber = null,
            RegistrationJourney = null
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        RegistrationApplicationService.Setup(x => x.GetProducerRegistrationFees(It.IsAny<ISession>())).ReturnsAsync(
            new FeeCalculationBreakdownViewModel
            {
                OrganisationSize = _feeCalculationDetails[0].OrganisationSize,
                IsOnlineMarketplace = _feeCalculationDetails[0].IsOnlineMarketplace,
                NumberOfSubsidiaries = _feeCalculationDetails[0].NumberOfSubsidiaries,
                NumberOfSubsidiariesBeingOnlineMarketplace = _feeCalculationDetails[0].NumberOfSubsidiariesBeingOnlineMarketPlace,
                IsLateFeeApplicable = Session.IsLateFeeApplicable,
                BaseFee = CalculationResponse.ProducerRegistrationFee,
                OnlineMarketplaceFee = CalculationResponse.ProducerOnlineMarketPlaceFee,
                TotalSubsidiaryFee = CalculationResponse.SubsidiariesFee - CalculationResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
                TotalSubsidiaryOnlineMarketplaceFee = CalculationResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
                TotalPreviousPayments = CalculationResponse.PreviousPayment,
                TotalFeeAmount = CalculationResponse.TotalFee,
                IsRegistrationFeePaid = Session.IsRegistrationFeePaid,
                ProducerLateRegistrationFee = CalculationResponse.ProducerLateRegistrationFee
            });

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    public async Task RegistrationFeeCalculations_ShouldCallGetProducerDetails_WithCorrectOrganisationNumber()
    {
        // Arrange
        RegistrationFeeCalculationDetails[] producerDetails =
        [
            new RegistrationFeeCalculationDetails
            {
                NumberOfSubsidiariesBeingOnlineMarketPlace = 1,
                OrganisationSize = "Large",
                IsOnlineMarketplace = true,
                NumberOfSubsidiaries = 1,
                OrganisationId = "1234"
            }
        ];

        var mockSession = new RegistrationApplicationSession
        {
            SubmissionId = Guid.NewGuid(),
            ApplicationReferenceNumber = "456",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeeCalculationDetails = producerDetails,
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now },
            RegistrationJourney = null,
        };

        var registrationFeesResponse = new PaymentCalculationResponse
        {
            OutstandingPayment = 100,
            SubsidiariesFee = 10,
            SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown
            {
                TotalSubsidiariesOnlineMarketplaceFee = 5
            }
        };

        SessionManagerMock
            .Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(mockSession);

        RegistrationApplicationService
            .Setup(s => s.GetProducerRegistrationFees(It.IsAny<ISession>()))
            .ReturnsAsync(new FeeCalculationBreakdownViewModel
            {
                OrganisationSize = _feeCalculationDetails[0].OrganisationSize,
                IsOnlineMarketplace = _feeCalculationDetails[0].IsOnlineMarketplace,
                NumberOfSubsidiaries = _feeCalculationDetails[0].NumberOfSubsidiaries,
                NumberOfSubsidiariesBeingOnlineMarketplace = _feeCalculationDetails[0].NumberOfSubsidiariesBeingOnlineMarketPlace,
                IsLateFeeApplicable = Session.IsLateFeeApplicable,
                BaseFee = registrationFeesResponse.ProducerRegistrationFee,
                OnlineMarketplaceFee = registrationFeesResponse.ProducerOnlineMarketPlaceFee,
                TotalSubsidiaryFee = registrationFeesResponse.SubsidiariesFee - registrationFeesResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
                TotalSubsidiaryOnlineMarketplaceFee = registrationFeesResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
                TotalPreviousPayments = registrationFeesResponse.PreviousPayment,
                TotalFeeAmount = registrationFeesResponse.TotalFee,
                IsRegistrationFeePaid = Session.IsRegistrationFeePaid,
                ProducerLateRegistrationFee = registrationFeesResponse.ProducerLateRegistrationFee
            });

        // Act
        await SystemUnderTest.RegistrationFeeCalculations();

        // Assert
        RegistrationApplicationService.Verify(s => s.GetProducerRegistrationFees(It.IsAny<ISession>()), Times.Once);
    }

    [Test]
    public async Task RegistrationFeeCalculations_ShouldNotCallGetProducerDetails_WhenRegistrationDataNotSubmitted()
    {
        // Arrange
        var mockSession = new RegistrationApplicationSession
        {
            SubmissionId = Guid.NewGuid(),
            ApplicationReferenceNumber = "TestRef",
            RegistrationJourney = null,
        };

        SessionManagerMock
            .Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(mockSession);

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(SystemUnderTest.RegistrationTaskList));
    }

    [Test]
    public async Task WhenRegistrationDataNotSubmitted_ProducerRegistrationGuidance_ReturnsCorrectViewModel()
    {
        // Arrange
        const string nationCode = "GB-ENG";
        var complianceScheme = new ComplianceSchemeDto
        {
            Id = Guid.NewGuid(),
            Name = "Biffpack (Environment Agency)",
            NationId = 1,
            CreatedOn = DateTime.Now
        };
        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            SelectedComplianceScheme = complianceScheme,
            RegulatorNation = nationCode
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.ProducerRegistrationGuidance],
            SelectedComplianceScheme = complianceScheme,
            RegulatorNation = nationCode
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.ProducerRegistrationGuidance() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<ProducerRegistrationGuidanceViewModel>();
        Session.FileUploadStatus.Should().Be(RegistrationTaskListStatus.NotStarted);
        Session.IsRegistrationFeePaid.Should().BeFalse();

        result.Model.As<ProducerRegistrationGuidanceViewModel>().Should().BeEquivalentTo(new ProducerRegistrationGuidanceViewModel
        {
            RegulatorNation = nationCode,
            ComplianceScheme = complianceScheme.Name,
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat()
        });
    }

    [Test]
    [TestCase(ApplicationStatusType.FileUploaded)]
    [TestCase(ApplicationStatusType.SubmittedAndHasRecentFileUpload)]
    public void WhenRegistrationDataSubmitted_ProducerRegistrationGuidance_RedirectsToTaskListView(ApplicationStatusType statusType)
    {
        // Arrange
        var submissionId = Guid.NewGuid();
        var reference = "TestRef";

        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }],
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = statusType
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        Session = new RegistrationApplicationSession
        {
            SubmissionId = registrationApplicationDetails.SubmissionId,
            IsSubmitted = registrationApplicationDetails.IsSubmitted,
            ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
            RegistrationReferenceNumber = registrationApplicationDetails.RegistrationReferenceNumber,
            LastSubmittedFile = registrationApplicationDetails.LastSubmittedFile,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate,
            RegistrationApplicationSubmittedComment = registrationApplicationDetails.RegistrationApplicationSubmittedComment,
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails,
            IsLateFeeApplicable = false
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.ProducerRegistrationGuidance().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        Session.FileUploadStatus.Should().Be(RegistrationTaskListStatus.NotStarted);
        Session.IsRegistrationFeePaid.Should().BeFalse();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    [TestCase(ApplicationStatusType.NotStarted, false, false)]
    [TestCase(ApplicationStatusType.FileUploaded, false, false)]
    [TestCase(ApplicationStatusType.NotStarted, true, true)]
    public void WhenRegistrationDataNotSubmittedOrFeeNotPaid_AdditionalInformation_RedirectsToTaskListView(ApplicationStatusType dataSubmitted, bool feePaid, bool appSubmitted)
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            ApplicationStatus = dataSubmitted,
            RegistrationFeePaymentMethod = feePaid ? "PayByPhone" : null,
            ApplicationReferenceNumber = "test",
            IsSubmitted = true,
            RegistrationApplicationSubmittedDate = appSubmitted ? DateTime.Now : null,
            SubmissionId = Guid.NewGuid()
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.AdditionalInformation().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    [TestCase(ApplicationStatusType.AcceptedByRegulator)]
    [TestCase(ApplicationStatusType.ApprovedByRegulator)]
    [TestCase(ApplicationStatusType.SubmittedToRegulator)]
    public void WhenRegistrationDataSubmittedAndFeePaid_AdditionalInformation_RedirectsToSubmitRegistrationRequestView(ApplicationStatusType applicationStatusType)
    {
        // Arrange
        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            ApplicationStatus = applicationStatusType,
            RegistrationFeePaymentMethod = "PayByPhone",
            ApplicationReferenceNumber = "test",
            IsSubmitted = true,
            RegistrationApplicationSubmittedDate = DateTime.Now,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }]
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(),It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            IsSubmitted = registrationApplicationDetails.IsSubmitted,
            RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate,
            SubmissionId = registrationApplicationDetails.SubmissionId,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.AdditionalInformation().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.SubmitRegistrationRequest));
    }

    [Test]
    public async Task WhenRegistrationDataSubmittedAndFeePaid_AdditionalInformation_ReturnsCorrectViewModel()
    {
        // Arrange
        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            SubmissionId = Guid.NewGuid(),
            ApplicationReferenceNumber = "TestRef",
            RegistrationFeePaymentMethod = "PayByPhone",
            RegulatorNation = "GB-SCT",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, OrganisationSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = "1234" }]
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            RegulatorNation = registrationApplicationDetails.RegulatorNation,
            SubmissionId = registrationApplicationDetails.SubmissionId,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails,
            RegistrationJourney = RegistrationJourney.CsoSmallProducer
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.AdditionalInformation() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<AdditionalInformationViewModel>();

        result.Model.As<AdditionalInformationViewModel>().Should().BeEquivalentTo(new AdditionalInformationViewModel
        {
            IsComplianceScheme = false,
            RegulatorNation = Session.RegulatorNation,
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            ComplianceScheme = Session.SelectedComplianceScheme?.Name,
            RegistrationJourney = Session.RegistrationJourney
        });
    }

    [Test]
    [TestCase("Approved Person")]
    [TestCase("Delegated Person")]
    public void WhenApplicationNotGrantedAndSubmissionExists_AdditionalInformationPostAction_CallsSubmitRegistrationApplicationAsync(string role)
    {
        // Arrange
        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() },
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeePaymentMethod = "PayByPhone",
            RegistrationApplicationSubmittedDate = null,
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }]
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails,
            ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            RegistrationApplicationSubmittedDate = registrationApplicationDetails.RegistrationApplicationSubmittedDate,
            RegulatorNation = registrationApplicationDetails.RegulatorNation,
            SubmissionId = registrationApplicationDetails.SubmissionId
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        SetupUserData(new UserData { ServiceRole = role });

        RegistrationApplicationService.Setup(x => x.CreateRegistrationApplicationEvent(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()));

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.SubmitRegistrationRequest));
        RegistrationApplicationService.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
    }

    [Test]
    [TestCase("Approved Person")]
    [TestCase("Delegated Person")]
    public void WhenApplicationNotGrantedAndSubmissionDoesNotExist_AdditionalInformationPostAction_NeverCallSubmitRegistrationApplicationAsync(string role)
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            RegistrationApplicationSubmittedDate = null
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);
        SetupUserData(new UserData { ServiceRole = role });

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.SubmitRegistrationRequest));
        RegistrationApplicationService.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void WhenApplicationHasBeenGrantedAndRegardlessOfSubmission_AdditionalInformationPostAction_NeverCallSubmitRegistrationApplicationAsync(bool valid)
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);
        SetupUserData(new UserData { ServiceRole = "Approved Person" });

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.SubmitRegistrationRequest));
        RegistrationApplicationService.Verify(x => x.CreateRegistrationApplicationEvent(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);
    }

    [Test]
    public void WhenApplicationHasBeenGranted_SubmitRegistration_ShouldRedirectToSubmitRegistrationRequest()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            ApplicationStatus = ApplicationStatusType.AcceptedByRegulator,
            ApplicationReferenceNumber = "test",
            IsSubmitted = true,
            RegistrationApplicationSubmittedDate = DateTime.Now,
            RegistrationFeePaymentMethod = "PayByPhone",
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            SubmissionId = Guid.NewGuid()
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.AdditionalInformation().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.SubmitRegistrationRequest));
    }

    [Test]
    [TestCase("Approved Person")]
    [TestCase("Delegated Person")]
    public void WhenPostActionCalledWithApprovedUser_AdditionalInformation_RedirectsToSubmitRegistrationRequest(string role)
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            RegulatorNation = "GB-SCT"
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);
        SetupUserData(new UserData { ServiceRole = role });

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.SubmitRegistrationRequest));
    }

    [Test]
    public void WhenPostActionCalledWithBasicUser_AdditionalInformation_RedirectsToUnauthorisedUserWarnings()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            RegulatorNation = "GB-SCT"
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);
        SetupUserData(new UserData { ServiceRole = "Basic User" });

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.UnauthorisedUserWarnings));
    }

    [Test]
    public void WhenGetActionCalledWithProducerUser_UnauthorisedUserWarnings_ShouldReturnCorrectViewData()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SubmissionId = Guid.NewGuid(),
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeePaymentMethod = "PayByPhone",
            RegulatorNation = "GB-SCT"
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.UnauthorisedUserWarnings().Result as ViewResult;

        // Assert
        result.ViewData["OrganisationName"].Should().Be("Acme Org Ltd");
        result.ViewData["OrganisationNumber"].Should().Be("123 456");
        result.ViewName.Should().Be("UnauthorisedUserWarnings");
    }

    [Test]
    public void WhenGetActionCalledWithCSOUser_UnauthorisedUserWarnings_ShouldReturnCorrectViewData()
    {
        // Arrange
        _userData.Organisations[0].OrganisationRole = "Compliance Scheme";
        SetupBase(_userData);
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            SelectedComplianceScheme = new ComplianceSchemeDto { Name = "Compliance Ltd" },
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            SubmissionId = Guid.NewGuid(),
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            RegistrationFeePaymentMethod = "PayByPhone",
            RegulatorNation = "GB-SCT"
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.UnauthorisedUserWarnings().Result as ViewResult;

        // Assert
        result.ViewData["ComplianceScheme"].Should().Be("Compliance Ltd");
        result.ViewData["NationName"].Should().Be("scotland");
        result.ViewName.Should().Be("UnauthorisedUserWarnings");
    }

    [TestCase("GB-ENG", "England")]
    [TestCase("GB-SCT", "Scotland")]
    [TestCase("GB-WLS", "Wales")]
    [TestCase("GB-NIR", "NorthernIreland")]
    public async Task SubmitRegistrationRequest_ReturnsCorrectViewModel(string nationCode, string nationName)
    {
        // Arrange
        const string viewName = "ApplicationSubmissionConfirmation";
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.AdditionalInformation],
            RegulatorNation = nationCode,
            ApplicationReferenceNumber = "1234EFGH",
            RegistrationReferenceNumber = "1234EFGH",
            ApplicationStatus = ApplicationStatusType.AcceptedByRegulator,
            RegistrationFeePaymentMethod = "PayByPhone",
            RegistrationApplicationSubmittedComment = "test comment",
            RegistrationApplicationSubmittedDate = DateTime.Today,
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "123", OrganisationSize = "Large" }]
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.SubmitRegistrationRequest() as ViewResult;
        var model = result.Model as ApplicationSubmissionConfirmationViewModel;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<ApplicationSubmissionConfirmationViewModel>();
        model.RegulatorNation.Should().Be(nationCode);
        model.NationName.Should().Be(nationName);
        pageBackLink.Should().Be(PagePaths.RegistrationTaskList);

        result.Model.As<ApplicationSubmissionConfirmationViewModel>().Should().BeEquivalentTo(new ApplicationSubmissionConfirmationViewModel
        {
            ApplicationStatus = Session.ApplicationStatus,
            RegulatorNation = Session.RegulatorNation,
            ApplicationReferenceNumber = Session.ApplicationReferenceNumber,
            RegistrationReferenceNumber = Session.RegistrationReferenceNumber,
            RegistrationApplicationSubmittedDate = Session.RegistrationApplicationSubmittedDate.Value
        });
    }

    [Test]
    public async Task WhenFileUploadStatus_NotCompleted_SelectPaymentOptions_RedirectsToTaskList()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.SelectPaymentOptions],
            RegulatorNation = "GB-ENG",
            ApplicationStatus = ApplicationStatusType.FileUploaded
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.SelectPaymentOptions() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    public void WhenEngland_SelectPaymentOptions_ReturnsCorrectViewAndModel()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.SelectPaymentOptions],
            RegulatorNation = "GB-ENG",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.SelectPaymentOptions().Result;

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<SelectPaymentOptionsViewModel>();
    }

    [TestCase("GB-SCT", "Scotland")]
    [TestCase("GB-WLS", "Wales")]
    [TestCase("GB-NIR", "NorthernIreland")]
    public void WhenNationNotEngland_SelectPaymentOptions_RedirectsToPayByBankTransfer(string nationCode, string nationName)
    {
        // Arrange
        var details = new RegistrationApplicationSession
        {
            IsSubmitted = false,
            ApplicationStatus = ApplicationStatusType.NotStarted,
            RegistrationFeePaymentMethod = null,
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "test", RowNumber = 1 },
            RegulatorNation = nationCode,
        };

        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>())).ReturnsAsync(details);

        // Act
        var result = SystemUnderTest.SelectPaymentOptions().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(RegistrationApplicationController.PayByBankTransfer));
    }

    [Test]
    public void SelectPaymentOptions_OnSubmit_WhenNoPaymentOptionSelected_ReturnsInvalidModelState()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey =
            [
                PagePaths.RegistrationFeeCalculations,
                PagePaths.SelectPaymentOptions
            ]
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        var model = new SelectPaymentOptionsViewModel() { PaymentOption = null };

        ValidateViewModel(model);

        // Act
        var result = SystemUnderTest.SelectPaymentOptions(model).Result;

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;

        viewResult.ViewData.ModelState["PaymentOption"].Errors.Count.Should().Be(1);
    }

    [TestCase((int) PaymentOptions.PayOnline, "PayOnline")]
    [TestCase((int) PaymentOptions.PayByBankTransfer, "PayByBankTransfer")]
    [TestCase((int) PaymentOptions.PayByPhone, "PayByPhone")]
    [TestCase(4, null)]
    public void SelectPaymentOptions_OnSubmit_RedirectsToView(int paymentOption, string actionName)
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.SelectPaymentOptions]
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        var model = new SelectPaymentOptionsViewModel() { PaymentOption = paymentOption };

        // Act
        var result = SystemUnderTest.SelectPaymentOptions(model).Result;

        // Assert
        if (paymentOption <= 3)
        {
            result.Should().BeOfType<RedirectToActionResult>();
            var viewResult = result as RedirectToActionResult;

            viewResult.ActionName.Should().Be(actionName);
        }
        else
        {
            result.Should().BeOfType<ViewResult>();

            (result as ViewResult).Model.Should().BeOfType<SelectPaymentOptionsViewModel>();
        }
    }

    [Test]
    public async Task WhenFileUploadStatus_NotCompleted_PayByPhone_RedirectsToTaskList()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.PaymentOptionPayByPhone],
            RegulatorNation = "GB-ENG",
            ApplicationStatus = ApplicationStatusType.FileUploaded
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.PayByPhone() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    public async Task PayByPhone_ReturnsCorrectViewModel()
    {
        // Arrange
        const string viewName = "PaymentOptionPayByPhone";
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.PaymentOptionPayByPhone],
            TotalAmountOutstanding = 2045600,
            ApplicationReferenceNumber = "1234EFGH",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.PayByPhone() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayByPhoneViewModel>();

        result.Model.As<PaymentOptionPayByPhoneViewModel>().Should().BeEquivalentTo(new PaymentOptionPayByPhoneViewModel
        {
            TotalAmountOutstanding = Session.TotalAmountOutstanding,
            ApplicationReferenceNumber = Session.ApplicationReferenceNumber
        });
    }

    [Test]
    public async Task PayByPhone_WhenSessionIsNull_RedirectsToTaskListView()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((RegistrationApplicationSession) null); // Simulate null session

        // Act
        var result = await SystemUnderTest.PayByPhone();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.ActionName.Should().Be("RegistrationTaskList");
    }

    [Test]
    public async Task PayByPhone_WhenRegistrationSessionIsNull_RedirectsToTaskListView()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((RegistrationApplicationSession) null); // Simulate null session

        // Act
        var result = await SystemUnderTest.PayByPhone();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.ActionName.Should().Be("RegistrationTaskList");
    }

    [Test]
    public async Task PayByPhone_WhenApplicationReferenceNumberIsEmpty_RedirectsToTaskListView()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                ApplicationReferenceNumber = string.Empty // Simulate empty ApplicationReferenceNumber
            });

        // Act
        var result = await SystemUnderTest.PayByPhone();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.ActionName.Should().Be("RegistrationTaskList");
    }

    [Test]
    public async Task WhenFileUploadStatus_NotCompleted_PayByBankTransfer_RedirectsToTaskList()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            RegulatorNation = "GB-ENG",
            ApplicationStatus = ApplicationStatusType.FileUploaded
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.PayByBankTransfer() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    public void PayByBankTransfer_ReturnsCorrectView()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            TotalAmountOutstanding = 14030,
            ApplicationReferenceNumber = "AP-REF123456",
            RegulatorNation = "GB-ENG",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.PayByBankTransfer().Result;
        var model = (result as ViewResult).Model as PaymentOptionPayByBankTransferViewModel;

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<PaymentOptionPayByBankTransferViewModel>();
        model.RegulatorNation.Should().Be("GB-ENG");
        model.TotalAmountOutstanding.Should().Be(14030);
        model.ApplicationReferenceNumber.Should().Be("AP-REF123456");
    }

    [Test]
    public async Task WhenNoSessionExists_PayByBankTransfer_RedirectsToTaskList()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((RegistrationApplicationSession) null); // Simulate null session

        // Act
        var result = await SystemUnderTest.PayByBankTransfer() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    public async Task WhenNoRegistrationSessionExists_PayByBankTransfer_RedirectsToTaskList()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((RegistrationApplicationSession) null); // Simulate null session

        // Act
        var result = await SystemUnderTest.PayByBankTransfer() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    public async Task WhenNoApplicationReferenceNumberExists_PayByBankTransfer_RedirectsToTaskList()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            ApplicationReferenceNumber = string.Empty
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.PayByBankTransfer() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [TestCase("GB-NIR", "NorthernIreland")]
    [TestCase("GB-SCT", "Scotland")]
    [TestCase("GB-WLS", "Wales")]
    public void PayByBankTransfer_ShouldSetBackLink_To_RegistrationFeeCalculation_ForOtherNationsThatIsNotEngland(string nationCode, string nationName)
    {
        var expectedJourney = new List<string>() { PagePaths.RegistrationFeeCalculations, PagePaths.PaymentOptionPayByBankTransfer, null };
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.SelectPaymentOptions],
            TotalAmountOutstanding = 14030,
            ApplicationReferenceNumber = "AP-REF123456",
            RegulatorNation = nationCode,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.PayByBankTransfer().Result;
        var model = (result as ViewResult).Model as PaymentOptionPayByBankTransferViewModel;
        var webpageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<PaymentOptionPayByBankTransferViewModel>();

        model.RegulatorNation.Should().Be(nationCode);
        model.NationName.Should().Be(nationName);
        model.TotalAmountOutstanding.Should().Be(14030);
        model.ApplicationReferenceNumber.Should().Be("AP-REF123456");

        Session.Journey.Should().BeEquivalentTo(expectedJourney);
        webpageBackLink.Should().Be(PagePaths.RegistrationFeeCalculations);
    }

    [Test]
    public void PayByBankTransfer_ShouldSetBackLink_To_SelectPaymentOptions_ForEnglandNation()
    {
        var expectedJourney = new List<string>() { PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByBankTransfer, null };

        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.SelectPaymentOptions],
            TotalAmountOutstanding = 14030,
            ApplicationReferenceNumber = "AP-REF123456",
            RegulatorNation = "GB-ENG",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = SystemUnderTest.PayByBankTransfer().Result;
        var model = (result as ViewResult).Model as PaymentOptionPayByBankTransferViewModel;
        var webpageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<PaymentOptionPayByBankTransferViewModel>();
        model.RegulatorNation.Should().Be("GB-ENG");
        model.NationName.Should().Be("England");
        model.TotalAmountOutstanding.Should().Be(14030);
        model.ApplicationReferenceNumber.Should().Be("AP-REF123456");

        Session.Journey.Should().BeEquivalentTo(expectedJourney);
        webpageBackLink.Should().Be(PagePaths.SelectPaymentOptions);
    }

    [Test]
    public async Task WhenFileUploadStatus_NotCompleted_PayOnline_RedirectsToTaskList()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            RegulatorNation = "GB-ENG",
            ApplicationStatus = ApplicationStatusType.FileUploaded
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.PayOnline() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(RegistrationApplicationController.RegistrationTaskList));
    }

    [Test]
    public async Task PayOnline_ReturnsCorrectViewModel()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";
        const string expectedPaymentLink = "https://example/secure/9defb517-66f8-45cd-8d9b-20e571b76fb5";
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.PaymentOptionPayOnline],
            TotalAmountOutstanding = 2045600,
            ApplicationReferenceNumber = "1234EFGH",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() },
            RegistrationJourney = RegistrationJourney.CsoLargeProducer
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        RegistrationApplicationService.Setup(x => x.InitiatePayment(It.IsAny<ClaimsPrincipal>(), It.IsAny<ISession>())).ReturnsAsync(expectedPaymentLink);
        // Act
        var result = await SystemUnderTest.PayOnline() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayOnlineViewModel>();

        result.Model.As<PaymentOptionPayOnlineViewModel>().Should().BeEquivalentTo(new
        {
            Session.TotalAmountOutstanding,
            Session.ApplicationReferenceNumber,
            PaymentLink = expectedPaymentLink,
            TotalAmount = (Session.TotalAmountOutstanding /100).ToString("#,##0.00"),
            RegistrationJourney = RegistrationJourney.CsoLargeProducer,
            ShowRegistrationCaption = true
        });
    }

    [Test]
    public async Task PayOnline_Calls_InitiatePayment_Returns_PaymentLink_To_ViewModel()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";
        const string expectedPaymentLink = "https://example/secure/9defb517-66f8-45cd-8d9b-20e571b76fb5";
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.PaymentOptionPayOnline],
            TotalAmountOutstanding = 2045600,
            ApplicationReferenceNumber = "1234EFGH",
            SubmissionId = Guid.NewGuid(),
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        RegistrationApplicationService.Setup(x => x.CreateRegistrationApplicationEvent(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<string>(), SubmissionType.RegistrationFeePayment));

        RegistrationApplicationService.Setup(x => x.InitiatePayment(It.IsAny<ClaimsPrincipal>(), It.IsAny<ISession>())).ReturnsAsync(expectedPaymentLink);

        // Act
        var result = await SystemUnderTest.PayOnline() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayOnlineViewModel>();
    }

    [Test]
    public async Task PayOnline_When_InitiatePayment_Returns_False_ShouldRedirectTo_HandleThrownExceptions()
    {
        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.PaymentOptionPayOnline],
            RegistrationFeePaymentMethod = null,
            TotalAmountOutstanding = 2045600,
            ApplicationReferenceNumber = "1234EFGH",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            IsSubmitted = true,
            SubmissionId = Guid.NewGuid(),
            RegistrationFeeCalculationDetails = [new RegistrationFeeCalculationDetails { OrganisationId = "1", OrganisationSize = "L" }],
            LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        RegistrationApplicationService.Setup(x => x.InitiatePayment(It.IsAny<ClaimsPrincipal>(), It.IsAny<ISession>())).ReturnsAsync(string.Empty);
        RegistrationApplicationService.Setup(x => x.ValidateRegistrationYear(It.IsAny<string>(), It.IsAny<bool>())).Returns(DateTime.Now.Year);


        // Act
        var result = await SystemUnderTest.PayOnline() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ErrorController.HandleThrownExceptions));

        RegistrationApplicationService.Verify(x => x.InitiatePayment(It.IsAny<ClaimsPrincipal>(), It.IsAny<ISession>()), Times.Once);

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [TestCase(RegistrationJourney.CsoLargeProducer)]
    [TestCase(RegistrationJourney.CsoSmallProducer)]
    [TestCase(null)]
    public async Task RedirectToFileUpload_ReturnsCorrectView_WithRouteParams(RegistrationJourney? expectedRegistrationJourney)
    {
        // Arrange
        var expectedDataPeriod = "April 2025 to March 2026";
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.FileUploadCompanyDetailsSubLanding],
            SubmissionPeriod = expectedDataPeriod,
            Period = new SubmissionPeriod { DataPeriod = expectedDataPeriod },
            RegistrationJourney = expectedRegistrationJourney
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        // Act
        var result = await SystemUnderTest.RedirectToFileUpload() as RedirectToActionResult;

        // Assert
        result!.ActionName.Should().Be(nameof(FileUploadCompanyDetailsController.Get));
        result.RouteValues!["registrationJourney"].Should().Be(expectedRegistrationJourney);
        result.RouteValues!["dataPeriod"].Should().Be(expectedDataPeriod);
        RegistrationApplicationService.Verify(x => x.SetRegistrationFileUploadSession(It.IsAny<ISession>(), _userData.Organisations[0].OrganisationNumber, It.IsAny<int>(), It.IsAny<bool?>()), Times.Once);
    }

    [TestCase(ApplicationStatusType.NotStarted, "Basic User", "FileUploadCompanyDetails", null)]
    [TestCase(ApplicationStatusType.NotStarted, "Delegated Person", "FileUploadCompanyDetails", null)]
    [TestCase(ApplicationStatusType.NotStarted, "Approved Person", "FileUploadCompanyDetails", null)]
    [TestCase(ApplicationStatusType.FileUploaded, "Basic User", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    [TestCase(ApplicationStatusType.FileUploaded, "Delegated Person", "ReviewCompanyDetails", "submissionId")]
    [TestCase(ApplicationStatusType.FileUploaded, "Approved Person", "ReviewCompanyDetails", "submissionId")]
    [TestCase(ApplicationStatusType.SubmittedToRegulator, "Basic User", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    [TestCase(ApplicationStatusType.SubmittedToRegulator, "Delegated Person", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    [TestCase(ApplicationStatusType.SubmittedToRegulator, "Approved Person", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    [TestCase(ApplicationStatusType.SubmittedAndHasRecentFileUpload, "Basic User", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    [TestCase(ApplicationStatusType.SubmittedAndHasRecentFileUpload, "Delegated Person", "ReviewCompanyDetails", "submissionId")]
    [TestCase(ApplicationStatusType.SubmittedAndHasRecentFileUpload, "Approved Person", "ReviewCompanyDetails", "submissionId")]
    public async Task RedirectToRightAction_RedirectsToCorrectAction(
        ApplicationStatusType status,
        string role,
        string expectedController,
        string expectedRouteValueKey)
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(new RegistrationApplicationSession
            {
                SubmissionId = submissionId,
                ApplicationStatus = status,
                SubmissionPeriod = SubmissionPeriod,
                Period = new SubmissionPeriod { DataPeriod = SubmissionPeriod },
                Journey =
                [
                    PagePaths.FileUploadCompanyDetailsSubLanding,
                    PagePaths.FileUploadCompanyDetails,
                    PagePaths.FileUploadBrands,
                    PagePaths.FileUploadPartnerships
                ]
            });

        SetupUserData(new UserData { ServiceRole = role, Organisations = [new Organisation { OrganisationRole = OrganisationRoles.Producer }] });

        // Act
        var result = await SystemUnderTest.RedirectToFileUpload() as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(expectedController);
        if (expectedRouteValueKey != null)
        {
            result.RouteValues.Should().ContainKey(expectedRouteValueKey);
            result.RouteValues[expectedRouteValueKey].Should().Be(submissionId);
        }

        RegistrationApplicationService.Verify(x => x.SetRegistrationFileUploadSession(It.IsAny<ISession>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool?>()), Times.Once);
    }

    [Test]
    public async Task WhenComplianceSchemeRegistrationDataHasBeenSubmitted_RegistrationFeeCalculations_ReturnsComplianceSchemeFeeCalculationBreakdownViewModel()
    {
        SetupBase(GetUserData("Compliance Scheme"));

        // Arrange
        var registrationApplicationDetails = new RegistrationApplicationSession
        {
            SubmissionId = Guid.NewGuid(),
            ApplicationReferenceNumber = "TestRef",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = null,
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid(), SubmittedDateTime = DateTime.Now },
            RegistrationFeeCalculationDetails = _feeCalculationDetails,
            IsSubmitted = true,
            SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "Test", RowNumber = 1 }
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationDetails);

        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.RegistrationFeeCalculations],
            ApplicationStatus = registrationApplicationDetails.ApplicationStatus,
            RegistrationFeeCalculationDetails = registrationApplicationDetails.RegistrationFeeCalculationDetails,
            ApplicationReferenceNumber = registrationApplicationDetails.ApplicationReferenceNumber,
            RegistrationFeePaymentMethod = registrationApplicationDetails.RegistrationFeePaymentMethod,
            RegulatorNation = registrationApplicationDetails.RegulatorNation,
            SubmissionId = registrationApplicationDetails.SubmissionId,
            SelectedComplianceScheme = registrationApplicationDetails.SelectedComplianceScheme,
            RegistrationJourney = RegistrationJourney.CsoLargeProducer
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        RegistrationApplicationService.Setup(x => x.GetComplianceSchemeRegistrationFees(It.IsAny<ISession>())).ReturnsAsync(new ComplianceSchemeFeeCalculationBreakdownViewModel
        {
            RegistrationFee = _complianceSchemeCalculationResponse.ComplianceSchemeRegistrationFee,
            SmallProducersFee = 0,
            SmallProducersCount = 0,
            LargeProducersFee = 9000,
            LargeProducersCount = 1,
            OnlineMarketplaceFee = 7000,
            OnlineMarketplaceCount = 1,
            SubsidiaryCompanyFee = 11000,
            SubsidiaryCompanyCount = 2,
            TotalPreviousPayments = _complianceSchemeCalculationResponse.PreviousPayment,
            TotalFeeAmount = _complianceSchemeCalculationResponse.TotalFee,
            IsRegistrationFeePaid = Session.IsRegistrationFeePaid
        });

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<ComplianceSchemeFeeCalculationBreakdownViewModel>();
        result.Model.As<ComplianceSchemeFeeCalculationBreakdownViewModel>().Should().BeEquivalentTo(new ComplianceSchemeFeeCalculationBreakdownViewModel
        {
            LargeProducersCount = 1,
            LargeProducersFee = 9000,
            OnlineMarketplaceCount = 1,
            OnlineMarketplaceFee = 7000,
            RegistrationFee = 20000,
            IsRegistrationFeePaid = false,
            SmallProducersCount = 0,
            SmallProducersFee = 0,
            SubsidiaryCompanyCount = 2,
            SubsidiaryCompanyFee = 11000,
            TotalFeeAmount = 12345,
            TotalPreviousPayments = 23456,
            RegistrationJourney = RegistrationJourney.CsoLargeProducer
        });
    }

    [Test]
    public async Task WhenComplianceSchemeNotFound_RegistrationFeeCalculations_RedirectsTo_HandleThrownExceptions()
    {
        SetupBase(GetUserData("Compliance Scheme"));

        // Arrange
        Session = new RegistrationApplicationSession
        {
            Journey = [PagePaths.RegistrationFeeCalculations],
            SubmissionId = Guid.NewGuid(),
            ApplicationReferenceNumber = "TestRef",
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationFeePaymentMethod = null,
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid(), SubmittedDateTime = DateTime.Now },
            RegistrationFeeCalculationDetails = _feeCalculationDetails,
            IsSubmitted = true,
            RegistrationJourney = null
            //SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid(), NationId = 1, Name = "test", RowNumber = 1 }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(Session);

        RegistrationApplicationService.Setup(x => x.GetComplianceSchemeRegistrationFees(It.IsAny<ISession>())).ReturnsAsync((ComplianceSchemeFeeCalculationBreakdownViewModel) null);

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var viewResult = result as RedirectToActionResult;
        viewResult.ActionName.Should().Be(nameof(ErrorController.HandleThrownExceptions));
        LoggerMock.VerifyLog(x => x.LogWarning("Error in Getting Registration Fees Details for SubmissionId {SubmissionId}, OrganisationNumber {OrganisationNumber}, ApplicationReferenceNumber {ApplicationReferenceNumber}, selectedComplianceSchemeId {selectedComplianceSchemeId}", Session.SubmissionId, _userData.Organisations[0].OrganisationNumber!, Session.ApplicationReferenceNumber, It.IsAny<Guid?>()));
    }

    [Test]
    public async Task RedirectToUpdateRegistrationGuidance_ReturnsCorrectView()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(new RegistrationApplicationSession());
        var registrationApplicationSession = new RegistrationApplicationSession
        {
            RegistrationReferenceNumber = null
        };
        RegistrationApplicationService.Setup(x => x.GetRegistrationApplicationSession(It.IsAny<ISession>(), It.IsAny<Organisation>(), It.IsAny<int>(), It.IsAny<RegistrationJourney?>(), It.IsAny<bool?>()))
            .ReturnsAsync(registrationApplicationSession);

        // Act
        var result = await SystemUnderTest.UpdateRegistrationGuidance();
        // Assert
        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeNullOrEmpty();
    }

    private void ValidateViewModel(object model)
    {
        var validationContext = new ValidationContext(model, null, null);
        List<ValidationResult> validationResults = [];
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        foreach (var validationResult in validationResults)
        {
            SystemUnderTest.ControllerContext.ModelState.AddModelError(String.Join(", ", validationResult.MemberNames), validationResult.ErrorMessage);
        }
    }
}
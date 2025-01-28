namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.ComponentModel.DataAnnotations;
using Application.Constants;
using Application.DTOs;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.PaymentCalculations;
using Application.DTOs.Submission;
using Application.Enums;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UI.Controllers;
using UI.Controllers.FrontendSchemeRegistration;
using UI.Extensions;
using UI.Sessions;
using UI.ViewModels;

[TestFixture]
public class FrontendSchemeRegistrationControllerTests : FrontendSchemeRegistrationTestBase
{
    private const string OrganisationName = "Acme Org Ltd";
    private const string SubmissionPeriod = "Jul to Dec 23";
    private readonly Guid _organisationId = Guid.NewGuid();
    private static readonly ProducerDetailsDto ProducerDetailsDto = new()
    {
        ProducerSize = "Large",
        IsOnlineMarketplace = true,
        NumberOfSubsidiaries = 54,
        NumberOfSubsidiariesBeingOnlineMarketPlace = 29
    };
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
            UnitOnlineMarketplaceFee = 257900,
        }
    };
    private static readonly ComplianceSchemePaymentCalculationResponse _complianceSchemeCalculationResponse = new()
    {
        ComplianceSchemeMembersWithFees = [new ComplianceSchemePaymentCalculationResponseMember {
            MemberId = "123",
            MemberLateRegistrationFee = 5000,
            MemberOnlineMarketPlaceFee = 7000,
            MemberRegistrationFee = 9000,
            SubsidiariesFee = 11000,
            SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown {
                CountOfOnlineMarketplaceSubsidiaries = 1,
                TotalSubsidiariesOnlineMarketplaceFee = 2000,
                UnitOnlineMarketplaceFee = 3000,
                FeeBreakdowns = [new FeeBreakdown {
                    BandNumber = 5,
                    TotalPrice = 6000,
                    UnitCount = 7,
                    UnitPrice = 8000
                }]
            },
            TotalMemberFee = 15000
        }],
        TotalFee = 12345,
        PreviousPayment = 23456,
        ComplianceSchemeRegistrationFee = 20000,
        OutstandingPayment = 30000
    };
    private static readonly ComplianceSchemeDetailsDto _complianceSchemeDetailsDto = new()
    {
        Members = [new ComplianceSchemeDetailsMemberDto {
             IsLateFeeApplicable = true,
             IsOnlineMarketplace = false,
             MemberId = "123",
             MemberType = "Large",
             NumberOfSubsidiaries = 2,
             NumberOfSubsidiariesBeingOnlineMarketPlace = 3
        }]
    };

    private static readonly ProducerDetailsDto SmallProducerDetailsDto = new()
    {
        ProducerSize = "Small",
        IsOnlineMarketplace = false,
        NumberOfSubsidiaries = 0,
        NumberOfSubsidiariesBeingOnlineMarketPlace = 0
    };
    private static readonly ComplianceSchemeDetailsDto _complianceSchemeDetailsSmallProducerDto = new()
    {
        Members = [new ComplianceSchemeDetailsMemberDto {
             IsLateFeeApplicable = true,
             IsOnlineMarketplace = false,
             MemberId = "12345",
             MemberType = "Small",
             NumberOfSubsidiaries = 0,
             NumberOfSubsidiariesBeingOnlineMarketPlace = 0
        }]
    };
    private static readonly ComplianceSchemePaymentCalculationResponse _complianceSchemeCalculationSmallProducerResponse = new()
    {
        ComplianceSchemeMembersWithFees = [new ComplianceSchemePaymentCalculationResponseMember {
            MemberId = "12345",
            MemberLateRegistrationFee = 5000,
            MemberOnlineMarketPlaceFee = 7000,
            MemberRegistrationFee = 9000,
            TotalMemberFee = 0
        }],
        TotalFee = 12345,
        PreviousPayment = 23456,
        ComplianceSchemeRegistrationFee = 20000,
        OutstandingPayment = 30000
    };

    private UserData _userData;

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
            Organisations = new()
            {
                new()
                {
                    Name = OrganisationName,
                    OrganisationNumber = "123456",
                    Id = _organisationId,
                    OrganisationRole = organisationRole
                }
            }
        };
    }

    [Test]
    public void ApprovedPersonCreated_ReturnsCorrectViewAndModel()
    {
        // Arrange
        var message = "some_new_message";
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string>
                {
                    PagePaths.Root,
                },
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.ApprovedPersonCreated(message).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        FrontEndSchemeRegistrationSession.RegistrationSession.NotificationMessage.Should().Be(message);
    }

    [Test]
    public async Task RegistrationTaskList_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        SetupBase(GetUserData("Compliance Scheme"));

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationTaskList },
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = null,
                ApplicationStatus = ApplicationStatusType.NotStarted,
                RegistrationFeePaymentMethod = null,
            }
        };
        var period = new SubmissionPeriod { StartMonth = "April", EndMonth = "September", Year = "2025" };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), period)).Returns(reference);

        SubmissionService.Setup(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()));

        SubmissionService
            .Setup(s => s.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int?>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<RegistrationSubmission>());

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be(PagePaths.ComplianceSchemeLanding);
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        PaymentCalculationService.Verify(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), It.IsAny<SubmissionPeriod>()), Times.Never);
        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.NotStarted
        });
    }

    [Test]
    public async Task RegistrationTaskList_SubmitRegistrationData_When_FileUploaded_Is_PendingState_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationTaskList }

            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                FileReachedSynapse = false,
                ApplicationStatus = ApplicationStatusType.FileUploaded,
                SubmissionId = Guid.NewGuid(),
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
                ApplicationReferenceNumber = reference,
            }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        SubmissionService.Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()));

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = null,
            CsoMemberDetails = null,
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = null,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.FileUploaded,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };
        SubmissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        PaymentCalculationService.Verify(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), It.IsAny<SubmissionPeriod>()), Times.Never);
        SubmissionService.Verify(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Pending,
            PaymentViewStatus = RegistrationTaskListStatus.CanNotStartYet,
            AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
            FileReachedSynapse = false,
            ApplicationStatus = ApplicationStatusType.FileUploaded
        });
    }

    [Test]
    public async Task RegistrationTaskList_SetsSession_RegistrationFeePaid_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationTaskList }

            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = Guid.NewGuid(),
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
                ApplicationReferenceNumber = reference,
            }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        SubmissionService.Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()));

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = new ProducerDetailsDto { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, ProducerSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = 1234 },
            CsoMemberDetails = null,
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };
        SubmissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;
        var pageBackLink = SystemUnderTest.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be(PagePaths.HomePageSelfManaged);
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        PaymentCalculationService.Verify(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), It.IsAny<SubmissionPeriod>()), Times.Never);
        SubmissionService.Verify(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Completed,
            PaymentViewStatus = RegistrationTaskListStatus.Completed,
            AdditionalDetailsStatus = RegistrationTaskListStatus.NotStarted,
            FileReachedSynapse = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
        });
    }

    [Test]
    public async Task RegistrationTaskList_SetsSession_RegistrationApplicationSubmitted_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationTaskList }

            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = Guid.NewGuid(),
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
                ApplicationReferenceNumber = reference,
            }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        SubmissionService.Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()));

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = new ProducerDetailsDto { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, ProducerSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = 1234 },
            CsoMemberDetails = null,
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = "Test",
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };
        SubmissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.IsLateFeeApplicable.Should().BeFalse();
        PaymentCalculationService.Verify(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), It.IsAny<SubmissionPeriod>()), Times.Never);
        SubmissionService.Verify(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Completed,
            PaymentViewStatus = RegistrationTaskListStatus.Completed,
            AdditionalDetailsStatus = RegistrationTaskListStatus.Completed,
            FileReachedSynapse = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
        });
    }

    [Test]
    public async Task RegistrationTaskList_SetsSession_RegistrationApplicationApproved_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationTaskList }

            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.ApprovedByRegulator,
                SubmissionId = Guid.NewGuid(),
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
                ApplicationReferenceNumber = reference,
            }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        SubmissionService.Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()));

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = new ProducerDetailsDto { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, ProducerSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = 1234 },
            CsoMemberDetails = null,
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = "PayOnline",
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.ApprovedByRegulator,
            RegistrationApplicationSubmittedComment = "Test",
            RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
        };
        SubmissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.IsLateFeeApplicable.Should().BeFalse();
        PaymentCalculationService.Verify(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), It.IsAny<SubmissionPeriod>()), Times.Never);
        SubmissionService.Verify(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Completed,
            PaymentViewStatus = RegistrationTaskListStatus.Completed,
            AdditionalDetailsStatus = RegistrationTaskListStatus.Completed,
            FileReachedSynapse = true,
            ApplicationStatus = ApplicationStatusType.ApprovedByRegulator
        });
    }


    [Test]
    public async Task RegistrationTaskList_SetsSession_With_ComplianceScheme_RegistrationFeePaid_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationTaskList },
                SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = submissionId,
                ApplicationStatus = ApplicationStatusType.NotStarted,
                RegistrationFeePaymentMethod = null,
            }
        };
        var period = new SubmissionPeriod { StartMonth = "April", EndMonth = "September", Year = "2025" };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), period)).Returns(reference);

        SubmissionService.Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()));

        SubmissionService
            .Setup(s => s.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int?>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<RegistrationSubmission>());
        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.IsLateFeeApplicable.Should().BeFalse();
        PaymentCalculationService.Verify(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), It.IsAny<SubmissionPeriod>()), Times.Never);
        SubmissionService.Verify(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.NotStarted,
            PaymentViewStatus = RegistrationTaskListStatus.CanNotStartYet,
            AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
        });
    }

    [Test]
    public async Task RegistrationTaskList_Sets_ApplicationReferenceNumber_ReturnsCorrectViewAndModel()
    {
        // Arrange
        const string reference = "PEPR00002125P1";
        var submissionId = Guid.NewGuid();

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationTaskList }

            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = Guid.NewGuid(),
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), It.IsAny<SubmissionPeriod>())).Returns(reference);

        SubmissionService.Setup(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()));

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = new ProducerDetailsDto { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, ProducerSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = 1234 },
            CsoMemberDetails = null,
            ApplicationReferenceNumber = null,
            SubmissionId = submissionId,
            RegistrationFeePaymentMethod = null,
            IsSubmitted = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
            RegistrationApplicationSubmittedComment = null,
            RegistrationApplicationSubmittedDate = null
        };
        SubmissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var result = await SystemUnderTest.RegistrationTaskList() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<RegistrationTaskListViewModel>();
        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.IsLateFeeApplicable.Should().BeFalse();
        PaymentCalculationService.Verify(x => x.CreateApplicationReferenceNumber(false, 0, It.IsAny<string>(), It.IsAny<SubmissionPeriod>()), Times.Once);
        SubmissionService.Verify(x => x.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        result.Model.As<RegistrationTaskListViewModel>().Should().BeEquivalentTo(new RegistrationTaskListViewModel
        {
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            FileUploadStatus = RegistrationTaskListStatus.Completed,
            PaymentViewStatus = RegistrationTaskListStatus.NotStarted,
            AdditionalDetailsStatus = RegistrationTaskListStatus.CanNotStartYet,
            FileReachedSynapse = true,
            ApplicationStatus = ApplicationStatusType.SubmittedToRegulator
        });

        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.ApplicationReferenceNumber.Should().Be(reference);
    }

    [Test]
    public async Task WhenRegistrationDataHasBeenSubmitted_RegistrationFeeCalculations_ReturnsFeeCalculationBreakdownViewModel()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationFeeCalculations }

            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "TestRef",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                FileReachedSynapse = true,
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid(), SubmittedDateTime = DateTime.Now },
                ProducerDetails = ProducerDetailsDto,
                CsoMemberDetails = null,
                IsSubmitted = true,
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService
            .Setup(s => s.GetProducerRegistrationFees(
                FrontEndSchemeRegistrationSession.RegistrationApplicationSession.ProducerDetails,
                "TestRef",
                FrontEndSchemeRegistrationSession.RegistrationApplicationSession.IsLateFeeApplicable,
                _userData.Organisations[0].Id,
                It.IsAny<DateTime>()))
            .ReturnsAsync(CalculationResponse);

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<FeeCalculationBreakdownViewModel>();
        result.Model.As<FeeCalculationBreakdownViewModel>().Should().BeEquivalentTo(new FeeCalculationBreakdownViewModel
        {
            ProducerSize = ProducerDetailsDto.ProducerSize,
            IsOnlineMarketplace = ProducerDetailsDto.IsOnlineMarketplace,
            ProducerLateRegistrationFee = CalculationResponse.ProducerLateRegistrationFee,
            NumberOfSubsidiaries = ProducerDetailsDto.NumberOfSubsidiaries,
            NumberOfSubsidiariesBeingOnlineMarketplace = ProducerDetailsDto.NumberOfSubsidiariesBeingOnlineMarketPlace,
            BaseFee = CalculationResponse.ProducerRegistrationFee,
            OnlineMarketplaceFee = CalculationResponse.ProducerOnlineMarketPlaceFee,
            TotalSubsidiaryFee = CalculationResponse.SubsidiariesFee - CalculationResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
            TotalPreviousPayments = CalculationResponse.PreviousPayment,
            TotalFeeAmount = CalculationResponse.TotalFee,
            RegistrationFeePaid = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.RegistrationFeePaid,
            TotalSubsidiaryOnlineMarketplaceFee = CalculationResponse.SubsidiariesFeeBreakdown.TotalSubsidiariesOnlineMarketplaceFee,
        });
    }

    [Test]
    public void WhenProducerNotFound_RegistrationFeeCalculations_RedirectsToTaskListView()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationFeeCalculations }

            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "TestRef",
                FileReachedSynapse = true,
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.GetProducerRegistrationFees(It.IsAny<ProducerDetailsDto>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<DateTime>())).ReturnsAsync((PaymentCalculationResponse)null);

        // Act
        var result = SystemUnderTest.RegistrationFeeCalculations().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var viewResult = result as RedirectToActionResult;
        viewResult.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
        LoggerMock.VerifyLog(x => x.LogWarning("Error in Getting ProducerRegistrationFees for ApplicationReferenceNumber {Number}", FrontEndSchemeRegistrationSession.RegistrationApplicationSession.ApplicationReferenceNumber));
    }

    [Test]
    public async Task WhenRegistrationDataIsNotSubmitted_RegistrationFeeCalculations_RedirectsToPreviousView()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationFeeCalculations }

            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = null,
                ApplicationReferenceNumber = null,
            }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.GetProducerRegistrationFees(It.IsAny<ProducerDetailsDto>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<DateTime>())).ReturnsAsync(CalculationResponse);

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }

    [Test]
    public async Task RegistrationFeeCalculations_ShouldCallGetProducerDetails_WithCorrectOrganisationNumber()
    {
        // Arrange
        var producerDetails = new ProducerDetailsDto
        {
            NumberOfSubsidiariesBeingOnlineMarketPlace = 1,
            ProducerSize = "Large",
            IsOnlineMarketplace = true,
            NumberOfSubsidiaries = 1,
            OrganisationId = 1234
        };

        var mockSession = new FrontendSchemeRegistrationSession
        {
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "456",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                FileReachedSynapse = true,
                ProducerDetails = producerDetails,
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now }
            }
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

        PaymentCalculationService
            .Setup(s => s.GetProducerRegistrationFees(producerDetails, "456", false, _userData.Organisations[0].Id, It.IsAny<DateTime>()))
            .ReturnsAsync(registrationFeesResponse);

        // Act
        await SystemUnderTest.RegistrationFeeCalculations();

        // Assert
        PaymentCalculationService.Verify(
            s => s.GetProducerRegistrationFees(It.IsAny<ProducerDetailsDto>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid>(), It.IsAny<DateTime>()),
            Times.Once,
            "GetProducerRegistrationFees should be called exactly once with the correct organisation number"
        );
    }

    [Test]
    public async Task RegistrationFeeCalculations_ShouldNotCallGetProducerDetails_WhenRegistrationDataNotSubmitted()
    {
        // Arrange
        var mockSession = new FrontendSchemeRegistrationSession
        {
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "TestRef",
            }
        };

        SessionManagerMock
            .Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(mockSession);

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(SystemUnderTest.RegistrationTaskList));

        //PaymentCalculationService.Verify(
        //    s => s.GetProducerDetails(It.IsAny<string>()),
        //    Times.Never,
        //    "GetProducerDetails should not be called if registration data is not submitted"
        //);
    }

    [Test]
    public async Task WhenRegistrationDataNotSubmitted_ProducerRegistrationGuidance_ReturnsCorrectViewModel()
    {
        // Arrange
        const string nationCode = "GB-ENG";
        var complianceSchemes = new List<ComplianceSchemeDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Biffpack (Environment Agency)", NationId = 1 }
        };
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.ProducerRegistrationGuidance },
            }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        ComplianceSchemeService.Setup(x => x.GetOperatorComplianceSchemes(It.IsAny<Guid>())).ReturnsAsync(complianceSchemes);

        PaymentCalculationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync(nationCode);

        // Act
        var result = await SystemUnderTest.ProducerRegistrationGuidance() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<ProducerRegistrationGuidanceViewModel>();
        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.FileUploadStatus.Should().Be(RegistrationTaskListStatus.NotStarted);
        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.RegistrationFeePaid.Should().BeFalse();

        result.Model.As<ProducerRegistrationGuidanceViewModel>().Should().BeEquivalentTo(new ProducerRegistrationGuidanceViewModel
        {
            RegulatorNation = nationCode,
            ComplianceScheme = complianceSchemes[0].Name,
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
        const string NationCode = "GB-ENG";
        var submissionId = Guid.NewGuid();
        var reference = "TestRef";

        var complianceSchemes = new List<ComplianceSchemeDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Biffpack (Environment Agency)", NationId = 1 }
        };

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.ProducerRegistrationGuidance }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationReferenceNumber = "test",
                SubmissionId = submissionId
            }
        };

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        ComplianceSchemeService.Setup(x => x.GetOperatorComplianceSchemes(It.IsAny<Guid>())).ReturnsAsync(complianceSchemes);

        PaymentCalculationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync(NationCode);

        var registrationApplicationDetails = new RegistrationApplicationDetails
        {
            LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
            ProducerDetails = new ProducerDetailsDto { NumberOfSubsidiariesBeingOnlineMarketPlace = 1, IsOnlineMarketplace = true, ProducerSize = "Large", NumberOfSubsidiaries = 1, OrganisationId = 1234 },
            ApplicationReferenceNumber = reference,
            SubmissionId = submissionId,
            IsSubmitted = true,
            ApplicationStatus = statusType
        };
        SubmissionService.Setup(x => x.GetRegistrationApplicationDetails(It.IsAny<GetRegistrationApplicationDetailsRequest>())).ReturnsAsync(registrationApplicationDetails);

        // Act
        var result = SystemUnderTest.ProducerRegistrationGuidance().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.FileUploadStatus.Should().Be(RegistrationTaskListStatus.NotStarted);
        FrontEndSchemeRegistrationSession.RegistrationApplicationSession.RegistrationFeePaid.Should().BeFalse();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }

    [Test]
    [TestCase(ApplicationStatusType.NotStarted, false, false)]
    [TestCase(ApplicationStatusType.FileUploaded, false, false)]
    [TestCase(ApplicationStatusType.NotStarted, true, true)]
    public void WhenRegistrationDataNotSubmittedOrFeeNotPaid_AdditionalInformation_RedirectsToTaskListView(ApplicationStatusType dataSubmitted, bool feePaid, bool appSubmitted)
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = dataSubmitted,
                RegistrationFeePaymentMethod = feePaid ? "PayByPhone" : null,
                ApplicationReferenceNumber = "test",
                FileReachedSynapse = true,
                IsSubmitted = true,
                RegistrationApplicationSubmittedDate = appSubmitted ? DateTime.Now : null,
                SubmissionId = Guid.NewGuid(),
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.AdditionalInformation().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }

    [Test]
    [TestCase(ApplicationStatusType.AcceptedByRegulator)]
    [TestCase(ApplicationStatusType.ApprovedByRegulator)]
    [TestCase(ApplicationStatusType.SubmittedToRegulator)]
    public void WhenRegistrationDataSubmittedAndFeePaid_AdditionalInformation_RedirectsToSubmitRegistrationRequestView(ApplicationStatusType applicationStatusType)
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = applicationStatusType,
                RegistrationFeePaymentMethod = "PayByPhone",
                ApplicationReferenceNumber = "test",
                FileReachedSynapse = true,
                IsSubmitted = true,
                RegistrationApplicationSubmittedDate = DateTime.Now,
                SubmissionId = Guid.NewGuid(),
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.AdditionalInformation().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.SubmitRegistrationRequest));
    }

    [Test]
    public async Task WhenRegistrationDataSubmittedAndFeePaid_AdditionalInformation_ReturnsCorrectViewModel()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "TestRef",
                RegistrationFeePaymentMethod = "PayByPhone",
                RegulatorNation = "GB-SCT",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                FileReachedSynapse = true,
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.AdditionalInformation() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<AdditionalInformationViewModel>();

        result.Model.As<AdditionalInformationViewModel>().Should().BeEquivalentTo(new AdditionalInformationViewModel
        {
            IsComplianceScheme = false,
            RegulatorNation = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.RegulatorNation,
            OrganisationName = _userData.Organisations[0].Name,
            OrganisationNumber = _userData.Organisations[0].OrganisationNumber.ToReferenceNumberFormat(),
            ComplianceScheme = FrontEndSchemeRegistrationSession.RegistrationSession.SelectedComplianceScheme?.Name
        });
    }

    [Test]
    [TestCase("Approved Person")]
    [TestCase("Delegated Person")]
    public void WhenApplicationNotGrantedAndSubmissionExists_AdditionalInformationPostAction_CallsSubmitRegistrationApplicationAsync(string role)
    {
        // Arrange
        var submission = new RegistrationSubmission { IsSubmitted = true };
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData { ServiceRole = role },
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = Guid.NewGuid(),
                RegistrationFeePaymentMethod = "PayByPhone",
                FileReachedSynapse = true,
                RegistrationApplicationSubmittedDate = null,
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);
        SubmissionService.Setup(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()));

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.SubmitRegistrationRequest));
        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
    }

    [Test]
    [TestCase("Approved Person")]
    [TestCase("Delegated Person")]
    public void WhenApplicationNotGrantedAndSubmissionDoesNotExist_AdditionalInformationPostAction_NeverCallSubmitRegistrationApplicationAsync(string role)
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData { ServiceRole = role },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationApplicationSubmittedDate = null
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.SubmitRegistrationRequest));
        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void WhenApplicationHasBeenGrantedAndRegardlessOfSubmission_AdditionalInformationPostAction_NeverCallSubmitRegistrationApplicationAsync(bool valid)
    {
        // Arrange
        var submission = valid ? new RegistrationSubmission() : null;
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData { ServiceRole = "Approved Person" },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationApplicationSubmittedDate = DateTime.Now.AddMinutes(-5)
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.SubmitRegistrationRequest));
        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Never);
    }

    [Test]
    public void WhenApplicationHasBeenGranted_SubmitRegistration_ShouldRedirectToSubmitRegistrationRequest()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.AcceptedByRegulator,
                ApplicationReferenceNumber = "test",
                FileReachedSynapse = true,
                IsSubmitted = true,
                RegistrationApplicationSubmittedDate = DateTime.Now,
                SubmissionId = Guid.NewGuid(),
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.AdditionalInformation().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.SubmitRegistrationRequest));
    }

    [Test]
    [TestCase("Approved Person")]
    [TestCase("Delegated Person")]
    public void WhenPostActionCalledWithApprovedUser_AdditionalInformation_RedirectsToSubmitRegistrationRequest(string role)
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation },
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegulatorNation = "GB-SCT"
            },
            UserData = new UserData { ServiceRole = role }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.SubmitRegistrationRequest));
    }

    [Test]
    public void WhenPostActionCalledWithBasicUser_AdditionalInformation_RedirectsToUnauthorisedUserWarnings()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation },
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegulatorNation = "GB-SCT"
            },
            UserData = new UserData { ServiceRole = "Basic User" }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.AdditionalInformation(new AdditionalInformationViewModel { AdditionalInformationText = "Extra details" }).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.UnauthorisedUserWarnings));
    }

    [Test]
    public void WhenGetActionCalledWithProducerUser_UnauthorisedUserWarnings_ShouldReturnCorrectViewData()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation },
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
                RegistrationFeePaymentMethod = "PayByPhone",
                RegulatorNation = "GB-SCT"
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

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
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation },
                SelectedComplianceScheme = new ComplianceSchemeDto { Name = "Compliance Ltd" }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() },
                RegistrationFeePaymentMethod = "PayByPhone",
                RegulatorNation = "GB-SCT",
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

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
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.AdditionalInformation }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegulatorNation = nationCode,
                ApplicationReferenceNumber = "1234EFGH",
                ApplicationStatus = ApplicationStatusType.AcceptedByRegulator
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

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
            ApplicationStatus = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.ApplicationStatus,
            RegulatorNation = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.RegulatorNation,
            ApplicationReferenceNumber = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.ApplicationReferenceNumber
        });
    }

    [Test]
    public async Task WhenFileUploadStatus_NotCompleted_SelectPaymentOptions_RedirectsToTaskList()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.SelectPaymentOptions }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegulatorNation = "GB-ENG",
                ApplicationStatus = ApplicationStatusType.FileUploaded
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.SelectPaymentOptions() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }

    [Test]
    public void WhenEngland_SelectPaymentOptions_ReturnsCorrectViewAndModel()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.SelectPaymentOptions }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegulatorNation = "GB-ENG",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                SubmissionId = Guid.NewGuid(),
                FileReachedSynapse = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

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
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.SelectPaymentOptions }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegulatorNation = nationCode,
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.SelectPaymentOptions().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        (result as RedirectToActionResult).ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.PayByBankTransfer));
    }

    [Test]
    public void SelectPaymentOptions_OnSubmit_WhenNoPaymentOptionSelected_ReturnsInvalidModelState()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string>
                {
                    PagePaths.RegistrationFeeCalculations,
                    PagePaths.SelectPaymentOptions
                }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        var model = new SelectPaymentOptionsViewModel() { PaymentOption = null };

        ValidateViewModel(model);

        // Act
        var result = SystemUnderTest.SelectPaymentOptions(model).Result;

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result as ViewResult;

        viewResult.ViewData.ModelState["PaymentOption"].Errors.Count.Should().Be(1);
    }

    [TestCase((int)PaymentOptions.PayOnline, "PayOnline")]
    [TestCase((int)PaymentOptions.PayByBankTransfer, "PayByBankTransfer")]
    [TestCase((int)PaymentOptions.PayByPhone, "PayByPhone")]
    [TestCase(4, null)]
    public void SelectPaymentOptions_OnSubmit_RedirectsToView(int paymentOption, string actionName)
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string>
                {
                    PagePaths.SelectPaymentOptions
                }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

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
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayByPhone }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegulatorNation = "GB-ENG",
                ApplicationStatus = ApplicationStatusType.FileUploaded
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayByPhone() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }

    [Test]
    public async Task PayByPhone_ReturnsCorrectViewModel()
    {
        // Arrange
        const string viewName = "PaymentOptionPayByPhone";
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayByPhone }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                SubmissionId = Guid.NewGuid(),
                FileReachedSynapse = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayByPhone() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayByPhoneViewModel>();

        result.Model.As<PaymentOptionPayByPhoneViewModel>().Should().BeEquivalentTo(new PaymentOptionPayByPhoneViewModel
        {
            TotalAmountOutstanding = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.TotalAmountOutstanding,
            ApplicationReferenceNumber = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.ApplicationReferenceNumber
        });
    }

    [Test]
    public async Task PayByPhone_WhenSessionIsNull_RedirectsToTaskListView()
    {
        // Arrange
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((FrontendSchemeRegistrationSession)null); // Simulate null session

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
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = null // Simulate null RegistrationSession
            });

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
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationApplicationSession = new RegistrationApplicationSession
                {
                    ApplicationReferenceNumber = string.Empty // Simulate empty ApplicationReferenceNumber
                }
            });

        // Act
        var result = await SystemUnderTest.PayByPhone();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult.ActionName.Should().Be("RegistrationTaskList");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PayByPhoneSaveSession_PostAction_SavesRegistrationFeePaidStatus(bool isComplianceScheme)
    {
        // Arrange
        Guid? schemeId = isComplianceScheme ? Guid.NewGuid() : null;
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayByPhone }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid()
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        SubmissionService.Setup(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), schemeId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), SubmissionType.RegistrationFeePayment));

        // Act
        var result = await SystemUnderTest.PayByPhoneSaveSession() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));
        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.Is<string>(s => s == "PayByPhone"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
    }

    [Test]
    public async Task PayByPhoneSaveSession_Should_Redirect_ProducerHomePage_WhenUserIsProducer()
    {
        // Arrange
        const string viewName = "PaymentOptionPayByPhone";

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayByPhone },
                SelectedComplianceScheme = null
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayByPhoneSaveSession() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));

        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), null, null, It.Is<string>(s => s == "PayByPhone"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
        SubmissionService.VerifyNoOtherCalls();

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PayByPhoneSaveSession_Should_Redirect_ComplianceHomePage_WhenUserIsCSO()
    {
        // Arrange
        const string viewName = "PaymentOptionPayByPhone";

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayByPhone },
                SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayByPhoneSaveSession() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeMemberLandingController.Get));

        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), null, It.Is<string>(s => s == "PayByPhone"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
        SubmissionService.VerifyNoOtherCalls();

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public void PayByBankTransfer_ReturnsCorrectView()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                TotalAmountOutstanding = 14030,
                ApplicationReferenceNumber = "AP-REF123456",
                RegulatorNation = "GB-ENG",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                SubmissionId = Guid.NewGuid(),
                FileReachedSynapse = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

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
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((FrontendSchemeRegistrationSession)null);

        // Act
        var result = await SystemUnderTest.PayByBankTransfer() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }

    [Test]
    public async Task WhenNoRegistrationSessionExists_PayByBankTransfer_RedirectsToTaskList()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession { RegistrationSession = null };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayByBankTransfer() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }

    [Test]
    public async Task WhenNoApplicationReferenceNumberExists_PayByBankTransfer_RedirectsToTaskList()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationApplicationSession = new RegistrationApplicationSession { ApplicationReferenceNumber = string.Empty }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayByBankTransfer() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }

    [TestCase("GB-NIR", "NorthernIreland")]
    [TestCase("GB-SCT", "Scotland")]
    [TestCase("GB-WLS", "Wales")]
    public void PayByBankTransfer_ShouldSetBackLink_To_RegistrationFeeCalculation_ForOtherNationsThatIsNotEngland(string nationCode, string nationName)
    {
        var expectedJourney = new List<string>() { PagePaths.RegistrationFeeCalculations, PagePaths.PaymentOptionPayByBankTransfer, null };
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.SelectPaymentOptions }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                TotalAmountOutstanding = 14030,
                ApplicationReferenceNumber = "AP-REF123456",
                RegulatorNation = nationCode,
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                SubmissionId = Guid.NewGuid(),
                FileReachedSynapse = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

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

        FrontEndSchemeRegistrationSession.RegistrationSession.Journey.Should().BeEquivalentTo(expectedJourney);
        webpageBackLink.Should().Be(PagePaths.RegistrationFeeCalculations);
    }

    [Test]
    public void PayByBankTransfer_ShouldSetBackLink_To_SelectPaymentOptions_ForEnglandNation()
    {
        var expectedJourney = new List<string>() { PagePaths.SelectPaymentOptions, PagePaths.PaymentOptionPayByBankTransfer, null };

        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.SelectPaymentOptions }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                TotalAmountOutstanding = 14030,
                ApplicationReferenceNumber = "AP-REF123456",
                RegulatorNation = "GB-ENG",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                SubmissionId = Guid.NewGuid(),
                FileReachedSynapse = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
            }
        };

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

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

        FrontEndSchemeRegistrationSession.RegistrationSession.Journey.Should().BeEquivalentTo(expectedJourney);
        webpageBackLink.Should().Be(PagePaths.SelectPaymentOptions);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PayByBankTransferSaveSession_PostAction_SavesRegistrationFeePaidStatus(bool isComplianceScheme)
    {
        // Arrange
        Guid? schemeId = isComplianceScheme ? Guid.NewGuid() : null;
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayByBankTransfer }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 232323,
                ApplicationReferenceNumber = "APPREF1RRW3GH",
                SubmissionId = Guid.NewGuid()
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        SubmissionService.Setup(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), schemeId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), SubmissionType.RegistrationFeePayment));

        // Act
        var result = await SystemUnderTest.PayByBankTransferSaveSession() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));
        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.Is<string>(s => s == "PayByBankTransfer"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
    }

    [Test]
    public async Task PayByBankTransferSaveSession_Should_Redirect_ProducerHomePage_WhenUserIsProducer()
    {
        // Arrange
        const string viewName = "PaymentOptionPayByBankTransfer";

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayByBankTransfer },
                SelectedComplianceScheme = null
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayByBankTransferSaveSession() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));

        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), null, null, It.Is<string>(s => s == "PayByBankTransfer"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
        SubmissionService.VerifyNoOtherCalls();

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PayByBankTransferSaveSession_Should_Redirect_ComplianceHomePage_WhenUserIsCSO()
    {
        // Arrange
        const string viewName = "PaymentOptionPayByBankTransfer";

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayByBankTransfer },
                SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayByBankTransferSaveSession() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeMemberLandingController.Get));

        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), null, It.Is<string>(s => s == "PayByBankTransfer"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
        SubmissionService.VerifyNoOtherCalls();

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PayOnline_ReturnsCorrectViewModel()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";
        const string expectedPaymentLink = "https://example/secure/9defb517-66f8-45cd-8d9b-20e571b76fb5";
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayOnline }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                SubmissionId = Guid.NewGuid(),
                FileReachedSynapse = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync("GB-ENG");


        PaymentCalculationService.Setup(x => x.InitiatePayment(It.IsAny<PaymentInitiationRequest>())).ReturnsAsync(expectedPaymentLink);
        // Act
        var result = await SystemUnderTest.PayOnline() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayOnlineViewModel>();

        result.Model.As<PaymentOptionPayOnlineViewModel>().Should().BeEquivalentTo(new PaymentOptionPayOnlineViewModel
        {
            TotalAmountOutstanding = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.TotalAmountOutstanding,
            ApplicationReferenceNumber = FrontEndSchemeRegistrationSession.RegistrationApplicationSession.ApplicationReferenceNumber,
            PaymentLink = expectedPaymentLink
        });
    }

    [Test]
    public async Task PayOnline_Calls_InitiatePayment_Returns_PaymentLink_To_ViewModel()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";
        const string expectedPaymentLink = "https://example/secure/9defb517-66f8-45cd-8d9b-20e571b76fb5";
        var schemeId = Guid.NewGuid();
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayOnline }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid(),
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                FileReachedSynapse = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        SubmissionService.Setup(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), schemeId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), SubmissionType.RegistrationFeePayment));

        PaymentCalculationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync("GB-ENG");

        PaymentCalculationService.Setup(x => x.InitiatePayment(It.IsAny<PaymentInitiationRequest>())).ReturnsAsync(expectedPaymentLink);

        // Act
        var result = await SystemUnderTest.PayOnline() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayOnlineViewModel>();
    }

    [Test]
    public async Task PayOnline_When_InitiatePayment_Returns_False_ShouldRedirectToAction()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayOnline }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                IsSubmitted = true,
                SubmissionId = Guid.NewGuid(),
                FileReachedSynapse = true,
                ProducerDetails = new ProducerDetailsDto(),
                LastSubmittedFile = new LastSubmittedFileDetails { SubmittedDateTime = DateTime.Now, SubmittedByName = "test", FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.GetRegulatorNation(It.IsAny<Guid>())).ReturnsAsync("GB-ENG");

        PaymentCalculationService.Setup(x => x.InitiatePayment(It.IsAny<PaymentInitiationRequest>())).ReturnsAsync(string.Empty);

        // Act
        var result = await SystemUnderTest.PayOnline() as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));

        PaymentCalculationService.Verify(x => x.GetRegulatorNation(It.IsAny<Guid>()), Times.Once);
        PaymentCalculationService.Verify(x => x.InitiatePayment(It.IsAny<PaymentInitiationRequest>()), Times.Once);
        PaymentCalculationService.VerifyNoOtherCalls();

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PayOnlineSaveSession_Should_SaveSesison_Redirect_ProducerHomePage_WhenUserIsProducer()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayOnline },
                SelectedComplianceScheme = null
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayOnlineSaveSession(new PaymentOptionPayOnlineViewModel()) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));

        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), null, null, It.Is<string>(s => s == "PayOnline"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
        SubmissionService.VerifyNoOtherCalls();

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PayOnlineSaveSession_Should_Redirect_ProducerHomePage_WhenUserIsProducer()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayOnline },
                SelectedComplianceScheme = null
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayOnlineSaveSession(new PaymentOptionPayOnlineViewModel()) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.VisitHomePageSelfManaged));

        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), null, null, It.Is<string>(s => s == "PayOnline"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
        SubmissionService.VerifyNoOtherCalls();

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task PayOnlineSaveSession_Should_Redirect_ComplianceHomePage_WhenUserIsCSO()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";

        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.PaymentOptionPayOnline },
                SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() }
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                RegistrationFeePaymentMethod = null,
                TotalAmountOutstanding = 2045600,
                ApplicationReferenceNumber = "1234EFGH",
                SubmissionId = Guid.NewGuid(),
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid() }
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = await SystemUnderTest.PayOnlineSaveSession(new PaymentOptionPayOnlineViewModel()) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(ComplianceSchemeMemberLandingController.Get));

        SubmissionService.Verify(x => x.SubmitRegistrationApplicationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), null, It.Is<string>(s => s == "PayOnline"), It.IsAny<string>(), It.IsAny<SubmissionType>()), Times.Once);
        SubmissionService.VerifyNoOtherCalls();

        SessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
        SessionManagerMock.VerifyNoOtherCalls();
    }

    [Test]
    public void RedirectToFileUpload_ReturnsCorrectView()
    {
        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string>
                {
                    PagePaths.FileUploadCompanyDetailsSubLanding
                },
                SubmissionPeriod = "April 2025 to March 2026",
                IsFileUploadJourneyInvokedViaRegistration = true
            }
        };

        SubmissionService
            .Setup(s => s.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int?>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<RegistrationSubmission>());

        SessionManagerMock.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        // Act
        var result = SystemUnderTest.RedirectToFileUpload().Result as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(FileUploadCompanyDetailsController.Get));
        FrontEndSchemeRegistrationSession.RegistrationSession.IsFileUploadJourneyInvokedViaRegistration.Should().BeTrue();
    }

    [TestCase(SubmissionPeriodStatus.FileUploaded, "Basic User", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    [TestCase(SubmissionPeriodStatus.FileUploaded, "Delegated Person", "ReviewCompanyDetails", "submissionId")]
    [TestCase(SubmissionPeriodStatus.SubmittedToRegulator, "Approved Person", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    [TestCase(SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload, "Approved Person", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    [TestCase(SubmissionPeriodStatus.NotStarted, "Basic User", "FileUploadCompanyDetails", null)]
    [TestCase(SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload, "Approved Person", "FileReUploadCompanyDetailsConfirmation", "submissionId")]
    public void RedirectToRightAction_RedirectsToCorrectAction(
    SubmissionPeriodStatus status,
    string role,
    string expectedController,
    string expectedRouteValueKey)
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            UserData = new UserData
            {
                ServiceRole = role.ToString()
            },
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto
                {
                    Id = Guid.NewGuid()
                }
            }
        };

        var submission = new RegistrationSubmission
        {
            Id = Guid.NewGuid(),
            IsSubmitted = status == SubmissionPeriodStatus.SubmittedToRegulator ||
                          status == SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload,
            LastUploadedValidFiles = status == SubmissionPeriodStatus.FileUploaded ||
                                     status == SubmissionPeriodStatus.SubmittedAndHasRecentFileUpload
                ? new UploadedRegistrationFilesInformation { CompanyDetailsUploadDatetime = DateTime.UtcNow }
                : null,
            LastSubmittedFiles = status == SubmissionPeriodStatus.SubmittedToRegulator
                ? new SubmittedRegistrationFilesInformation { SubmittedDateTime = DateTime.UtcNow.AddDays(-1) }
                : null
        };

        SubmissionService
            .Setup(s => s.GetSubmissionsAsync<RegistrationSubmission>(
                It.IsAny<List<string>>(),
                It.IsAny<int?>(),
                It.IsAny<Guid?>()))
            .ReturnsAsync(new List<RegistrationSubmission> { submission });

        SubmissionService
            .Setup(s => s.GetDecisionAsync<RegistrationDecision>(
                It.IsAny<int>(),
                It.IsAny<Guid>(),
                It.IsAny<SubmissionType>()))
            .ReturnsAsync((RegistrationDecision)null);

        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
           .ReturnsAsync(new FrontendSchemeRegistrationSession
           {
               UserData = new UserData
               {
                   Organisations = new List<Organisation>
                   {
                        new()
                        {
                            OrganisationRole = OrganisationRoles.Producer
                        }
                   },
                   ServiceRole = role
               },
               RegistrationSession = new RegistrationSession
               {
                   SubmissionPeriod = SubmissionPeriod,
                   IsFileUploadJourneyInvokedViaRegistration = true,
                   Journey = new List<string>
                   {
                        PagePaths.FileUploadCompanyDetailsSubLanding,
                        PagePaths.FileUploadCompanyDetails,
                        PagePaths.FileUploadBrands,
                        PagePaths.FileUploadPartnerships
                   }
               }
           });

        // Act
        var result = SystemUnderTest.RedirectToFileUpload().Result as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be(expectedController);
        if (expectedRouteValueKey != null)
        {
            result.RouteValues.Should().ContainKey(expectedRouteValueKey);
            result.RouteValues[expectedRouteValueKey].Should().Be(submission.Id);
        }
    }

    [Test]
    public async Task WhenComplianceSchemeRegistrationDataHasBeenSubmitted_RegistrationFeeCalculations_ReturnsComplianceSchemeFeeCalculationBreakdownViewModel()
    {
        SetupBase(GetUserData("Compliance Scheme"));

        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationFeeCalculations },
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "TestRef",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                FileReachedSynapse = true,
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid(), SubmittedDateTime = DateTime.Now },
                ProducerDetails = ProducerDetailsDto,
                CsoMemberDetails = _complianceSchemeDetailsDto,
                IsSubmitted = true,
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.GetComplianceSchemeDetails(It.IsAny<string>())).ReturnsAsync(_complianceSchemeDetailsDto);

        PaymentCalculationService.Setup(x => x.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemeDetailsDto>(), It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(_complianceSchemeCalculationResponse);

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
            RegistrationFeePaid = false,
            SmallProducersCount = 0,
            SmallProducersFee = 0,
            SubsidiaryCompanyCount = 2,
            SubsidiaryCompanyFee = 11000,
            TotalFeeAmount = 12345,
            TotalPreviousPayments = 23456,
            LateProducerFee = _complianceSchemeCalculationResponse.ComplianceSchemeMembersWithFees[0].MemberLateRegistrationFee,
            LateProducersCount = _complianceSchemeCalculationResponse.ComplianceSchemeMembersWithFees.Count,
        });
    }

    [Test]
    public async Task WhenComplianceSchemeRegistrationDataHasBeenSubmitted_ForSmallProducer_RegistrationFeeCalculations_ReturnsComplianceSchemeFeeCalculationBreakdownViewModel()
    {
        SetupBase(GetUserData("Compliance Scheme"));

        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationFeeCalculations },
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "TestRef",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                FileReachedSynapse = true,
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid(), SubmittedDateTime = DateTime.Now },
                ProducerDetails = SmallProducerDetailsDto,
                CsoMemberDetails = _complianceSchemeDetailsSmallProducerDto,
                IsSubmitted = true,
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.GetComplianceSchemeDetails(It.IsAny<string>())).ReturnsAsync(_complianceSchemeDetailsSmallProducerDto);

        PaymentCalculationService.Setup(x => x.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemeDetailsDto>(), It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(_complianceSchemeCalculationSmallProducerResponse);

        // Act
        var result = await SystemUnderTest.RegistrationFeeCalculations() as ViewResult;

        // Assert
        result.Model.Should().BeOfType<ComplianceSchemeFeeCalculationBreakdownViewModel>();
        result.Model.As<ComplianceSchemeFeeCalculationBreakdownViewModel>().Should().BeEquivalentTo(new ComplianceSchemeFeeCalculationBreakdownViewModel
        {
            LargeProducersCount = 0,
            LargeProducersFee = 0,
            OnlineMarketplaceCount = 1,
            OnlineMarketplaceFee = 7000,
            RegistrationFee = 20000,
            RegistrationFeePaid = false,
            SmallProducersCount = 1,
            SmallProducersFee = 9000,
            SubsidiaryCompanyCount = 0,
            SubsidiaryCompanyFee = 0,
            TotalFeeAmount = 12345,
            TotalPreviousPayments = 23456,
            LateProducerFee = _complianceSchemeCalculationSmallProducerResponse.ComplianceSchemeMembersWithFees[0].MemberLateRegistrationFee,
            LateProducersCount = _complianceSchemeCalculationSmallProducerResponse.ComplianceSchemeMembersWithFees.Count,
        });
    }

    [Test]
    public void WhenComplianceSchemeNotFound_RegistrationFeeCalculations_RedirectsToTaskListView()
    {
        SetupBase(GetUserData("Compliance Scheme"));

        // Arrange
        FrontEndSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.RegistrationFeeCalculations },
            },
            RegistrationApplicationSession = new RegistrationApplicationSession
            {
                SubmissionId = Guid.NewGuid(),
                ApplicationReferenceNumber = "TestRef",
                ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                FileReachedSynapse = true,
                RegistrationFeePaymentMethod = null,
                LastSubmittedFile = new LastSubmittedFileDetails { FileId = Guid.NewGuid(), SubmittedDateTime = DateTime.Now },
                ProducerDetails = ProducerDetailsDto,
                CsoMemberDetails = null,
                IsSubmitted = true,
            }
        };
        SessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(FrontEndSchemeRegistrationSession);

        PaymentCalculationService.Setup(x => x.GetComplianceSchemeDetails(It.IsAny<string>())).ReturnsAsync(_complianceSchemeDetailsDto);

        PaymentCalculationService.Setup(x => x.GetComplianceSchemeRegistrationFees(It.IsAny<ComplianceSchemeDetailsDto>(), It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync((ComplianceSchemePaymentCalculationResponse)null);

        // Act
        var result = SystemUnderTest.RegistrationFeeCalculations().Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var viewResult = result as RedirectToActionResult;
        viewResult.ActionName.Should().Be(nameof(FrontendSchemeRegistrationController.RegistrationTaskList));
    }
    private void ValidateViewModel(object Model)
    {
        ValidationContext validationContext = new ValidationContext(Model, null, null);
        List<ValidationResult> validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(Model, validationContext, validationResults, true);
        foreach (ValidationResult validationResult in validationResults)
        {
            SystemUnderTest.ControllerContext.ModelState.AddModelError(String.Join(", ", validationResult.MemberNames), validationResult.ErrorMessage);
        }
    }
}
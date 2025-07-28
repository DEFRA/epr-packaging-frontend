using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.RequestModels;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Controllers.ResubmissionApplication;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels.RegistrationApplication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class ResubmissionApplicationControllerTests
{
    private readonly Mock<ClaimsPrincipal> _userMock = new();

    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _mockSessionManager;
    private Mock<IResubmissionApplicationService> _mockResubmissionApplicationServices;
    protected Mock<IUserAccountService> _userAccountService;
    protected Mock<IRegulatorService> _regulatorService;

    private ResubmissionApplicationController _controller;
    private readonly Mock<HttpContext> _httpContextMock = new();
    private Mock<IUserAccountService> _mockUserAccountService;

    private const string OrganisationName = "Acme Org Ltd";
    private const string SubmissionPeriod = "Jul to Dec 23";
    private readonly Guid _organisationId = Guid.NewGuid();
    private Mock<IUrlHelper> _urlHelperMock;

    private UserData _userData;

    [SetUp]
    public void SetUp()
    {
        _userData = GetUserData("Producer");

        var claims = new List<Claim>();
        if (_userData != null)
        {
            claims.Add(new(ClaimTypes.UserData, JsonConvert.SerializeObject(_userData)));
        }

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);

        _mockSessionManager = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _mockResubmissionApplicationServices = new Mock<IResubmissionApplicationService>();
        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);

        _userAccountService = new Mock<IUserAccountService>();
        _regulatorService = new Mock<IRegulatorService>();
        _controller = new ResubmissionApplicationController(_mockSessionManager.Object, _mockResubmissionApplicationServices.Object, _userAccountService.Object, _regulatorService.Object);
        _controller.ControllerContext.HttpContext = _httpContextMock.Object;
        _controller.Url = _urlHelperMock.Object;

        _mockUserAccountService = new Mock<IUserAccountService>();
    }

    [Test]
    public async Task SelectPaymentOptions_Get_ReturnsViewResult_WithValidModel()
    {
        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                RegulatorNation = "GB-ENG",
                FeeBreakdownDetails = new FeeBreakdownDetails
                {
                    TotalAmountOutstanding = 100
                }
            },
        };
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        // Act
        var result = await _controller.SelectPaymentOptions();

        // Assert            
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<SelectPaymentOptionsViewModel>();
    }

    [Test]
    public async Task SelectPaymentOptions_Get_ReturnsPayByBank_WithValidModel()
    {
        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                RegulatorNation = "GB-CLS",
                FeeBreakdownDetails = new FeeBreakdownDetails
                {
                    TotalAmountOutstanding = 100
                }
            }
        };
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        // Act
        var result = await _controller.SelectPaymentOptions();

        // Assert            
        result.Should().BeOfType<RedirectToActionResult>();
        var viewResult = result as RedirectToActionResult;

        viewResult.ActionName.Should().Be("PayByBankTransfer");
    }

    [Test]
    public async Task SelectPaymentOptions_Post_InvalidModel_ReturnsViewResult()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                FeeBreakdownDetails = new FeeBreakdownDetails
                {
                    TotalAmountOutstanding = 100
                }
            }
        };
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        _controller.ModelState.AddModelError("PaymentOption", "Required");

        var model = new SelectPaymentOptionsViewModel();

        // Act
        var result = await _controller.SelectPaymentOptions(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        (result as ViewResult).Model.Should().BeOfType<SelectPaymentOptionsViewModel>();
    }

    [Test]
    public void SelectPaymentOptions_OnSubmit_WhenNoPaymentOptionSelected_ReturnsInvalidModelState()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                Journey = [PagePaths.RegistrationFeeCalculations, PagePaths.SelectPaymentOptions]
            }
        };

        _mockSessionManager.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var model = new SelectPaymentOptionsViewModel() { PaymentOption = null };

        ValidateViewModel(model);

        // Act
        var result = _controller.SelectPaymentOptions(model).Result;

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
        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.ResubmissionTaskList },
            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), IsSubmitted = true, LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } } },
                SubmissionPeriod = "January to December 2024",
                RegulatorNation = "GB-ENG"
            }
        };

        _mockSessionManager.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var model = new SelectPaymentOptionsViewModel() { PaymentOption = paymentOption };

        // Act
        var result = _controller.SelectPaymentOptions(model).Result;

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
    public void SelectPaymentOptions_NonENG_Nation_PaymentOnline_OnSubmit_RedirectTO_BankTrasferView()
    {
        // Arrange
        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.ResubmissionTaskList },
            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), IsSubmitted = true, LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } } },
                SubmissionPeriod = "January to December 2024",
                RegulatorNation = "GB-CLS",
                FeeBreakdownDetails = new FeeBreakdownDetails
                {
                    TotalAmountOutstanding = 100
                }
            }
        };

        _mockSessionManager.Setup(x =>
            x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);

        var model = new SelectPaymentOptionsViewModel() { PaymentOption = 1 };

        // Act
        var result = _controller.SelectPaymentOptions(model).Result;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var viewResult = result as RedirectToActionResult;

        viewResult.ActionName.Should().Be("PayByBankTransfer");
    }


    [Test]
    public async Task PayByPhone_ReturnsCorrectViewModel()
    {
        // Arrange
        const string viewName = "PaymentOptionPayByPhone";

        // Act
        var result = await _controller.PayByPhone() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayByPhoneViewModel>();
    }

    [Test]
    public async Task PayOnline_ReturnsCorrectViewModel()
    {
        // Arrange
        const string viewName = "PaymentOptionPayOnline";

        // Act
        var result = await _controller.PayOnline() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayOnlineViewModel>();
    }

    [Test]
    public async Task PayByBankTransfer_EnglandCustomer_ReturnsCorrectViewModel()
    {
        var session = new FrontendSchemeRegistrationSession()
        {
            PomResubmissionSession = new PackagingReSubmissionSession()
            {
                RegulatorNation = "GB-ENG"
            }
        };
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        // Arrange
        const string viewName = "PaymentOptionPayByBankTransfer";

        // Act
        var result = await _controller.PayByBankTransfer() as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
        result.Model.Should().BeOfType<PaymentOptionPayByBankTransferViewModel>();
    }

    [TestCase("GB-ENG", "England")]
    [TestCase("GB-SCT", "Scotland")]
    [TestCase("GB-WLS", "Wales")]
    [TestCase("GB-NIR", "NorthernIreland")]
    public async Task PayByBankTransfer_backLink_ReturnsCorrectLink(string nationCode, string nationName)
    {
        var session = new FrontendSchemeRegistrationSession()
        {
            PomResubmissionSession = new PackagingReSubmissionSession()
            {
                RegulatorNation = nationCode
            }
        };
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        // Arrange
        const string viewName = "PaymentOptionPayByBankTransfer";

        // Act
        var result = await _controller.PayByBankTransfer() as ViewResult;

        // Assert
        if (nationCode == "GB-ENG")
        {
            result.ViewData["BackLinkToDisplay"].Should().Be(PagePaths.SelectPaymentOptions);
        }
        else
        {
            result.ViewData["BackLinkToDisplay"].Should().Be($"/report-data/{PagePaths.ResubmissionFeeCalculations}");
        }

        result.ViewName.Should().Be(viewName);
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");


    }

    [Test]
    public async Task AdditionalInformation_Get_ReturnsViewResult_WithValidModel()
    {
        var session = new FrontendSchemeRegistrationSession();
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        // Act
        var result = await _controller.AdditionalInformation() as ViewResult;
        var pageBackLink = _controller.ViewBag.BackLinkToDisplay as string;

        // Assert
        pageBackLink.Should().Be($"/report-data/{PagePaths.ResubmissionTaskList}");
        result.Model.Should().BeOfType<AdditionalInformationViewModel>();
        result.Should().BeOfType<ViewResult>();
    }

    [Test]
    public async Task AdditionalInformation_Post_ReturnsViewResult_WithValidModel()
    {
        // Arrange
        var expectedDate = DateTime.UtcNow;
        var expectedReference = "REF123";
        var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                Journey = new List<string> { PagePaths.ResubmissionTaskList },
            },
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PomSubmissions = new List<PomSubmission> { new PomSubmission { Id = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b"), IsSubmitted = true, LastSubmittedFile = new SubmittedFileInformation { FileId = new Guid("147f59f0-3d4e-4557-91d2-db033dffa60b") } } },
                SubmissionPeriod = "January to December 2024",
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession
                {
                    ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                    ApplicationReferenceNumber = expectedReference
                },
                PomSubmission = new PomSubmission()
                {
                    LastSubmittedFile = new SubmittedFileInformation
                    {
                        SubmittedDateTime = expectedDate
                    }
                }

            }

        };
        
        //_mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(session);
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(frontendSchemeRegistrationSession));

        _userAccountService.Setup(x => x.GetPersonByUserId(It.IsAny<Guid>())).ReturnsAsync(new Application.DTOs.UserAccount.PersonDto { FirstName = "Test", LastName = "Name" });

        // Act
        var result = await _controller.AdditionalInformation(It.IsAny<AdditionalInformationViewModel>()) as RedirectToActionResult;
        var pageBackLink = _controller.ViewBag.BackLinkToDisplay as string;

        _regulatorService.Verify(mock => mock.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>()), Times.Once());

        // Assert
        pageBackLink.Should().Be($"/report-data/{PagePaths.ResubmissionTaskList}");
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Test]
    public async Task SubmitToEnvironmentRegulator_ShouldReturnView_WithExpectedModel()
    {
        // Arrange
        var expectedDate = DateTime.UtcNow;
        var expectedReference = "REF123";

        var session = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession
                {
                    ApplicationStatus = ApplicationStatusType.SubmittedToRegulator,
                    ApplicationReferenceNumber = expectedReference
                },
                PomSubmission = new PomSubmission()
                {
                    LastSubmittedFile = new SubmittedFileInformation
                    {
                        SubmittedDateTime = expectedDate
                    }
                }
            }
        };

        _mockSessionManager.Setup(s => s.GetSessionAsync(It.IsAny<ISession>()))
                           .ReturnsAsync(session);

        // Act
        var result = await _controller.SubmitToEnvironmentRegulator() as ViewResult;

        var model = result.Model as ApplicationSubmissionConfirmationViewModel;

        // Assert
        result.Should().NotBeNull();
        result.ViewName.Should().Be("ResubmissionConfirmation");      

        model.Should().NotBeNull();
        model.RegistrationApplicationSubmittedDate.Should().Be(expectedDate);
        model.ApplicationReferenceNumber.Should().Be(expectedReference);
        model.ApplicationStatus.Should().Be(ApplicationStatusType.SubmittedToRegulator);
    }

    [Test]
    public async Task RedirectToComplianceSchemeDashboard_Redirects_ComplianceSchemeLanding()
    {
        var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                    LastSubmittedFile = new LastSubmittedFileDetails() { FileId = Guid.NewGuid() }
                }
            }
        };

        var session = new FrontendSchemeRegistrationSession();
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(frontendSchemeRegistrationSession));

        // Act
        var result = await _controller.RedirectToComplianceSchemeDashboard() as RedirectToActionResult;

        // Assert
        result.ControllerName.Should().Be("ComplianceSchemeLanding");
        result.ActionName.Should().Be(nameof(ComplianceSchemeLandingController.Get));
    }

    [Test]
    public async Task RedirectToComplianceSchemeDashboard_Calls_FeePaymentEvent_Once_IfPaymentMethodIsNullOrEmpty()
    {
        var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                    LastSubmittedFile = new LastSubmittedFileDetails() { FileId = Guid.NewGuid() },
                    ResubmissionFeePaymentMethod = string.Empty
                },
            }
        };

        var session = new FrontendSchemeRegistrationSession();
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(frontendSchemeRegistrationSession));

        // Act
        var result = await _controller.RedirectToComplianceSchemeDashboard() as RedirectToActionResult;

        // Assert
        _mockResubmissionApplicationServices.Verify(x => x.CreatePackagingDataResubmissionFeePaymentEvent(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task RedirectToComplianceSchemeDashboard_NeverCalls_FeePaymentEvent_IfPaymentMethodHasValue()
    {
        var frontendSchemeRegistrationSession = new FrontendSchemeRegistrationSession
        {
            PomResubmissionSession = new PackagingReSubmissionSession
            {
                PackagingResubmissionApplicationSession = new PackagingResubmissionApplicationSession()
                {
                    SubmissionId = Guid.NewGuid(),
                    LastSubmittedFile = new LastSubmittedFileDetails() { FileId = Guid.NewGuid() },
                    ResubmissionFeePaymentMethod = "PayByBankTransfer"
                },
            }
        };

        var session = new FrontendSchemeRegistrationSession();
        _mockSessionManager.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(frontendSchemeRegistrationSession));

        // Act
        var result = await _controller.RedirectToComplianceSchemeDashboard() as RedirectToActionResult;

        // Assert
        _mockResubmissionApplicationServices.Verify(x => x.CreatePackagingDataResubmissionFeePaymentEvent(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string>()), Times.Never);
    }

    private void ValidateViewModel(object model)
    {
        var validationContext = new ValidationContext(model, null, null);
        List<ValidationResult> validationResults = [];
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        foreach (var validationResult in validationResults)
        {
            _controller.ControllerContext.ModelState.AddModelError(String.Join(", ", validationResult.MemberNames), validationResult.ErrorMessage);
        }
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
}

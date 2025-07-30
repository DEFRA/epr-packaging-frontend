using FrontendSchemeRegistration.UI.Services;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Application.DTOs.ComplianceScheme;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using UI.Controllers.FrontendSchemeRegistration;
using UI.Sessions;

public abstract class FrontendSchemeRegistrationTestBase
{
    protected const string ModelErrorKey = "Error";
    private const string BackLinkViewDataKey = "BackLinkToDisplay";
    private static readonly Guid _complianceSchemeId = new Guid("00000000-0000-0000-0000-000000000002");
    private static readonly Guid _selectedSchemeId = new Guid("00000000-0000-0000-0000-000000000003");
    private static readonly Guid _complianceSchemeOperatorId = new Guid("00000000-0000-0000-0000-000000000004");
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();
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
            DataPeriod = "Data period 3",
            /* This will be excluded because it is after the latest allowed period ending June 2024 */
            Deadline = DateTime.Parse("2024-12-31"),
            ActiveFrom = DateTime.Today.AddDays(10),
            Year = "2025",
            StartMonth = "July",
            EndMonth = "December"
        }
    };

    protected static ProducerComplianceSchemeDto CurrentComplianceScheme => new()
    {
        ComplianceSchemeId = _complianceSchemeId,
        ComplianceSchemeName = "currentTestScheme",
        ComplianceSchemeOperatorName = "testOperator",
        SelectedSchemeId = _selectedSchemeId,
        ComplianceSchemeOperatorId = _complianceSchemeOperatorId,
    };

    protected static ComplianceSchemeDto SelectedComplianceScheme => new()
    {
        Id = _complianceSchemeId,
        Name = "newTestScheme",
    };

    protected static SelectedSchemeDto CommittedSelectedScheme => new()
    {
        Id = _complianceSchemeId,
    };

    protected Mock<ISessionManager<FrontendSchemeRegistrationSession>> SessionManagerMock { get; private set; }

    protected FrontendSchemeRegistrationController SystemUnderTest { get; set; }

    protected Mock<INotificationService> NotificationService { get; private set; }

    protected Mock<ISubmissionService> SubmissionService { get; private set; }

    protected Mock<IRegistrationApplicationService> RegistrationApplicationService { get; private set; }

    protected Mock<IComplianceSchemeService> ComplianceSchemeService { get; private set; }

    protected Mock<IUserAccountService> UserAccountService { get; private set; }

    protected Mock<IAuthorizationService> AuthorizationService { get; private set; }

    protected Mock<IPaymentCalculationService> PaymentCalculationService { get; private set; }

	protected Mock<ILogger<FrontendSchemeRegistrationController>> LoggerMock { get; set; }

    protected FrontendSchemeRegistrationSession FrontEndSchemeRegistrationSession { get; set; }
    
    protected Mock<IResubmissionApplicationService> ResubmissionApplicationService { get; set; }
    protected Mock<IOptions<GlobalVariables>> globalVariables { get; set; }

    protected static void AssertBackLink(ViewResult viewResult, string expectedBackLink)
    {
        var hasBackLinkKey = viewResult.ViewData.TryGetValue(BackLinkViewDataKey, out var gotBackLinkObject);
        hasBackLinkKey.Should().BeTrue();
        (gotBackLinkObject as string)?.Should().Be(expectedBackLink);
    }

    protected void SetupBase(UserData userData)
    {
        var claims = new List<Claim>();
        if (userData != null)
        {
            claims.Add(new(ClaimTypes.UserData, JsonConvert.SerializeObject(userData)));
        }

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);

        globalVariables = new Mock<IOptions<GlobalVariables>>();
        globalVariables.Setup(o => o.Value).Returns(new GlobalVariables { BasePath = "path", SubmissionPeriods = _submissionPeriods });

        var tempDataDictionaryMock = new Mock<ITempDataDictionary>();

        SessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession()));

        LoggerMock = new Mock<ILogger<FrontendSchemeRegistrationController>>();
        ComplianceSchemeService = new Mock<IComplianceSchemeService>();
        UserAccountService = new Mock<IUserAccountService>();
        AuthorizationService = new Mock<IAuthorizationService>();
        NotificationService = new Mock<INotificationService>();
        SubmissionService = new Mock<ISubmissionService>();
        RegistrationApplicationService = new Mock<IRegistrationApplicationService>();
        PaymentCalculationService = new Mock<IPaymentCalculationService>();
        ResubmissionApplicationService = new Mock<IResubmissionApplicationService>();

        SystemUnderTest = new FrontendSchemeRegistrationController(
            SessionManagerMock.Object,
            LoggerMock.Object,
            ComplianceSchemeService.Object,
            RegistrationApplicationService.Object,
            ResubmissionApplicationService.Object,
            AuthorizationService.Object,
        NotificationService.Object);
        SystemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;
        SystemUnderTest.TempData = tempDataDictionaryMock.Object;
    }
}

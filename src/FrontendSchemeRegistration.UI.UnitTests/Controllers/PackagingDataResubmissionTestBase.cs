namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Application.Services.Interfaces;
using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FrontendSchemeRegistration.Application.DTOs.Submission;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using UI.Controllers.FrontendSchemeRegistration;
using UI.Sessions;

public abstract class PackagingDataResubmissionTestBase
{
    private readonly Mock<HttpContext> _httpContextMock = new();
    private readonly Mock<ClaimsPrincipal> _userMock = new();

    protected Mock<ISessionManager<FrontendSchemeRegistrationSession>> SessionManagerMock { get; private set; }

    protected PackagingDataResubmissionController SystemUnderTest { get; set; }

    protected Mock<IUserAccountService> UserAccountService { get; private set; }

    protected Mock<ILogger<PackagingDataResubmissionController>> LoggerMock { get; set; }

    protected FrontendSchemeRegistrationSession FrontendSchemeRegistrationSession { get; set; }

    protected Mock<IPaymentCalculationService> PaymentCalculationService { get; set; }

    protected Mock<IResubmissionApplicationService> ResubmissionApplicationService { get; set; }
    
    protected Mock<IComplianceSchemeService> ComplianceService { get; set; }


	protected void SetupBase(UserData userData)
    {
        var claims = new List<Claim>();
        if (userData != null)
        {
            claims.Add(new(ClaimTypes.UserData, JsonConvert.SerializeObject(userData)));
        }

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);

        SessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        SessionManagerMock.Setup(sm => sm.GetSessionAsync(It.IsAny<ISession>()))
            .Returns(Task.FromResult(new FrontendSchemeRegistrationSession()));

        LoggerMock = new Mock<ILogger<PackagingDataResubmissionController>>();
        UserAccountService = new Mock<IUserAccountService>();
        PaymentCalculationService = new Mock<IPaymentCalculationService>();
        ResubmissionApplicationService = new Mock<IResubmissionApplicationService>();
        ComplianceService = new Mock<IComplianceSchemeService>();

		SystemUnderTest = new PackagingDataResubmissionController(
            SessionManagerMock.Object,
            LoggerMock.Object,
            UserAccountService.Object,
            Options.Create(new GlobalVariables
            {
                SubmissionPeriods = new List<SubmissionPeriod>()
                {
                    new SubmissionPeriod { DataPeriod = $"January to December 2024", StartMonth = "January", EndMonth = "December", Year = "2024" }
                }
            }),
            ResubmissionApplicationService.Object,
			ComplianceService.Object);
        SystemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;
        SystemUnderTest.ControllerContext.HttpContext.Session = new Mock<ISession>().Object;
    }
}
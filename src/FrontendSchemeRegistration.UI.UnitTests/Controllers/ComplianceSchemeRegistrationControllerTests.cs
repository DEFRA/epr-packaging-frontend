using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.Security.Claims;
using Application.Constants;
using Application.DTOs.ComplianceScheme;
using Application.DTOs.Submission;
using Application.Enums;
using Application.Options;
using Application.Services.Interfaces;
using AutoFixture;
using EPR.Common.Authorization.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Moq;
using Newtonsoft.Json;
using UI.Services;

[TestFixture]
public class ComplianceSchemeRegistrationControllerTests
{
    private ComplianceSchemeRegistrationController _sut;
    private Mock<IComplianceSchemeService> _complianceSchemeService;
    private Mock<IRegistrationApplicationService> _registrationApplicationService;
    private Mock<HttpContext> _httpContextMock;
    private Mock<ClaimsPrincipal> _userMock;
    private readonly IFixture _fixture = new Fixture();

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
            Deadline = DateTime.Parse("2025-10-01"),
            ActiveFrom = DateTime.Today.AddDays(10),
            Year = "2025",
            StartMonth = "January",
            EndMonth = "June"
        }
    };

    [SetUp]
    public void Setup()
    {
        _complianceSchemeService = new();
        _registrationApplicationService = new();
        _httpContextMock = new();
        _userMock = new();
        
        var orgs = _fixture.Build<Organisation>()
            .With(o => o.OrganisationRole, "ComplianceScheme")
            .CreateMany(1);

        var userData = _fixture.Build<UserData>()
            .With(ud => ud.Organisations, orgs.ToList())
            .With(ud => ud.ServiceRole, "Approved Person")
            .Create();
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, JsonConvert.SerializeObject(userData))
        };

        _userMock.Setup(x => x.Claims).Returns(claims);
        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _httpContextMock.Setup(x => x.Session).Returns(new Mock<ISession>().Object);

        _sut = new(_complianceSchemeService.Object, _registrationApplicationService.Object)
        {
            ControllerContext = { HttpContext = _httpContextMock.Object }
        };
    }

    [Test]
    public async Task WHEN_ComplianceSchemeRegistrationCalled_THEN_CorrectViewModelReturned()
    {
        // arrange
        var nation = Nation.England;
        const string expectedViewName = "ComplianceSchemeRegistration";
        var englandCso = _fixture.Build<ComplianceSchemeDto>()
            .With(cs => cs.NationId, (int)Nation.England)
            .Create();
        var scotlandCso = _fixture.Build<ComplianceSchemeDto>()
            .With(cs => cs.NationId, (int)Nation.Scotland)
            .Create();

        var registrationApplicationPerYear = _fixture.CreateMany<RegistrationApplicationPerYearViewModel>().ToArray();

        _complianceSchemeService
            .Setup(service => service.GetOperatorComplianceSchemes(It.IsAny<Guid>()))
            .ReturnsAsync([scotlandCso, englandCso]);
        _registrationApplicationService.Setup(x => x.BuildRegistrationApplicationPerYearViewModels(It.IsAny<ISession>(), It.IsAny<Organisation>()))
            .ReturnsAsync(registrationApplicationPerYear.ToList());
        
        var expectedViewModel = new ComplianceSchemeRegistrationViewModel(englandCso.Name, nation.ToString(), registrationApplicationPerYear, 2026);
        
        // Act
        var result = await _sut.ComplianceSchemeRegistration(nation) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result.Model.Should()
            .BeOfType<ComplianceSchemeRegistrationViewModel>()
            .And
            .BeEquivalentTo(expectedViewModel);
    }
}
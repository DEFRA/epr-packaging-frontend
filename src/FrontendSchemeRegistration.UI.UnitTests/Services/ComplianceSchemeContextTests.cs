using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.Sessions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Services;

[TestFixture]
public class ComplianceSchemeContextTests
{
    private readonly Mock<IComplianceSchemeMemberService> _complianceSchemeMemberService = new();
    private readonly Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManager = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();

    [Test]
    public async Task GetComplianceSchemeIdAsync_ReturnsIdFromHttpContextItems_WhenPresent()
    {
        var complianceSchemeId = Guid.NewGuid();
        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeId())
            .Returns(complianceSchemeId);

        var result = await CreateSystemUnderTest().GetComplianceSchemeIdAsync();

        result.Should().Be(complianceSchemeId);
        _sessionManager.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Never);
    }

    [Test]
    public async Task GetComplianceSchemeIdAsync_ReturnsIdFromSession_WhenHttpContextItemIsMissing()
    {
        var complianceSchemeId = Guid.NewGuid();
        _complianceSchemeMemberService
            .Setup(x => x.GetComplianceSchemeId())
            .Returns((Guid?)null);
        var httpContext = new Mock<HttpContext>();
        var httpSession = new Mock<ISession>();
        httpContext.SetupGet(x => x.Session).Returns(httpSession.Object);
        _httpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext.Object);
        _sessionManager
            .Setup(x => x.GetSessionAsync(httpSession.Object))
            .ReturnsAsync(new FrontendSchemeRegistrationSession
            {
                RegistrationSession = new RegistrationSession
                {
                    SelectedComplianceScheme = new ComplianceSchemeDto { Id = complianceSchemeId }
                }
            });

        var result = await CreateSystemUnderTest().GetComplianceSchemeIdAsync();

        result.Should().Be(complianceSchemeId);
    }

    private ComplianceSchemeContext CreateSystemUnderTest() => new(
        _complianceSchemeMemberService.Object,
        _sessionManager.Object,
        _httpContextAccessor.Object);
}

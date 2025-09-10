namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using UI.Extensions;

[TestFixture]
public class ClaimsExtensionsTests
{
    private IFixture _fixture;
    private Mock<HttpContext> _httpContextMock;
    private Mock<ClaimsIdentity> _claimsIdentityMock;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _httpContextMock = new Mock<HttpContext>();
        _claimsIdentityMock = new Mock<ClaimsIdentity>();
    }

    [Test]
    public void GetUserData_ShouldThrow_WhenNoUserDataClaimPresent()
    {
        // Arrange
        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns(Enumerable.Empty<Claim>());

        // Act
        Action act = () => claimsPrincipalMock.Object.GetUserData();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void GetUserData_ShouldReturnUserData_WhenUserDataClaimPresent()
    {
        // Arrange
        var userData = _fixture.Create<UserData>();
        var serializedUserData = JsonSerializer.Serialize(userData);

        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns(new[] { new Claim(ClaimTypes.UserData, serializedUserData) });

        // Act
        var result = claimsPrincipalMock.Object.GetUserData();

        // Assert
        result.Should().BeEquivalentTo(userData);
    }

    [Test]
    public void GetUserData_ShouldThrow_WhenUserDataClaimIsMalformed()
    {
        // Arrange
        var malformedUserData = "{this is not valid JSON}";

        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns(new[] { new Claim(ClaimTypes.UserData, malformedUserData) });

        // Act
        Action act = () => claimsPrincipalMock.Object.GetUserData();

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Test]
    public void TryGetUserData_ShouldReturnNull_WhenNoUserDataClaimPresent()
    {
        // Arrange
        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns(Enumerable.Empty<Claim>());

        // Act
        var result = claimsPrincipalMock.Object.TryGetUserData();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void TryGetUserData_ShouldReturnUserData_WhenUserDataClaimPresent()
    {
        // Arrange
        var userData = _fixture.Create<UserData>();
        var serializedUserData = JsonSerializer.Serialize(userData);

        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns(new[] { new Claim(ClaimTypes.UserData, serializedUserData) });

        // Act
        var result = claimsPrincipalMock.Object.GetUserData();

        // Assert
        result.Should().BeEquivalentTo(userData);
    }

    [Test]
    public void TryGetUserData_ShouldReturnNull_WhenUserDataClaimIsMalformed()
    {
        // Arrange
        var malformedUserData = "{this is not valid JSON}";

        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns(new[] { new Claim(ClaimTypes.UserData, malformedUserData) });

        // Act
        var result = claimsPrincipalMock.Object.TryGetUserData();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateUserDataClaimsAndSignInAsync_ShouldUpdateClaimsAndSignInUser()
    {
        // Arrange
        var userData = _fixture.Create<UserData>();
        var userDataJson = JsonSerializer.Serialize(userData);

        _claimsIdentityMock.Setup(x => x.FindFirst(ClaimTypes.UserData)).Returns((Claim)null);

        var userMock = new Mock<ClaimsPrincipal>();
        userMock.Setup(x => x.Identity).Returns(_claimsIdentityMock.Object);

        var authenticationServiceMock = new Mock<IAuthenticationService>();
        authenticationServiceMock
            .Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(_ => _.GetService(typeof(IAuthenticationService)))
            .Returns(authenticationServiceMock.Object);
        serviceProviderMock.Setup(_ => _.GetService(typeof(IUrlHelperFactory)))
            .Returns(Mock.Of<IUrlHelperFactory>());

        _httpContextMock
            .Setup(x => x.User)
            .Returns(userMock.Object);

        _httpContextMock
            .SetupGet(x => x.RequestServices)
            .Returns(serviceProviderMock.Object);

        _httpContextMock
            .SetupGet(x => x.Features)
            .Returns(Mock.Of<IFeatureCollection>());

        // Act
        await ClaimsExtensions.UpdateUserDataClaimsAndSignInAsync(_httpContextMock.Object, userData);

        // Assert
        _claimsIdentityMock.Verify(x => x.RemoveClaim(It.IsAny<Claim>()), Times.Never);

        // Verify that SignInAsync was called
        authenticationServiceMock.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Test]
    public void TryGetOrganisatonIds_ShouldReturnNull_WhenNoOrganisationIdsDataClaimPresent()
    {
        // Arrange
        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns(Enumerable.Empty<Claim>());

        // Act
        var result = claimsPrincipalMock.Object.TryGetOrganisatonIds();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void TryGetOrganisatonIds_ShouldReturnExpectedOrganisationIds_WhenUserDataClaimPresent()
    {
        // Arrange
        const string organisationIds = "012345,67890";

        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns(new[] { new Claim(CustomClaimTypes.OrganisationIds, organisationIds) });

        // Act
        var result = claimsPrincipalMock.Object.TryGetOrganisatonIds();

        // Assert
        result.Should().Be(organisationIds);
    }

    [Test]
    public void TryGetOrganisatonIds_ShouldReturnNull_WhenClaimsIsNull()
    {
        // Arrange
        var claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        claimsPrincipalMock.Setup(x => x.Claims).Returns((IEnumerable<Claim>)null);
        
        // Act
        var result = claimsPrincipalMock.Object.TryGetOrganisatonIds();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void TryGetOrganisatonIds_ShouldReturnNull_WhenClaimsPrincipalIsNull()
    {
        // Arrange
        var claimsPrincipal =  default(ClaimsPrincipal);

        // Act
        var result = claimsPrincipal.TryGetOrganisatonIds();

        // Assert
        result.Should().BeNull();
    }
}
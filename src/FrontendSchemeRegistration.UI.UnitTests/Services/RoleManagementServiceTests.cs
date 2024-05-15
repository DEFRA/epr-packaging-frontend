using System.Net;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FrontendSchemeRegistration.UI.UnitTests.Services
{
    [TestFixture]
    public class RoleManagementServiceTests
    {
        private Mock<IAccountServiceApiClient> _mockAccountServiceApiClient;
        private Mock<ILogger<RoleManagementService>> _logger;
        private IRoleManagementService _systemUnderTest;

        [SetUp]
        public void SetUp()
        {
            _mockAccountServiceApiClient = new Mock<IAccountServiceApiClient>();
            _logger = new Mock<ILogger<RoleManagementService>>();
            _systemUnderTest = new RoleManagementService(_mockAccountServiceApiClient.Object, _logger.Object);
        }

        [Test]
        public async Task AcceptNominationToApprovedPerson_Returns_Success_Response()
        {
            // Arrange
            var enrolmentId = Guid.NewGuid();
            var organisationId = Guid.NewGuid();
            var serviceKey = "Packaging";
            var acceptApprovedPersonRequest = new AcceptApprovedPersonRequest
            {
                JobTitle = "TestTitle",
                DeclarationFullName = "Declaration",
                PersonFirstName = "TestFst",
                PersonLastName = "TestLst",
                ContactEmail = "test@test.com",
                DeclarationTimeStamp = DateTime.UtcNow,
                OrganisationName = "Org",
                OrganisationNumber = "1",
                Telephone = "07898989898"
            };

            var expectedEndpoint = $"enrolments/{enrolmentId}/approved-person-acceptance?serviceKey={serviceKey}";
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

            _mockAccountServiceApiClient
                .Setup(x => x.PutAsJsonAsync<AcceptApprovedPersonRequest>(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AcceptApprovedPersonRequest>()))
                .Callback<Guid, string, AcceptApprovedPersonRequest>((orgId, endpoint, request) =>
                {
                    // Assert
                    orgId.Should().Be(organisationId);
                    endpoint.Should().Be(expectedEndpoint);
                    request.Should().BeEquivalentTo(acceptApprovedPersonRequest);
                })
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _systemUnderTest.AcceptNominationToApprovedPerson(enrolmentId, organisationId, serviceKey, acceptApprovedPersonRequest);

            // Assert
            result.Should().BeSameAs(expectedResponse);
        }
    }
}

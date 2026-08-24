using System.Net;
using System.Text;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Services
{
    [TestFixture]
    public class RoleManagementServiceTests
    {
        private Mock<IAccountServiceApiClient> _mockAccountServiceApiClient;
        private Mock<ILogger<RoleManagementService>> _logger;
        private RoleManagementService _systemUnderTest;

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

        [Test]
        public async Task AcceptNominationToApprovedPerson_Throws_When_Response_Not_Successful()
        {
            // Arrange
            var enrolmentId = Guid.NewGuid();
            var organisationId = Guid.NewGuid();
            var acceptApprovedPersonRequest = new AcceptApprovedPersonRequest();
            var failureResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

            _mockAccountServiceApiClient
                .Setup(x => x.PutAsJsonAsync<AcceptApprovedPersonRequest>(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AcceptApprovedPersonRequest>()))
                .ReturnsAsync(failureResponse);

            // Act
            Func<Task> act = () => _systemUnderTest.AcceptNominationToApprovedPerson(enrolmentId, organisationId, "Packaging", acceptApprovedPersonRequest);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Test]
        public async Task AcceptNominationToDelegatedPerson_Returns_Success_Response()
        {
            // Arrange
            var enrolmentId = Guid.NewGuid();
            var organisationId = Guid.NewGuid();
            var serviceKey = "Packaging";
            var acceptNominationRequest = new AcceptNominationRequest
            {
                Telephone = "07898989898",
                NomineeDeclaration = "Declaration"
            };

            var expectedEndpoint = $"enrolments/{enrolmentId}/delegated-person-acceptance?serviceKey={serviceKey}";
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

            _mockAccountServiceApiClient
                .Setup(x => x.PutAsJsonAsync<AcceptNominationRequest>(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AcceptNominationRequest>()))
                .Callback<Guid, string, AcceptNominationRequest>((orgId, endpoint, request) =>
                {
                    // Assert
                    orgId.Should().Be(organisationId);
                    endpoint.Should().Be(expectedEndpoint);
                    request.Should().BeEquivalentTo(acceptNominationRequest);
                })
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _systemUnderTest.AcceptNominationToDelegatedPerson(enrolmentId, organisationId, serviceKey, acceptNominationRequest);

            // Assert
            result.Should().BeSameAs(expectedResponse);
        }

        [Test]
        public async Task AcceptNominationToDelegatedPerson_Throws_When_Response_Not_Successful()
        {
            // Arrange
            var enrolmentId = Guid.NewGuid();
            var organisationId = Guid.NewGuid();
            var acceptNominationRequest = new AcceptNominationRequest();
            var failureResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            _mockAccountServiceApiClient
                .Setup(x => x.PutAsJsonAsync<AcceptNominationRequest>(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<AcceptNominationRequest>()))
                .ReturnsAsync(failureResponse);

            // Act
            Func<Task> act = () => _systemUnderTest.AcceptNominationToDelegatedPerson(enrolmentId, organisationId, "Packaging", acceptNominationRequest);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Test]
        public async Task GetDelegatedPersonNominator_Returns_Deserialized_Result()
        {
            // Arrange
            var enrolmentId = Guid.NewGuid();
            var organisationId = Guid.NewGuid();
            var expectedDto = new DelegatedPersonNominatorDto
            {
                FirstName = "Jane",
                LastName = "Doe",
                OrganisationName = "Org"
            };
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(expectedDto), Encoding.UTF8, "application/json")
            };

            _mockAccountServiceApiClient
                .Setup(x => x.SendGetRequest($"enrolments/{enrolmentId}/delegated-person-nominator?serviceKey=Packaging"))
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _systemUnderTest.GetDelegatedPersonNominator(enrolmentId, organisationId);

            // Assert
            result.Should().BeEquivalentTo(expectedDto);
            _mockAccountServiceApiClient.Verify(x => x.AddHttpClientHeader("X-EPR-Organisation", organisationId.ToString()), Times.Once);
        }

        [Test]
        public async Task GetDelegatedPersonNominator_Logs_And_Rethrows_When_Response_Not_Successful()
        {
            // Arrange
            var enrolmentId = Guid.NewGuid();
            var organisationId = Guid.NewGuid();
            var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

            _mockAccountServiceApiClient
                .Setup(x => x.SendGetRequest(It.IsAny<string>()))
                .ReturnsAsync(httpResponse);

            // Act
            Func<Task> act = () => _systemUnderTest.GetDelegatedPersonNominator(enrolmentId, organisationId);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task GetDelegatedPersonNominator_Passes_Null_Organisation_Header_When_OrganisationId_Is_Null()
        {
            // Arrange
            var enrolmentId = Guid.NewGuid();
            var expectedDto = new DelegatedPersonNominatorDto();
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(expectedDto), Encoding.UTF8, "application/json")
            };

            _mockAccountServiceApiClient
                .Setup(x => x.SendGetRequest(It.IsAny<string>()))
                .ReturnsAsync(httpResponse);

            // Act
            await _systemUnderTest.GetDelegatedPersonNominator(enrolmentId, null);

            // Assert
            _mockAccountServiceApiClient.Verify(x => x.AddHttpClientHeader("X-EPR-Organisation", string.Empty), Times.Once);
        }
    }
}

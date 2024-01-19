using System.Net;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.RequestModels;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Moq;
using Moq.Protected;

namespace FrontendSchemeRegistration.Application.UnitTests.Services
{
    [TestFixture]
    public class RegulatorServiceTests : ServiceTestBase<IRegulatorService>
    {
        private const string ServiceError = "Service error";

        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
        private readonly Mock<ILogger<RegulatorService>> _loggerMock = new();
        private readonly Mock<ITokenAcquisition> _tokenAcquisitionMock = new();
        private IRegulatorService _regulatorService;

        [Test]
        public async Task SendRegulatorResubmissionEmail_ReturnsOk()
        {
            var request = new ResubmissionEmailRequestModel
            {
                OrganisationNumber = "123456",
                ProducerOrganisationName = "Organisation Name",
                SubmissionPeriod = "Jan to Jun 2023",
                NationId = 2,
                IsComplianceScheme = true,
            };

            var stringContent = "notificationId";
            _regulatorService = MockService(HttpStatusCode.OK, stringContent.ToJsonContent());

            var result = await _regulatorService.SendRegulatorResubmissionEmail(request);

            result.Should().Be(stringContent);
        }

        [Test]
        public async Task SendRegulatorResubmissionEmail_ReturnsException()
        {
            _regulatorService = MockService(HttpStatusCode.InternalServerError, null, true);

            Func<Task<string?>> func = async () => await _regulatorService.SendRegulatorResubmissionEmail(It.IsAny<ResubmissionEmailRequestModel>());

            // Assert
            var ex = func.Should().ThrowAsync<Exception>();
            ex.Result.And.Message.Should().Contain(ServiceError);
        }

        protected override IRegulatorService MockService(HttpStatusCode expectedStatusCode, HttpContent expectedContent, bool raiseServiceException = false)
        {
            if (raiseServiceException)
            {
                _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception(ServiceError));
            }
            else
            {
                _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = expectedStatusCode,
                    Content = expectedContent,
                });
            }

            var client = new HttpClient(_httpMessageHandlerMock.Object);
            client.BaseAddress = new Uri("https://mock/api/test/");
            client.Timeout = TimeSpan.FromSeconds(30);

            var facadeOptions = Microsoft.Extensions.Options.Options.Create(new AccountsFacadeApiOptions { DownstreamScope = "https://mock/test" });
            var accountServiceApiClient = new AccountServiceApiClient(client, _tokenAcquisitionMock.Object, facadeOptions);

            return new RegulatorService(_loggerMock.Object, accountServiceApiClient);
        }
    }
}

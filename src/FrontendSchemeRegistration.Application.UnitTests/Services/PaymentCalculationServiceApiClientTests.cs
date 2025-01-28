using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services;
using Microsoft.Identity.Web;
using Moq;

namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using FluentAssertions;
using System.Net;
using Moq.Protected;
using Options = Microsoft.Extensions.Options.Options;

public class PaymentCalculationServiceApiClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly Mock<ITokenAcquisition> _tokenAcquisition = new();

    [Test]
    public async Task SendPostRequest_ReturnsSuccessfully()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        var client = new HttpClient(_httpMessageHandlerMock.Object);
        client.BaseAddress = new Uri("https://mock/api/test/");
        client.Timeout = TimeSpan.FromSeconds(30);
        var facadeOptions = Options.Create(new PaymentFacadeApiOptions { DownstreamScope = "https://mock/test" });

        var sut = new PaymentCalculationServiceApiClient(client, _tokenAcquisition.Object, facadeOptions);

        // Act
        var result = await sut.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>());

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccessStatusCode.Should().BeTrue();
    }
}
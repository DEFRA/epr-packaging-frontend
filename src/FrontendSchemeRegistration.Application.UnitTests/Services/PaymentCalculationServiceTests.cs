namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using System.Net;
using Application.Services;
using Application.Services.Interfaces;
using DTOs;
using DTOs.PaymentCalculations;
using DTOs.Submission;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UI.Extensions;
using Options;
using Options = Microsoft.Extensions.Options.Options;

[TestFixture]
public class PaymentCalculationServiceTests
{
    private Mock<IAccountServiceApiClient> _accountServiceApiClientMock;
    private Mock<IPaymentCalculationServiceApiClient> _paymentServiceApiClientMock;
    private PaymentCalculationService _systemUnderTest;
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;

    private static readonly ProducerDetailsDto ProducerDetailsDto = new ProducerDetailsDto
    {
        ProducerSize = "Large",
        IsOnlineMarketplace = true,
        NumberOfSubsidiaries = 54,
        NumberOfSubsidiariesBeingOnlineMarketPlace = 29,
        OrganisationId = 0,
    };
    private static readonly PaymentCalculationResponse CalculationResponse = new PaymentCalculationResponse
    {
        ProducerRegistrationFee = 262000,
        ProducerOnlineMarketPlaceFee = 257900,
        ProducerLateRegistrationFee = 33200,
        SubsidiariesFee = 9071100,
        TotalFee = 9591000,
        PreviousPayment = 150000,
        SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown
        {
            TotalSubsidiariesOnlineMarketplaceFee = 7479100,
            CountOfOnlineMarketplaceSubsidiaries = 29,
            UnitOnlineMarketplaceFee = 257900,
        }
    };
    private static readonly PaymentInitiationRequest PaymentRequest = new PaymentInitiationRequest
    {
        UserId = Guid.NewGuid(),
        OrganisationId = Guid.NewGuid(),
        Reference = "222019EFGH",
        Regulator = "GB-ENG",
        Amount = 2045600
    };


    private static readonly ComplianceSchemeDetailsDto _complianceSchemeDetailsDto = new()
    {
        Members = [new ComplianceSchemeDetailsMemberDto {
             IsLateFeeApplicable = true,
             IsOnlineMarketplace = false,
             MemberId = "123",
             MemberType = "Large",
             NumberOfSubsidiaries = 2,
             NumberOfSubsidiariesBeingOnlineMarketPlace = 3
        },
            new ComplianceSchemeDetailsMemberDto {
             IsLateFeeApplicable = true,
             IsOnlineMarketplace = false,
             MemberId = "234",
             MemberType = "Small",
             NumberOfSubsidiaries = 5,
             NumberOfSubsidiariesBeingOnlineMarketPlace = 6
        },

        ]
    };

    private static readonly ComplianceSchemePaymentCalculationResponse _complianceSchemeCalculationResponse = new()
    {
        ComplianceSchemeMembersWithFees = [new ComplianceSchemePaymentCalculationResponseMember {
            MemberId = "123",
            MemberLateRegistrationFee = 5000,
            MemberOnlineMarketPlaceFee = 7000,
            MemberRegistrationFee = 9000,
            SubsidiariesFee = 11000,
            SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown {
                CountOfOnlineMarketplaceSubsidiaries = 1,
                TotalSubsidiariesOnlineMarketplaceFee = 2000,
                UnitOnlineMarketplaceFee = 3000,
                FeeBreakdowns = [new FeeBreakdown {
                    BandNumber = 5,
                    TotalPrice = 6000,
                    UnitCount = 7,
                    UnitPrice = 8000
                }]
            },
            TotalMemberFee = 15000
        }],
        TotalFee = 12345,
        PreviousPayment = 23456,
        ComplianceSchemeRegistrationFee = 20000,
        OutstandingPayment = 30000
    };

    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [SetUp]
    public void Init()
    {
        _accountServiceApiClientMock = new Mock<IAccountServiceApiClient>();
        _paymentServiceApiClientMock = new Mock<IPaymentCalculationServiceApiClient>();
        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        var facadeOptions = Options.Create(new PaymentFacadeApiOptions { DownstreamScope = "https://mock/test", Endpoints = new PaymentFacadeApiEndpoints { OnlinePaymentsEndpoint = "online-payments" } });
        _systemUnderTest = new PaymentCalculationService(_accountServiceApiClientMock.Object, _webApiGatewayClientMock.Object, _paymentServiceApiClientMock.Object, new NullLogger<PaymentCalculationService>(), facadeOptions);
    }

    [Test]
    public async Task ProducerExists_GetProducerRegistrationFees_Returns_CalculationResponse()
    {
        // Arrange
        var nationResponse = new HttpResponseMessage(HttpStatusCode.OK);
        nationResponse.Content = "GB-ENG".ToJsonContent();

        _accountServiceApiClientMock.Setup(client => client.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(nationResponse);

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = CalculationResponse.ToJsonContent(_jsonOptions);

        _paymentServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(ProducerDetailsDto, "1234", false, Guid.NewGuid(), DateTime.Now);

        // Assert
        result.Should().BeOfType(typeof(PaymentCalculationResponse));
        result.Should().BeEquivalentTo(CalculationResponse);
    }

    [Test]
    public async Task ProducerNotFound_GetProducerRegistrationFees_Returns_Null()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(ProducerDetailsDto, "1234", false, Guid.NewGuid(), DateTime.Today);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task WhenClientThrowsException_GetProducerRegistrationFees_Returns_Null()
    {
        // Arrange
        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>())).ThrowsAsync(new Exception());

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(ProducerDetailsDto, "1234", false, Guid.NewGuid(), DateTime.Today);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task RegulatorNationExists_FindPaymentCalculationInputParameters_Returns_CalculationRequest()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = "GB-ENG".ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        // Act
        _ = await _systemUnderTest.GetProducerRegistrationFees(ProducerDetailsDto, "1234", false, Guid.NewGuid(), DateTime.Today);

        // Assert
        _paymentServiceApiClientMock.Verify(client =>
            client.SendPostRequest(
                It.IsAny<string>(),
                It.Is<PaymentCalculationRequest>(request =>
                    request.Regulator == "GB-ENG"
                )
            ), Times.Once, "Regulator nation should be included in the POST request.");
    }

    [Test]
    public async Task RegulatorNationNotFound_FindPaymentCalculationInputParameters_Returns_BlankRegulator()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        // Act
        _ = await _systemUnderTest.GetProducerRegistrationFees(ProducerDetailsDto, "1234", false, Guid.NewGuid(), DateTime.Today);

        // Assert
        _paymentServiceApiClientMock.Verify(client =>
            client.SendPostRequest(
                It.IsAny<string>(),
                It.Is<PaymentCalculationRequest>(request =>
                    request.Regulator == string.Empty
                )
            ), Times.Once, "Regulator nation should be Empty in the POST request.");
    }

    [Test]
    public async Task OrganisationExists_GetRegulatorNation_Returns_ValidNation()
    {
        // Arrange
        const string nation = "GB-SCT";
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = nation.ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetRegulatorNation(Guid.NewGuid());

        // Assert
        result.Should().BeOfType(typeof(string));
        result.Should().BeEquivalentTo(nation);
    }

    [Test]
    public async Task OrganisationNotFound_GetRegulatorNation_Returns_Blank()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetRegulatorNation(Guid.NewGuid());

        // Assert
        result.Should().BeOfType(typeof(string));
        result.Should().BeEmpty();
    }

    [Test]
    public async Task WhenClientThrowsException_GetRegulatorNation_Returns_Blank()
    {
        // Arrange
        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ThrowsAsync(new Exception());

        // Act
        var result = await _systemUnderTest.GetRegulatorNation(Guid.NewGuid());

        // Assert
        result.Should().BeOfType(typeof(string));
        result.Should().BeEmpty();
    }

    [Test]
    public async Task ProducerExists_InitiatePayment_Returns_Success()
    {
        // Arrange
        const string expectedPaymentLink = "https://example/secure/9defb517-66f8-45cd-8d9b-20e571b76fb5";
        const string htmlContent = $@"<!DOCTYPE html><html lang=""en""><script>window.location.href = '{expectedPaymentLink}';</script>";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
        };

        _paymentServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentInitiationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.InitiatePayment(PaymentRequest);

        // Assert
        result.Should().Be(expectedPaymentLink);
    }

    [Test]
    public async Task ProducerExists_InitiatePayment_WhenNoLinkInHtml_ReturnsEmptyResult()
    {
        // Arrange
        const string htmlContent = $@"<!DOCTYPE html><html lang=""en""><p>HTML with no link</p>";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
        };

        _paymentServiceApiClientMock.Setup(x =>
            x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentInitiationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.InitiatePayment(PaymentRequest);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public async Task ProducerNotFound_InitiatePayment_Returns_EmptyResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _paymentServiceApiClientMock.Setup(x =>
                 x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentInitiationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.InitiatePayment(PaymentRequest);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public async Task WhenClientThrowsException_InitiatePayment_Returns_False()
    {
        // Arrange
        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentInitiationRequest>())).ThrowsAsync(new Exception());

        // Act
        var result = await _systemUnderTest.InitiatePayment(PaymentRequest);

        // Assert
        result.Should().Be(string.Empty);
    }

    [TestCase(false, 0)]
    [TestCase(true, 34)]
    public void CreateApplicationReferenceNumber_Returns_CorrectFormat(bool isComplianceScheme, int csRowNumber)
    {
        // Arrange
        const string organisationId = "100082";
        SubmissionPeriod[] submissionPeriods =
        [
            new SubmissionPeriod { StartMonth = "January", EndMonth = "June", Year = "2024" },
            new SubmissionPeriod { StartMonth = "April",   EndMonth = "September", Year = "2025" }
        ];

        foreach (var period in submissionPeriods)
        {
            // Act
            var reference = _systemUnderTest.CreateApplicationReferenceNumber(isComplianceScheme, csRowNumber, organisationId, period);

            switch (int.Parse(period.Year))
            {
                // Assert
                case 2024:
                    reference.Should().EndWith("P2");
                    break;
                case 2025:
                    reference.Should().EndWith("P1");
                    break;
            }
            reference.Should().Contain(organisationId);

            if (isComplianceScheme)
            {
                reference.Should().Contain(csRowNumber.ToString());
            }
        }
    }

    [Test]
    public async Task ComplianceSchemeExists_GetComplianceSchemeRegistrationFees_Returns_CalculationResponse()
    {
        // Arrange
        var nationResponse = new HttpResponseMessage(HttpStatusCode.OK);
        nationResponse.Content = "GB-ENG".ToJsonContent();

        _accountServiceApiClientMock.Setup(client => client.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(nationResponse);

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = _complianceSchemeCalculationResponse.ToJsonContent(_jsonOptions);

        _paymentServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(_complianceSchemeDetailsDto, "1234", Guid.NewGuid());

        // Assert
        result.Should().BeOfType(typeof(ComplianceSchemePaymentCalculationResponse));
        result.Should().BeEquivalentTo(_complianceSchemeCalculationResponse);
    }

    [Test]
    public async Task ComplianceSchemeExists_PaymentServiceNotFound_GetComplianceSchemeRegistrationFees_Returns_Null()
    {
        // Arrange
        var nationResponse = new HttpResponseMessage(HttpStatusCode.OK);
        nationResponse.Content = "GB-ENG".ToJsonContent();

        _accountServiceApiClientMock.Setup(client => client.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(nationResponse);

        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _paymentServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(_complianceSchemeDetailsDto, "1234", Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task ComplianceSchemeNotFound_GetComplianceSchemeRegistrationFees_Returns_Null()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(_complianceSchemeDetailsDto, "1234", Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task WhenClientThrowsException_GetComplianceSchemeRegistrationFees_Returns_Null()
    {
        // Arrange
        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationRequest>())).ThrowsAsync(new Exception());

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(_complianceSchemeDetailsDto, "1234", Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetComplianceSchemeDetails_ShouldReturnComplianceSchemeDetails_WhenClientReturnsValidResponse()
    {
        // Arrange
        var organisationId = "org-123";
        var expectedDetails = new ComplianceSchemeDetailsDto
        {
            Members = [new ComplianceSchemeDetailsMemberDto {
                IsLateFeeApplicable = true,
                IsOnlineMarketplace = false,
                MemberId = "123",
                MemberType = "Large",
                NumberOfSubsidiaries = 12,
                NumberOfSubsidiariesBeingOnlineMarketPlace = 9
            }]
        };

        _webApiGatewayClientMock
            .Setup(client => client.GetComplianceSchemeDetails(organisationId))
            .ReturnsAsync(expectedDetails);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeDetails(organisationId);

        // Assert
        result.Should().BeEquivalentTo(expectedDetails, "the returned compliance scheme details should match the expected details");
        _webApiGatewayClientMock.Verify(client => client.GetComplianceSchemeDetails(organisationId), Times.Once, "the method should call the client exactly once");
    }

    [Test]
    public void GetComplianceSchemeDetails_ShouldThrowException_WhenClientThrowsException()
    {
        // Arrange
        var organisationId = "org-123";

        _webApiGatewayClientMock
            .Setup(client => client.GetComplianceSchemeDetails(organisationId))
            .ThrowsAsync(new Exception("Client error"));

        // Act
        AsyncTestDelegate action = async () => await _systemUnderTest.GetComplianceSchemeDetails(organisationId);

        // Assert
        Assert.ThrowsAsync<Exception>(action, "the method should propagate exceptions from the client");
        _webApiGatewayClientMock.Verify(client => client.GetComplianceSchemeDetails(organisationId), Times.Once, "the method should call the client exactly once");
    }

    [Test]
    public async Task GetComplianceSchemeDetails_ShouldHandleNullResult_WhenClientReturnsNull()
    {
        // Arrange
        var organisationId = "org-123";

        _webApiGatewayClientMock
            .Setup(client => client.GetComplianceSchemeDetails(organisationId))
            .ReturnsAsync((ComplianceSchemeDetailsDto)null!);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeDetails(organisationId);

        // Assert
        result.Should().BeNull("the method should return null if the client returns null");
        _webApiGatewayClientMock.Verify(client => client.GetComplianceSchemeDetails(organisationId), Times.Once, "the method should call the client exactly once");
    }
}

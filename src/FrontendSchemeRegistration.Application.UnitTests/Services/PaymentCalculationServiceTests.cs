using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.PaymentCalculations;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FrontendSchemeRegistration.Application.UnitTests.Services;

[TestFixture]
public class PaymentCalculationServiceTests
{
	private Mock<IAccountServiceApiClient> _accountServiceApiClientMock;
	private Mock<IPaymentCalculationServiceApiClient> _paymentServiceApiClientMock;
	private PaymentCalculationService _systemUnderTest;
	private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;

    private static readonly PaymentCalculationResponse CalculationResponse = new PaymentCalculationResponse
    {
        ProducerRegistrationFee = 262000,
        ProducerOnlineMarketPlaceFee = 257900,
        ProducerClosedLoopRecyclingFee = 100000,
        ProducerLateRegistrationFee = 33200,
        SubsidiariesFee = 9071100,
        TotalFee = 9591000,
        PreviousPayment = 150000,
        SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown
        {
            TotalSubsidiariesOnlineMarketplaceFee = 7479100,
            CountOfOnlineMarketplaceSubsidiaries = 29,
            UnitOnlineMarketplaceFee = 257900,
            TotalSubsidiariesClosedLoopRecyclingFee = 50000,
            CountOfClosedLoopRecyclingSubsidiaries = 5,
            UnitClosedLoopRecyclingFee = 10000,
        }
    };

    private static readonly PaymentInitiationRequest PaymentRequest = new PaymentInitiationRequest
    {
        UserId = Guid.NewGuid(),
        OrganisationId = Guid.NewGuid(),
        Reference = "222019EFGH",
        Regulator = "GB-ENG",
        Amount = 2045600,
        FileId = Guid.NewGuid()
    };

    private static readonly ComplianceSchemePaymentCalculationResponse _complianceSchemeCalculationResponse = new()
    {
        ComplianceSchemeMembersWithFees =
        [
            new ComplianceSchemePaymentCalculationResponseMember
            {
                MemberId = "123",
                MemberLateRegistrationFee = 5000,
                MemberOnlineMarketPlaceFee = 7000,
                MemberRegistrationFee = 9000,
                SubsidiariesFee = 11000,
                SubsidiariesFeeBreakdown = new SubsidiariesFeeBreakdown
                {
                    CountOfOnlineMarketplaceSubsidiaries = 1,
                    TotalSubsidiariesOnlineMarketplaceFee = 2000,
                    UnitOnlineMarketplaceFee = 3000,
                    FeeBreakdowns =
                    [
                        new FeeBreakdown
                        {
                            BandNumber = 5,
                            TotalPrice = 6000,
                            UnitCount = 7,
                            UnitPrice = 8000
                        }
                    ]
                },
                TotalMemberFee = 15000
            }
        ],
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
		var facadeOptions = Microsoft.Extensions.Options.Options.Create(new PaymentFacadeApiOptions { DownstreamScope = "https://mock/test", Endpoints = new PaymentFacadeApiEndpoints { OnlinePaymentsEndpoint = "online-payments", RegistrationFeeCalculationDetailsEndpoint = "registration-submission-data/{submissionId}/fee-calculation-details" } });
		_systemUnderTest = new PaymentCalculationService(_accountServiceApiClientMock.Object, _paymentServiceApiClientMock.Object, new NullLogger<PaymentCalculationService>(), facadeOptions);
	}

	[Test]
	public async Task ProducerExists_GetProducerRegistrationFees_Returns_CalculationResponse()
	{
		// Arrange
		var nationResponse = new HttpResponseMessage(HttpStatusCode.OK);
		nationResponse.Content = "GB-ENG".ToJsonContent();

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = CalculationResponse.ToJsonContent(_jsonOptions);

        _paymentServiceApiClientMock.Setup(x =>
            x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(new PaymentCalculationRequest() { ApplicationReferenceNumber= "A2"});

        // Assert
        result.Should().BeOfType(typeof(PaymentCalculationResponse));
        result.Should().BeEquivalentTo(CalculationResponse);
    }

    [Test]
    public async Task ProducerNotFound_GetProducerRegistrationFees_Returns_Null()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        _paymentServiceApiClientMock.Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(new PaymentCalculationRequest { ApplicationReferenceNumber = "test" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task WhenClientThrowsException_GetProducerRegistrationFees_Returns_Null()
    {
        // Arrange
        _paymentServiceApiClientMock.Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>())).ThrowsAsync(new Exception());

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(new PaymentCalculationRequest { ApplicationReferenceNumber = "Test" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetProducerRegistrationFees_PassesClosedLoopFieldsOnRequest_AndDeserializesClosedLoopResponseFields()
    {
        // Arrange
        var request = new PaymentCalculationRequest
        {
            ApplicationReferenceNumber = "CLR-1",
            IsProducerOnlineMarketplace = true,
            NoOfSubsidiariesOnlineMarketplace = 2,
            IsClosedLoopRecycling = true,
            NoOfSubsidiariesClosedLoopRecycling = 3
        };

        PaymentCalculationRequest? captured = null;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CalculationResponse.ToJsonContent(_jsonOptions)
        };
        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>()))
            .Callback<string, PaymentCalculationRequest>((_, r) => captured = r)
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(request);

        // Assert
        captured.Should().NotBeNull();
        captured!.IsClosedLoopRecycling.Should().BeTrue();
        captured.NoOfSubsidiariesClosedLoopRecycling.Should().Be(3);
        captured.IsProducerOnlineMarketplace.Should().BeTrue();
        captured.NoOfSubsidiariesOnlineMarketplace.Should().Be(2);

        result.Should().NotBeNull();
        result!.ProducerClosedLoopRecyclingFee.Should().Be(100000);
        result.SubsidiariesFeeBreakdown.TotalSubsidiariesClosedLoopRecyclingFee.Should().Be(50000);
        result.SubsidiariesFeeBreakdown.CountOfClosedLoopRecyclingSubsidiaries.Should().Be(5);
        result.SubsidiariesFeeBreakdown.UnitClosedLoopRecyclingFee.Should().Be(10000);
    }

    [Test]
    public void SubsidiariesFeeBreakdown_DeserializesUsingExternalJsonNames_ForClosedLoopRecycling()
    {
        // Arrange - mirrors payment-service contract field names.
        const string json = """
        {
          "totalSubsidiariesClosedLoopRecyclingFees": 1234,
          "countOfClosedLoopRecyclingSubsidiaries": 6,
          "unitClosedLoopRecyclingFees": 200
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<SubsidiariesFeeBreakdown>(json, _jsonOptions);

        // Assert
        dto.Should().NotBeNull();
        dto!.TotalSubsidiariesClosedLoopRecyclingFee.Should().Be(1234);
        dto.CountOfClosedLoopRecyclingSubsidiaries.Should().Be(6);
        dto.UnitClosedLoopRecyclingFee.Should().Be(200);
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
        const string htmlContent = $"""<!DOCTYPE html><html lang="en"><script>window.location.href = '{expectedPaymentLink}';</script>""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
        };

        _paymentServiceApiClientMock.Setup(x =>
            x.SendPostRequest(It.IsAny<string>(), It.Is<PaymentInitiationRequest>(r => r.FileId == PaymentRequest.FileId))).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.InitiatePayment(PaymentRequest);

        // Assert
        result.Should().Be(expectedPaymentLink);
    }

    [Test]
    public async Task ProducerExists_InitiatePayment_WhenNoLinkInHtml_ReturnsEmptyResult()
    {
        // Arrange
        const string htmlContent = """<!DOCTYPE html><html lang="en"><p>HTML with no link</p>""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
        };

        _paymentServiceApiClientMock.Setup(x =>
            x.SendPostRequest(It.IsAny<string>(), It.Is<PaymentInitiationRequest>(r => r.FileId == PaymentRequest.FileId))).ReturnsAsync(response);

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
            x.SendPostRequest(It.IsAny<string>(), It.Is<PaymentInitiationRequest>(r => r.FileId == PaymentRequest.FileId))).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.InitiatePayment(PaymentRequest);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public async Task WhenClientThrowsException_InitiatePayment_Returns_False()
    {
        // Arrange
        _paymentServiceApiClientMock.Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>())).ThrowsAsync(new Exception());

        // Act
        var result = await _systemUnderTest.InitiatePayment(PaymentRequest);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public async Task ComplianceSchemeExists_GetComplianceSchemeRegistrationFees_Returns_CalculationResponse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = _complianceSchemeCalculationResponse.ToJsonContent(_jsonOptions);

        _paymentServiceApiClientMock.Setup(x =>
            x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationRequest() { ApplicationReferenceNumber = "A1B2"});

        // Assert
        result.Should().BeOfType(typeof(ComplianceSchemePaymentCalculationResponse));
        result.Should().BeEquivalentTo(_complianceSchemeCalculationResponse);
    }

    [Test]
    public async Task ComplianceSchemeNotFound_GetComplianceSchemeRegistrationFees_Returns_Null()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _paymentServiceApiClientMock.Setup(x =>
            x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationRequest>())).ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationRequest { ApplicationReferenceNumber = "Test" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task WhenClientThrowsException_GetComplianceSchemeRegistrationFees_Returns_Null()
    {
        // Arrange
        _paymentServiceApiClientMock.Setup(x =>
            x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationRequest>())).ThrowsAsync(new Exception());

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationRequest { ApplicationReferenceNumber = "Test" });

        // Assert
        result.Should().BeNull();
    }

	[Test]
	public async Task ComplianceSchemeExists_GetResubmissionFees_Returns_CalculationResponse()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.OK);
		response.Content = CalculationResponse.ToJsonContent(_jsonOptions);

		_paymentServiceApiClientMock.Setup(x =>
				x.SendPostRequest(It.IsAny<string>(), It.IsAny<PackagingPaymentRequest>())).ReturnsAsync(response);

		// Act
		var result = await _systemUnderTest.GetResubmissionFees("test ref", "GB-ENG", 1, true, null);

		// Assert
		result.Should().BeOfType(typeof(PackagingPaymentResponse));
		result.Should().BeEquivalentTo(new PackagingPaymentResponse());
	}

	[Test]
	public async Task ComplianceSchemeExists_PaymentServiceNotFound_GetResubmissionFees_Returns_Null()
	{
		// Arrange

		var response = new HttpResponseMessage(HttpStatusCode.NotFound);

		_paymentServiceApiClientMock.Setup(x =>
				x.SendPostRequest(It.IsAny<string>(), It.IsAny<PackagingPaymentRequest>())).ReturnsAsync(response);

		// Act
		var result = await _systemUnderTest.GetResubmissionFees("test ref", "GB-ENG", 1, true, null);

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public async Task ComplianceSchemeExists_WhenClientThrowsException_GetResubmissionFees_Returns_Null()
	{
		// Arrange
		_paymentServiceApiClientMock.Setup(x =>
				x.SendPostRequest(It.IsAny<string>(), It.IsAny<PackagingPaymentRequest>())).ThrowsAsync(new Exception());

		// Act
		var result = await _systemUnderTest.GetResubmissionFees("test ref", "GB-ENG", 1, true, null);

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public async Task ProducerExists_GetResubmissionFees_Returns_CalculationResponse()
	{
		// Arrange
		var response = new HttpResponseMessage(HttpStatusCode.OK);
		response.Content = CalculationResponse.ToJsonContent(_jsonOptions);

		_paymentServiceApiClientMock.Setup(x =>
				x.SendPostRequest(It.IsAny<string>(), It.IsAny<PackagingPaymentRequest>())).ReturnsAsync(response);

		// Act
		var result = await _systemUnderTest.GetResubmissionFees("test ref", "GB-ENG", 0, false, null);

		// Assert
		result.Should().BeOfType(typeof(PackagingPaymentResponse));
		result.Should().BeEquivalentTo(new PackagingPaymentResponse());
	}

	[Test]
	public async Task ProducerExists_PaymentServiceNotFound_GetResubmissionFees_Returns_Null()
	{
		// Arrange

		var response = new HttpResponseMessage(HttpStatusCode.NotFound);

		_paymentServiceApiClientMock.Setup(x =>
				x.SendPostRequest(It.IsAny<string>(), It.IsAny<PackagingPaymentRequest>())).ReturnsAsync(response);

		// Act
		var result = await _systemUnderTest.GetResubmissionFees("test ref", "GB-ENG", 0, false, null);

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public async Task ProducerExists_WhenClientThrowsException_GetResubmissionFees_Returns_Null()
	{
		// Arrange
		_paymentServiceApiClientMock.Setup(x =>
				x.SendPostRequest(It.IsAny<string>(), It.IsAny<PackagingPaymentRequest>())).ThrowsAsync(new Exception());

		// Act
		var result = await _systemUnderTest.GetResubmissionFees("test ref", "GB-ENG", 0, false, null);

		// Assert
		result.Should().BeNull();
	}

    [Test]
    public async Task ComplianceSchemeExists_Returns_Emptyobject_When_MemberCount_Is_Zero()
    {
        // Arrange

        var response = new PackagingPaymentResponse();

        // Act
        var result = await _systemUnderTest.GetResubmissionFees("test ref", "GB-ENG", 0, true, null);

        // Assert
        result.Should().BeEquivalentTo(response);
    }

    [Test]
    public async Task GetProducerRegistrationFees_V2_Succeeds()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = PaymentCalculationServiceTests.CalculationResponse.ToJsonContent(_jsonOptions)
        };

        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ProducerPaymentCalculationV2Request>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(new ProducerPaymentCalculationV2Request { ApplicationReferenceNumber = "V2-OK" });

        // Assert
        result.Should().BeEquivalentTo(PaymentCalculationServiceTests.CalculationResponse);
    }

    [Test]
    public async Task GetProducerRegistrationFees_V2_NotFound_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ProducerPaymentCalculationV2Request>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(new ProducerPaymentCalculationV2Request { ApplicationReferenceNumber = "V2-NF" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetProducerRegistrationFees_V2_ServerError_EnsureSuccess_Throws_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error")
        };
        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ProducerPaymentCalculationV2Request>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(new ProducerPaymentCalculationV2Request { ApplicationReferenceNumber = "V2-500" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetProducerRegistrationFees_V2_Exception_ReturnsNull()
    {
        // Arrange
        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ProducerPaymentCalculationV2Request>()))
            .ThrowsAsync(new Exception("exception"));

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(new ProducerPaymentCalculationV2Request { ApplicationReferenceNumber = "V2-EX" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_V2_Succeeds()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = PaymentCalculationServiceTests._complianceSchemeCalculationResponse.ToJsonContent(_jsonOptions)
        };

        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationV2Request>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationV2Request { ApplicationReferenceNumber = "CSV2-OK" });

        // Assert
        result.Should().BeEquivalentTo(PaymentCalculationServiceTests._complianceSchemeCalculationResponse);
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_V2_NotFound_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationV2Request>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationV2Request { ApplicationReferenceNumber = "CSV2-NF" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_V2_ServerError_EnsureSuccess_Throws_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error")
        };
        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationV2Request>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationV2Request { ApplicationReferenceNumber = "CSV2-500" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_V2_Exception_ReturnsNull()
    {
        // Arrange
        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationV2Request>()))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationV2Request { ApplicationReferenceNumber = "CSV2-EX" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task InitiatePayment_WhenClientThrowsException_ReturnsEmpty()
    {
        // Arrange
        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.Is<PaymentInitiationRequest>(r => r.FileId == PaymentRequest.FileId)))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _systemUnderTest.InitiatePayment(PaymentCalculationServiceTests.PaymentRequest);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public async Task GetProducerRegistrationFees_ServerError_EnsureSuccess_Throws_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error")
        };

        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<PaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetProducerRegistrationFees(new PaymentCalculationRequest { ApplicationReferenceNumber = "P-500" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetComplianceSchemeRegistrationFees_ServerError_EnsureSuccess_Throws_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error")
        };

        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<ComplianceSchemePaymentCalculationRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetComplianceSchemeRegistrationFees(new ComplianceSchemePaymentCalculationRequest { ApplicationReferenceNumber = "CS-500" });

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetResubmissionFees_ServerError_EnsureSuccess_Throws_ReturnsNull_ForProducer()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error")
        };

        _paymentServiceApiClientMock
            .Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<PackagingPaymentRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetResubmissionFees("ref", "GB-ENG", memberCount: 0, isComplianceScheme: false, resubmissionDate: null);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetRegulatorNation_ServerError_EnsureSuccess_Throws_ReturnsEmpty()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error")
        };

        _accountServiceApiClientMock
            .Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _systemUnderTest.GetRegulatorNation(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetRegistrationFeeCalculationDetails_Ok_ReturnsArrayFromBody()
    {
        var submissionId = Guid.NewGuid();
        var payload = new[]
        {
            new FrontendSchemeRegistration.Application.DTOs.RegistrationFeeCalculationDetails
            {
                OrganisationId = "ORG-1",
                OrganisationSize = "Large",
                NationId = 1,
                NumberOfSubsidiaries = 3,
                NumberOfSubsidiariesBeingOnlineMarketPlace = 1,
                NumberOfSubsidiariesBeingClosedLoopRecycling = 2,
                IsOnlineMarketplace = true,
                IsClosedLoopRecycling = true,
                IsNewJoiner = false,
            },
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json"),
        };
        _paymentServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>())).ReturnsAsync(response);

        var result = await _systemUnderTest.GetRegistrationFeeCalculationDetails(submissionId);

        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(payload);
        _paymentServiceApiClientMock.Verify(
            x => x.SendGetRequest($"registration-submission-data/{submissionId}/fee-calculation-details"),
            Times.Once);
    }

    [Test]
    public async Task GetRegistrationFeeCalculationDetails_NotFound_ReturnsNull()
    {
        _paymentServiceApiClientMock
            .Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await _systemUnderTest.GetRegistrationFeeCalculationDetails(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task GetRegistrationFeeCalculationDetails_ClientThrows_ReturnsNull()
    {
        _paymentServiceApiClientMock
            .Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await _systemUnderTest.GetRegistrationFeeCalculationDetails(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task GetRegistrationFeeCalculationDetails_ServerError_ReturnsNull()
    {
        _paymentServiceApiClientMock
            .Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await _systemUnderTest.GetRegistrationFeeCalculationDetails(Guid.NewGuid());

        result.Should().BeNull();
    }
}
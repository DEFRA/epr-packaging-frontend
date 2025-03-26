namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using Application.Services;
using Application.Services.Interfaces;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Organisation;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.Text;

[TestFixture]
public class SubsidiaryServiceTests
{
    private const string NewOrganisationId = "123456";

    private Mock<IAccountServiceApiClient> _accountServiceApiClientMock;
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private SubsidiaryService _sut;
    private const string RedisFileUploadStatusViewedKey = "SubsidiaryFileUploadStatusViewed";
    private Mock<IDistributedCache> _mockDistributedCache;

    [SetUp]
    public void Init()
    {
        _accountServiceApiClientMock = new Mock<IAccountServiceApiClient>();
        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        _mockDistributedCache = new Mock<IDistributedCache>();

        _sut = new SubsidiaryService(_accountServiceApiClientMock.Object, _webApiGatewayClientMock.Object, new NullLogger<SubsidiaryService>(), _mockDistributedCache.Object);
    }

    [Test]
    public async Task AddSubsidiary_Returns_OrganisationId()
    {
        // Arrange
        var subsidiaryAddDto = new SubsidiaryAddDto
        {
            ParentOrganisationId = Guid.NewGuid(),
            ChildOrganisationId = Guid.NewGuid(),
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = NewOrganisationId.ToJsonContent();

        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest<SubsidiaryAddDto>(It.IsAny<string>(), It.IsAny<SubsidiaryAddDto>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.AddSubsidiary(subsidiaryAddDto);

        // Assert
        result.Should().Be(NewOrganisationId);
    }

    [Test]
    public async Task AddSubsidiary_Returns_OrganisationId_NotFound()
    {
        // Arrange
        var subsidiaryAddDto = new SubsidiaryAddDto
        {
            ParentOrganisationId = Guid.NewGuid(),
            ChildOrganisationId = Guid.NewGuid(),
        };

        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest<SubsidiaryAddDto>(It.IsAny<string>(), It.IsAny<SubsidiaryAddDto>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.AddSubsidiary(subsidiaryAddDto);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task AddSubsidiary_WhenClientThrowsException_ThrowsException()
    {
        // Arrange
        var subsidiaryAddDto = new SubsidiaryAddDto
        {
            ParentOrganisationId = Guid.NewGuid(),
            ChildOrganisationId = Guid.NewGuid(),
        };

        // Act &  Assert
        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest<SubsidiaryAddDto>(It.IsAny<string>(), It.IsAny<SubsidiaryAddDto>()))
            .ThrowsAsync(new Exception());
        Func<Task> act = async () => await _sut.AddSubsidiary(subsidiaryAddDto);

        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task SaveSubsidiary_Returns_OrganisationId()
    {
        // Arrange
        var subsidiaryDto = new SubsidiaryDto
        {
            Subsidiary = new OrganisationModel { CompaniesHouseNumber = "0123456X", Name = "Test Company" }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = NewOrganisationId.ToJsonContent();

        _accountServiceApiClientMock
            .Setup(x => x.SendPostRequest<SubsidiaryDto>(It.IsAny<string>(), It.IsAny<SubsidiaryDto>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.SaveSubsidiary(subsidiaryDto);

        // Assert
        result.Should().Be(NewOrganisationId);
    }

    [Test]
    public async Task SaveSubsidiary_Returns_OrganisationId_NotFound()
    {
        // Arrange
        var subsidiaryDto = new SubsidiaryDto
        {
            Subsidiary = new OrganisationModel { CompaniesHouseNumber = "0123456X", Name = "Test Company" }
        };

        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _accountServiceApiClientMock
            .Setup(x => x.SendPostRequest<SubsidiaryDto>(It.IsAny<string>(), It.IsAny<SubsidiaryDto>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.SaveSubsidiary(subsidiaryDto);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task SaveSubsidiary_WhenClientThrowsException_ThrowsException()
    {
        // Arrange
        var subsidiaryDto = new SubsidiaryDto
        {
            Subsidiary = new OrganisationModel { CompaniesHouseNumber = "0123456X", Name = "Test Company" }
        };

        // Act &  Assert
        _accountServiceApiClientMock.Setup(x =>
                x.SendPostRequest<SubsidiaryAddDto>(It.IsAny<string>(), It.IsAny<SubsidiaryAddDto>()))
            .ThrowsAsync(new Exception());
        Func<Task> act = async () => await _sut.SaveSubsidiary(subsidiaryDto);

        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task GetSubsidiariesStreamAsync_WithComplianceScheme_ReturnsExpectedStream_With_Joiner_And_Leaver_Columns()
    {
        // Arrange
        const bool isComplianceScheme = true;
        const string companiesHouseNumber = "CHN001";
        const string organisationName = "CS Subsidiary 1";
        const string organisationId = "100";
        const string subsidiaryId = "123";
        const string reportingType = "Group";
        var joinerDate = DateTime.Parse("2025-02-01");

        var expectedModel = new List<ExportOrganisationSubsidiariesResponseModel>
        {
            new()
            {
                CompaniesHouseNumber = companiesHouseNumber, OrganisationName = organisationName, OrganisationId = organisationId, SubsidiaryId = subsidiaryId, JoinerDate = joinerDate, ReportingType = reportingType
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = expectedModel.ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetSubsidiariesStreamAsync(Guid.NewGuid(), Guid.NewGuid(), isComplianceScheme, true);

        // Assert
        result.Should().NotBeNull();
        result.Position.Should().Be(0);
        _accountServiceApiClientMock.Verify(client => client.SendGetRequest(It.Is<string>(s => s.Contains("compliance-schemes"))), Times.Once);

        using var reader = new StreamReader(result);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("organisation_id,subsidiary_id,organisation_name,companies_house_number,joiner_date,reporting_type");
        content.Should().Contain($"{organisationId},{subsidiaryId},{organisationName},{companiesHouseNumber},{joinerDate:dd/MM/yyyy},{reportingType}");
    }

    [Test]
    public async Task GetSubsidiariesStreamAsync_WithComplianceScheme_ReturnsExpectedStream_Without_Joiner_And_Leaver_Columns()
    {
        // Arrange
        const bool isComplianceScheme = true;
        const string companiesHouseNumber = "CHN001";
        const string organisationName = "CS Subsidiary 1";
        const string organisationId = "100";
        const string subsidiaryId = "123";

        var expectedModel = new List<ExportOrganisationSubsidiariesResponseModel>
        {
            new()
            {
                CompaniesHouseNumber = companiesHouseNumber, OrganisationName = organisationName, OrganisationId = organisationId, SubsidiaryId = subsidiaryId
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = expectedModel.ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetSubsidiariesStreamAsync(Guid.NewGuid(), Guid.NewGuid(), isComplianceScheme, false);

        // Assert
        result.Should().NotBeNull();
        result.Position.Should().Be(0);
        _accountServiceApiClientMock.Verify(client => client.SendGetRequest(It.Is<string>(s => s.Contains("compliance-schemes"))), Times.Once);

        using var reader = new StreamReader(result);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("organisation_id,subsidiary_id,organisation_name,companies_house_number");
        content.Should().Contain($"{organisationId},{subsidiaryId},{organisationName},{companiesHouseNumber}");
    }

    [Test]
    public async Task GetSubsidiariesStreamAsync_WithDirectProducer_ReturnsExpectedStream_With_Joiner_And_Leaver_Columns()
    {
        // Arrange
        const bool isComplianceScheme = false;
        const string companiesHouseNumber = "CHN001";
        const string organisationName = "DP Subsidiary 1";
        const string organisationId = "500";
        const string subsidiaryId = "523";
        const string reportingType = "Individual";
        var joinerDate = DateTime.Parse("2025-02-01");

        var expectedModel = new List<ExportOrganisationSubsidiariesResponseModel>
        {
            new()
            {
                CompaniesHouseNumber = companiesHouseNumber, OrganisationName = organisationName, OrganisationId = organisationId, SubsidiaryId = subsidiaryId, JoinerDate = joinerDate, ReportingType = reportingType
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = expectedModel.ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetSubsidiariesStreamAsync(Guid.NewGuid(), Guid.NewGuid(), isComplianceScheme, true);

        // Assert
        result.Should().NotBeNull();
        result.Position.Should().Be(0);
        _accountServiceApiClientMock.Verify(client => client.SendGetRequest(It.Is<string>(s => s.Contains("organisations"))), Times.Once);

        using var reader = new StreamReader(result);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("organisation_id,subsidiary_id,organisation_name,companies_house_number,joiner_date,reporting_type");
        content.Should().Contain($"{organisationId},{subsidiaryId},{organisationName},{companiesHouseNumber},{joinerDate:dd/MM/yyyy},{reportingType}");
    }

    [Test]
    public async Task GetSubsidiariesStreamAsync_WithDirectProducer_ReturnsExpectedStream_Without_Joiner_And_Leaver_Columns()
    {
        // Arrange
        const bool isComplianceScheme = false;
        const string companiesHouseNumber = "CHN001";
        const string organisationName = "DP Subsidiary 1";
        const string organisationId = "500";
        const string subsidiaryId = "523";

        var expectedModel = new List<ExportOrganisationSubsidiariesResponseModel>
        {
            new()
            {
                CompaniesHouseNumber = companiesHouseNumber, OrganisationName = organisationName, OrganisationId = organisationId, SubsidiaryId = subsidiaryId
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = expectedModel.ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetSubsidiariesStreamAsync(Guid.NewGuid(), Guid.NewGuid(), isComplianceScheme, false);

        // Assert
        result.Should().NotBeNull();
        result.Position.Should().Be(0);
        _accountServiceApiClientMock.Verify(client => client.SendGetRequest(It.Is<string>(s => s.Contains("organisations"))), Times.Once);

        using var reader = new StreamReader(result);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("organisation_id,subsidiary_id,organisation_name,companies_house_number");
        content.Should().Contain($"{organisationId},{subsidiaryId},{organisationName},{companiesHouseNumber}");
    }

    [Test]
    public async Task GetPagedOrganisationSubsidiaries_WhenApiThrowsException_ReturnsException()
    {
        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        // Act and Assert
        Func<Task> act = async () => await _sut.GetPagedOrganisationSubsidiaries(1, 20);

        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task GetPagedOrganisationSubsidiaries_WhenApiReturnsSuccessfully_ReturnsResponse()
    {
        var expectedModel = new PaginatedResponse<RelationshipResponseModel>
        {
            CurrentPage = 1,
            TotalItems = 1,
            PageSize = 20,
            Items = new List<RelationshipResponseModel>
            {
                new RelationshipResponseModel
                {
                    OrganisationName = "Test1",
                    OrganisationNumber = "2345",
                    RelationshipType = "Parent",
                    CompaniesHouseNumber = "CH123455"
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = expectedModel.ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetPagedOrganisationSubsidiaries(1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedModel);
    }

    [Test]
    public async Task GetOrganisationSubsidiaries_WhenApiThrowsException_ReturnsException()
    {
        var organisationId = Guid.NewGuid();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        // Act and Assert
        Func<Task> act = async () => await _sut.GetOrganisationSubsidiaries(organisationId);

        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task GetOrganisationSubsidiaries_WhenApiReturnsUnsuccessfully_ReturnsNull()
    {
        var organisationId = Guid.NewGuid();
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetOrganisationSubsidiaries(organisationId);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetOrganisationSubsidiaries_WhenApiReturnsSuccessfully_ReturnsResponse()
    {
        var organisationId = Guid.NewGuid();
        var expectedModel = new OrganisationRelationshipModel
        {
            Organisation = new OrganisationDetailModel
            {
                Name = "Test Company",
                OrganisationNumber = "0123456789"
            },
            Relationships = new List<RelationshipResponseModel>
            {
                new() { OrganisationName = "Subsidiary1" }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = expectedModel.ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetOrganisationSubsidiaries(organisationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedModel);
    }

    [Test]
    public async Task GetSubsidiaryFileUploadStatusAsync_NoStatus_ReturnsNoFileUploadActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        _webApiGatewayClientMock.Setup(c => c.GetSubsidiaryFileUploadStatusAsync(userId, organisationId))
            .ReturnsAsync(new UploadFileErrorResponse { Status = string.Empty });

        // Act
        var result = await _sut.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);

        // Assert
        result.Should().Be(SubsidiaryFileUploadStatus.NoFileUploadActive);
    }

    [Test]
    public async Task GetSubsidiaryFileUploadStatusAsync_FinishedWithNoErrors_ReturnsFileUploadedSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        _webApiGatewayClientMock.Setup(c => c.GetSubsidiaryFileUploadStatusAsync(userId, organisationId))
            .ReturnsAsync(new UploadFileErrorResponse { Status = "finished", Errors = null });

        // Act
        var result = await _sut.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);

        // Assert
        result.Should().Be(SubsidiaryFileUploadStatus.FileUploadedSuccessfully);
    }

    [Test]
    public async Task GetSubsidiaryFileUploadStatusAsync_FinishedWithErrors_ReturnsHasErrors()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        _webApiGatewayClientMock.Setup(c => c.GetSubsidiaryFileUploadStatusAsync(userId, organisationId))
            .ReturnsAsync(new UploadFileErrorResponse { Status = "finished", Errors = new List<UploadFileErrorModel>() });

        // Act
        var result = await _sut.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);

        // Assert
        result.Should().Be(SubsidiaryFileUploadStatus.HasErrors);
    }
    [Test]
    public async Task GetSubsidiaryFileUploadStatusAsync_FinishedWithPartialErrors_ReturnsPartialSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        _webApiGatewayClientMock.Setup(c => c.GetSubsidiaryFileUploadStatusAsync(userId, organisationId))
            .ReturnsAsync(new UploadFileErrorResponse { Status = "finished", Errors = new List<UploadFileErrorModel>() { new UploadFileErrorModel() }, RowsAdded = 2 });

        // Act
        var result = await _sut.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);

        // Assert
        result.Should().Be(SubsidiaryFileUploadStatus.PartialUpload);
    }

    [Test]
    public async Task GetSubsidiaryFileUploadStatusAsync_Uploading_ReturnsFileUploadInProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        _webApiGatewayClientMock.Setup(c => c.GetSubsidiaryFileUploadStatusAsync(userId, organisationId))
            .ReturnsAsync(new UploadFileErrorResponse { Status = "uploading" });

        // Act
        var result = await _sut.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);

        // Assert
        result.Should().Be(SubsidiaryFileUploadStatus.FileUploadInProgress);
    }

    [Test]
    public async Task GetSubsidiaryFileUploadStatusAsync_UnknownStatus_ReturnsNoFileUploadActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        _webApiGatewayClientMock.Setup(c => c.GetSubsidiaryFileUploadStatusAsync(userId, organisationId))
            .ReturnsAsync(new UploadFileErrorResponse { Status = "unknown" });

        // Act
        var result = await _sut.GetSubsidiaryFileUploadStatusAsync(userId, organisationId);

        // Assert
        result.Should().Be(SubsidiaryFileUploadStatus.NoFileUploadActive);
    }

    [Test]
    public async Task SetSubsidiaryFileUploadStatusViewedAsync_ShouldSetCorrectKeyAndValue()
    {
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var redisKey = $"SubsidiaryFileUploadStatusViewed:{userId}:{organisationId}";
        var valueToCache = true;

        var serializedValue = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(valueToCache));

        _mockDistributedCache
            .Setup(c => c.SetAsync(redisKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default))
            .Callback<string, byte[], DistributedCacheEntryOptions, System.Threading.CancellationToken>((key, value, options, token) =>
            {
                key.Should().Be(redisKey, "because the cache key should match the expected format");
                value.Should().BeEquivalentTo(serializedValue, "because the serialized value should match the boolean value to cache");

            })
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SetSubsidiaryFileUploadStatusViewedAsync(valueToCache, userId, organisationId);

        // Assert
        _mockDistributedCache.Verify(c => c.SetAsync(redisKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Once);
    }

    [Test]
    public async Task GetSubsidiaryFileUploadStatusViewedAsync_ShouldReturnTrue_WhenValueIsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var redisKey = $"{RedisFileUploadStatusViewedKey}:{userId}:{organisationId}";
        var expectedValue = true;

        var serializedValue = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(expectedValue));
        _mockDistributedCache.Setup(c => c.Get(redisKey))
                             .Returns(serializedValue);

        // Act
        var result = await _sut.GetSubsidiaryFileUploadStatusViewedAsync(userId, organisationId);

        // Assert
        result.Should().BeTrue("because the value was found in the cache and is expected to be true");
    }

    [Test]
    public async Task GetSubsidiaryFileUploadStatusViewedAsync_ShouldReturnFalse_WhenCacheMissOccurs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var redisKey = $"{RedisFileUploadStatusViewedKey}:{userId}:{organisationId}";

        _mockDistributedCache
            .Setup(c => c.Get(redisKey))
            .Returns((byte[])null);

        // Act
        var result = await _sut.GetSubsidiaryFileUploadStatusViewedAsync(userId, organisationId);

        // Assert
        result.Should().BeFalse("because a cache miss should lead to a return value of false");
    }

    [Test]
    public async Task GetOrganisationByReferenceNumber_WhenApiReturnsSuccessfully_ReturnsResponse()
    {
        var referenceNumber = "abc";

        var expectedOrganisationDto = new OrganisationDto
        {
            CompaniesHouseNumber = "abc",
            Id = 12345,
            ExternalId = Guid.NewGuid(),
            Name = "Test",
            RegistrationNumber = "ABC",
            TradingName = "DEF"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = expectedOrganisationDto.ToJsonContent();

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetOrganisationByReferenceNumber(referenceNumber);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedOrganisationDto);
    }

    [Test]
    public async Task GetOrganisationByReferenceNumber_WhenApiReturnsError_ReturnsResponse()
    {
        var referenceNumber = "abc";

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        response.Content = null;

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetOrganisationByReferenceNumber(referenceNumber);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetOrganisationByReferenceNumber_WhenApiThrowsException_ThrowsException()
    {
        var referenceNumber = "abc";

        _accountServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .Throws<Exception>();

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _sut.GetOrganisationByReferenceNumber(referenceNumber));
    }

    [Test]
    public async Task TerminateSubsidiary_WhenApiReturns_SuccessResult_ReturnOkStatus()
    {
        var parentOrganisationExternalId = Guid.NewGuid();
        var childOrganisationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var response = new HttpResponseMessage(HttpStatusCode.OK);

        _accountServiceApiClientMock.Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(response);

        // Act/ Assert
        Assert.DoesNotThrowAsync(async () => await _sut.TerminateSubsidiary(parentOrganisationExternalId, childOrganisationId, userId));
    }


    [Test]
    public async Task TerminateSubsidiary_WhenApiReturnsError_ThrowsException()
    {
        var parentOrganisationExternalId = Guid.NewGuid();
        var childOrganisationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _accountServiceApiClientMock.Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(response);

        // Act/Assert
        Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.TerminateSubsidiary(parentOrganisationExternalId, childOrganisationId, userId));
    }

    [Test]
    public async Task TerminateSubsidiary_WhenApiReturnsNotFound_NoError()
    {
        var parentOrganisationExternalId = Guid.NewGuid();
        var childOrganisationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _accountServiceApiClientMock.Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Assert
        Assert.DoesNotThrowAsync(async () => await _sut.TerminateSubsidiary(parentOrganisationExternalId, childOrganisationId, userId));
    }

    [Test]
    public async Task GetUploadStatus_WhenCallSuccessful_ReturnsResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        var expectedDto = new SubsidiaryUploadStatusDto
        {
            Status = SubsidiaryUploadStatus.Uploading
        };

        _webApiGatewayClientMock.Setup(x => x.GetSubsidiaryUploadStatus(userId, organisationId)).ReturnsAsync(expectedDto);

        // Act
        var result = await _sut.GetUploadStatus(userId, organisationId);

        // Assert
        result.Should().Be(expectedDto);
        _webApiGatewayClientMock.Verify(x => x.GetSubsidiaryUploadStatus(userId, organisationId), Times.Once);
    }

    [TestCase(true, "Error")]
    [TestCase(false, "Warning")]
    public async Task GetUploadErrorsReport_WhenErrorsExist_ReturnsFileWithErrors(bool isError, string expectedIssue)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        var errors = new SubsidiaryUploadErrorDto[]
        {
            new SubsidiaryUploadErrorDto
            {
                FileLineNumber = 2,
                FileContent = ",test-2,test-3,test-4,test-5,test-6\r\n",
                Message = "test-1 issue.",
                IsError = isError,
                ErrorNumber = 7,
            }
        };

        _webApiGatewayClientMock.Setup(x => x.GetSubsidiaryUploadStatus(userId, organisationId)).ReturnsAsync(new SubsidiaryUploadStatusDto { Errors = errors });

        var expectedString = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,Row Number,Issue,Message\r\n"
            + $"{errors[0].FileContent.Replace("\r\n", "")},{errors[0].FileLineNumber},{expectedIssue},{errors[0].Message}\r\n";

        // Act
        var result = await _sut.GetUploadErrorsReport(userId, organisationId);

        // Assert
        var resultAsString = new StreamReader(result).ReadToEnd();

        resultAsString.Should().Be(expectedString);
        _webApiGatewayClientMock.Verify(x => x.GetSubsidiaryUploadStatus(userId, organisationId), Times.Once);
    }

    [Test]
    public async Task GetUploadErrorsReport_WhenFileContentEmpty_ReturnsFileWithEmptyColumnsAndError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        var errors = new SubsidiaryUploadErrorDto[]
        {
            new SubsidiaryUploadErrorDto
            {
                FileLineNumber = 2,
                FileContent = "",
                Message = "No rows",
                IsError = true,
                ErrorNumber = 7,
            }
        };

        _webApiGatewayClientMock.Setup(x => x.GetSubsidiaryUploadStatus(userId, organisationId)).ReturnsAsync(new SubsidiaryUploadStatusDto { Errors = errors });

        var expectedString = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,Row Number,Issue,Message\r\n"
            + $",,,,,,{errors[0].FileLineNumber},Error,{errors[0].Message}\r\n";

        // Act
        var result = await _sut.GetUploadErrorsReport(userId, organisationId);

        // Assert
        var resultAsString = new StreamReader(result).ReadToEnd();

        resultAsString.Should().Be(expectedString);
        _webApiGatewayClientMock.Verify(x => x.GetSubsidiaryUploadStatus(userId, organisationId), Times.Once);
    }

    [Test]
    public async Task GetUploadErrorsReport_WhenNoErrorsExist_ReturnsFileWithoutErrors()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();

        _webApiGatewayClientMock.Setup(x => x.GetSubsidiaryUploadStatus(userId, organisationId)).ReturnsAsync(new SubsidiaryUploadStatusDto());

        var expectedString = "organisation_id,subsidiary_id,organisation_name,companies_house_number,parent_child,franchisee_licensee_tenant,Row Number,Issue,Message\r\n";

        // Act
        var result = await _sut.GetUploadErrorsReport(userId, organisationId);

        // Assert
        var resultAsString = new StreamReader(result).ReadToEnd();

        resultAsString.Should().Be(expectedString);
        _webApiGatewayClientMock.Verify(x => x.GetSubsidiaryUploadStatus(userId, organisationId), Times.Once);
    }

    [Test]
    public async Task GetAllSubsidiariesStream_WhenApiReturnsSuccess_ReturnsValidStream()
    {
        // Arrange
        var responseData = new List<RelationshipResponseModel>
            {
                new RelationshipResponseModel { OrganisationName = "Test Org" }
            };

        _accountServiceApiClientMock.Setup(api => api.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseData))
            });

        // Act
        var result = await _sut.GetAllSubsidiariesStream();

        // Assert
        result.Should().NotBeNull();
        result.Position.Should().Be(0);
    }

    [Test]
    public async Task GetAllSubsidiariesStream_WhenMultiplePages_ReturnsCompleteData()
    {
        // Arrange
        var listOfSubsidiaries = new List<RelationshipResponseModel>
            {
                new RelationshipResponseModel { OrganisationName = "First Page Org" },
                new RelationshipResponseModel { OrganisationName = "Second Page Org" }
            };

        var requestIndex = 0;
        _accountServiceApiClientMock.Setup(api => api.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                requestIndex++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(listOfSubsidiaries))
                };
            });

        // Act
        var result = await _sut.GetAllSubsidiariesStream();

        // Assert
        result.Should().NotBeNull();
        using var reader = new StreamReader(result);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("First Page Org");
        content.Should().Contain("Second Page Org");
    }

    [Test]
    public async Task GetAllSubsidiariesStream_WhenApiReturnsFailure_ThrowsException()
    {
        // Arrange
        _accountServiceApiClientMock.Setup(api => api.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Internal Server Error"));

        // Act
        Func<Task> act = async () => await _sut.GetAllSubsidiariesStream();

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task GetAllSubsidiariesStream_WhenApiThrowsException_ThrowsException()
    {
        // Arrange
        _accountServiceApiClientMock.Setup(api => api.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        // Act
        Func<Task> act = async () => await _sut.GetAllSubsidiariesStream();

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
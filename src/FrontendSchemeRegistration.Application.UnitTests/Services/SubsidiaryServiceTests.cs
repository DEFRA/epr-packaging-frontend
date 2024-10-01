namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using Application.Services;
using Application.Services.Interfaces;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs.Organisation;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;
using FrontendSchemeRegistration.UI.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;

[TestFixture]
public class SubsidiaryServiceTests
{
    private const string NewOrganisationId = "123456";

    private Mock<IAccountServiceApiClient> _accountServiceApiClientMock;
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private SubsidiaryService _sut;

    [SetUp]
    public void Init()
    {
        _accountServiceApiClientMock = new Mock<IAccountServiceApiClient>();
        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        _sut = new SubsidiaryService(_accountServiceApiClientMock.Object, _webApiGatewayClientMock.Object, new NullLogger<SubsidiaryService>());
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
    public async Task GetSubsidiariesStreamAsync_WithComplianceScheme_ReturnsExpectedStream()
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
        var result = await _sut.GetSubsidiariesStreamAsync(Guid.NewGuid(), Guid.NewGuid(), isComplianceScheme);

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
    public async Task GetSubsidiariesStreamAsync_WithDirectProducer_ReturnsExpectedStream()
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
        var result = await _sut.GetSubsidiariesStreamAsync(Guid.NewGuid(), Guid.NewGuid(), isComplianceScheme);

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
    public async Task GetFileUploadTemplateAsync__WhenCallSuccessful_ReturnsResponse()
    {
        // Arrange
        var expectedDto = new SubsidiaryFileUploadTemplateDto
        {
            Name = "test.csv",
            ContentType = "text/csv",
            Content = new MemoryStream([1])
        };

        _webApiGatewayClientMock.Setup(x => x.GetSubsidiaryFileUploadTemplateAsync()).ReturnsAsync(expectedDto);

        // Act
        var result = await _sut.GetFileUploadTemplateAsync();

        // Assert
        result.Should().Be(expectedDto);
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
    public async Task TerminateSubsidiary_WhenApiReturnsSuccessfully_ReturnsResponse()
    {
        var parentOrganisationExternalId = Guid.NewGuid();
        var childOrganisationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var response = new HttpResponseMessage(HttpStatusCode.OK);

        _accountServiceApiClientMock.Setup(x => x.SendPostRequest(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(response);

        // Act/Assert
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

}
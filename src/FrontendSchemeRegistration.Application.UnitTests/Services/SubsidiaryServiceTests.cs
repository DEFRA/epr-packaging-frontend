namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using Application.Services;
using Application.Services.Interfaces;
using FluentAssertions;
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
    private SubsidiaryService _sut;

    [SetUp]
    public void Init()
    {
        _accountServiceApiClientMock = new Mock<IAccountServiceApiClient>();
        _sut = new SubsidiaryService(_accountServiceApiClientMock.Object, new NullLogger<SubsidiaryService>());
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
        var subsidiaryParentId = 123;
        var isComplianceScheme = true;

        // Act
        var result = await _sut.GetSubsidiariesStreamAsync(subsidiaryParentId, isComplianceScheme);

        // Assert
        result.Should().NotBeNull();
        result.Position.Should().Be(0);

        using (var reader = new StreamReader(result))
        {
            var content = await reader.ReadToEndAsync();
            content.Should().Contain("Organisation_Id,Subsidiary_Id,Organisation_Name,Companies_House_Number");
            content.Should().Contain("100,101,Subsidiary A,CHN001");
        }
    }

    [Test]
    public async Task GetSubsidiariesStreamAsync_WithDirectProducer_ReturnsExpectedStream()
    {
        // Arrange
        var subsidiaryParentId = 123;
        var isComplianceScheme = false;

        // Act
        var result = await _sut.GetSubsidiariesStreamAsync(subsidiaryParentId, isComplianceScheme);

        // Assert
        result.Should().NotBeNull();
        result.Position.Should().Be(0);

        using (var reader = new StreamReader(result))
        {
            var content = await reader.ReadToEndAsync();
            content.Should().Contain("Organisation_Id,Subsidiary_Id,Organisation_Name,Companies_House_Number");
            content.Should().Contain("100,101,Subsidiary A,CHN001");
        }
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
}
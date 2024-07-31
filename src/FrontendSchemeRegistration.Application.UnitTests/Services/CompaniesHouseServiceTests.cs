namespace FrontendSchemeRegistration.Application.UnitTests.Services;

using System.Net;
using Application.Services;
using Application.Services.Interfaces;
using DTOs.CompaniesHouse;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UI.Extensions;

[TestFixture]
public class CompaniesHouseServiceTests
{
    private Mock<IIntegrationServiceApiClient> _integrationServiceApiClientMock;
    private CompaniesHouseService _sut;

    [SetUp]
    public void Init()
    {
        _integrationServiceApiClientMock = new Mock<IIntegrationServiceApiClient>();
        _sut = new CompaniesHouseService(_integrationServiceApiClientMock.Object, new NullLogger<CompaniesHouseService>());
    }

    [Test]
    public async Task GetCompanyByCompaniesHouseNumber_ReturnsAccount()
    {
        // Arrange
        const string companyName = "Test Company";
        var companiesHouseNumber = "0123456X";
        const string buildingNumber = "1";
        const string street = "Main Street";
        const string postcode = "SW1A 1AA";
        const string countryIso = "GB";
        const string countryName = "United Kingdom";

        var company = new CompaniesHouseCompany
        {
            AccountExists = true,
            Organisation = new Organisation
            {
                Name = companyName,
                RegistrationNumber = companiesHouseNumber,
                RegisteredOffice = new RegisteredOfficeAddress
                {
                    BuildingNumber = buildingNumber,
                    Street = street,
                    Postcode = postcode,
                    Country = new Country
                    {
                        Iso = countryIso,
                        Name = countryName
                    }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = company.ToJsonContent();
        _integrationServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
        .ReturnsAsync(response);

        // Act
        var result = await _sut.GetCompanyByCompaniesHouseNumber(companiesHouseNumber);

        // Assert
        result.Should().BeOfType<Company>();
        result.Name.Should().Be(companyName);
        result.CompaniesHouseNumber.Should().Be(companiesHouseNumber);
        result.BusinessAddress.BuildingNumber.Should().Be(buildingNumber);
        result.BusinessAddress.Street.Should().Be(street);
        result.BusinessAddress.Postcode.Should().Be(postcode);
        result.BusinessAddress.Country.Should().Be(countryName);
    }

    [Test]
    public async Task GetCompanyByCompaniesHouseNumber_WhenClientThrowsException_ThrowsException()
    {
        // Arrange
        var companiesHouseNumber = "0123456X";

        _integrationServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        // Act &  Assert
        Func<Task> act = async () => await _sut.GetCompanyByCompaniesHouseNumber(companiesHouseNumber);
        await act.Should().ThrowAsync<Exception>();
    }

    [Test]
    public async Task GetCompanyByCompaniesHouseNumber_WhenNoContent_ReturnsNull()
    {
        // Arrange
        var companiesHouseNumber = "0123456X";

        var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        _integrationServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.GetCompanyByCompaniesHouseNumber(companiesHouseNumber);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetCompanyByCompaniesHouseNumber_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var companiesHouseNumber = "0123456X";

        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        _integrationServiceApiClientMock.Setup(x => x.SendGetRequest(It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act &  Assert
        Func<Task> act = async () => await _sut.GetCompanyByCompaniesHouseNumber(companiesHouseNumber);
        await act.Should().ThrowAsync<Exception>();
    }
}
namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Privacy;

using Application.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using UI.Controllers.Privacy;
using UI.ViewModels.Privacy;

[TestFixture]
public class PrivacyControllerTests
{
    private Mock<HttpContext>? _httpContextMock;
    private Mock<HttpRequest> _httpRequest = null!;

    private Mock<IOptions<ExternalUrlOptions>> _urlOptions = null!;
    private Mock<IOptions<EmailAddressOptions>> _emailOptions = null!;
    private Mock<IOptions<SiteDateOptions>> _siteDateOptions = null!;

    private PrivacyController _systemUnderTest = null!;

    [SetUp]
    public void TestInitialize()
    {
        _httpContextMock = new Mock<HttpContext>();
        _httpRequest = new Mock<HttpRequest>();
        _urlOptions = new Mock<IOptions<ExternalUrlOptions>>();
        _emailOptions = new Mock<IOptions<EmailAddressOptions>>();
        _siteDateOptions = new Mock<IOptions<SiteDateOptions>>();

        SetUpConfigOption();

        _systemUnderTest = new PrivacyController(
            _urlOptions.Object,
            _emailOptions.Object,
            _siteDateOptions.Object);

        _systemUnderTest.ControllerContext.HttpContext = _httpContextMock.Object;
        _httpContextMock.Setup(x => x.Request).Returns(_httpRequest.Object);
    }

    [Test]
    public void Detail_SetsModel()
    {
        // Arrange
        const string returnUrl = "~/home/index";
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(m => m.IsLocalUrl(It.IsAny<string>()))
            .Returns(true)
            .Verifiable();

        _systemUnderTest.Url = mockUrlHelper.Object;

        // Act
        var result = _systemUnderTest!.Detail(returnUrl);
        var viewResult = (ViewResult)result;
        var privacyViewModel = (PrivacyViewModel)viewResult.Model!;
        var expectedDate = _siteDateOptions.Object.Value.PrivacyLastUpdated.ToString(_siteDateOptions.Object.Value.DateFormat);

        // Assert
        result.Should().BeOfType(typeof(ViewResult));
        privacyViewModel.LastUpdated.Should().Be(expectedDate);
        privacyViewModel.DataProtectionEmail.Should().Be(_emailOptions.Object.Value.DataProtection);
        privacyViewModel.DefraGroupProtectionOfficerEmail.Should().Be(_emailOptions.Object.Value.DefraGroupProtectionOfficer);
        privacyViewModel.DataProtectionPublicRegisterUrl.Should().Be(_urlOptions.Object.Value.PrivacyDataProtectionPublicRegister);
        privacyViewModel.DefrasPersonalInformationCharterUrl.Should().Be(_urlOptions.Object.Value.PrivacyDefrasPersonalInformationCharter);
        privacyViewModel.InformationCommissionerUrl.Should().Be(_urlOptions.Object.Value.PrivacyInformationCommissioner);
        privacyViewModel.EnvironmentAgencyUrl.Should().Be(_urlOptions.Object.Value.PrivacyEnvironmentAgency);
        privacyViewModel.NationalResourcesWalesUrl.Should().Be(_urlOptions.Object.Value.PrivacyNationalResourcesWales);
        privacyViewModel.NorthernIrelandEnvironmentAgencyUrl.Should().Be(_urlOptions.Object.Value.PrivacyNorthernIrelandEnvironmentAgency);
        privacyViewModel.ScottishEnvironmentalProtectionAgencyUrl.Should().Be(_urlOptions.Object.Value.PrivacyScottishEnvironmentalProtectionAgency);
    }

    [Test]
    public void Detail_IfUrlNoLocalUrl_SetsModelWithHomePath()
    {
        // Arrange
        const string returnUrl = "~/home/index";
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(m => m.IsLocalUrl(It.IsAny<string>()))
            .Returns(false)
            .Verifiable();

        _systemUnderTest.Url = mockUrlHelper.Object;

        // Act
        var result = _systemUnderTest!.Detail(returnUrl);
        var viewResult = (ViewResult)result;
        var privacyViewModel = (PrivacyViewModel)viewResult.Model!;
        var expectedDate = _siteDateOptions.Object.Value.PrivacyLastUpdated.ToString(_siteDateOptions.Object.Value.DateFormat);

        // Assert
        result.Should().BeOfType(typeof(ViewResult));
        privacyViewModel.LastUpdated.Should().Be(expectedDate);
        privacyViewModel.DataProtectionEmail.Should().Be(_emailOptions.Object.Value.DataProtection);
        privacyViewModel.DefraGroupProtectionOfficerEmail.Should().Be(_emailOptions.Object.Value.DefraGroupProtectionOfficer);
        privacyViewModel.DataProtectionPublicRegisterUrl.Should().Be(_urlOptions.Object.Value.PrivacyDataProtectionPublicRegister);
        privacyViewModel.DefrasPersonalInformationCharterUrl.Should().Be(_urlOptions.Object.Value.PrivacyDefrasPersonalInformationCharter);
        privacyViewModel.InformationCommissionerUrl.Should().Be(_urlOptions.Object.Value.PrivacyInformationCommissioner);
        privacyViewModel.EnvironmentAgencyUrl.Should().Be(_urlOptions.Object.Value.PrivacyEnvironmentAgency);
        privacyViewModel.NationalResourcesWalesUrl.Should().Be(_urlOptions.Object.Value.PrivacyNationalResourcesWales);
        privacyViewModel.NorthernIrelandEnvironmentAgencyUrl.Should().Be(_urlOptions.Object.Value.PrivacyNorthernIrelandEnvironmentAgency);
        privacyViewModel.ScottishEnvironmentalProtectionAgencyUrl.Should().Be(_urlOptions.Object.Value.PrivacyScottishEnvironmentalProtectionAgency);
    }

    private void SetUpConfigOption()
    {
        var externalUrlOptions = new ExternalUrlOptions
        {
            PrivacyDataProtectionPublicRegister = "url2",
            PrivacyDefrasPersonalInformationCharter = "url6",
            PrivacyInformationCommissioner = "url7",
            PrivacyEnvironmentAgency = "url8",
            PrivacyNationalResourcesWales = "url9",
            PrivacyNorthernIrelandEnvironmentAgency = "url10",
            PrivacyScottishEnvironmentalProtectionAgency = "url11"
        };

        var emailAddressOptions = new EmailAddressOptions
        {
            DataProtection = "1@email.com",
            DefraGroupProtectionOfficer = "2@email.com"
        };

        var siteDateOptions = new SiteDateOptions
        {
            PrivacyLastUpdated = DateTime.Parse("2000-01-01"),
            DateFormat = "d MMMM yyyy"
        };

        _urlOptions!
            .Setup(x => x.Value)
            .Returns(externalUrlOptions);

        _emailOptions!
            .Setup(x => x.Value)
            .Returns(emailAddressOptions);

        _siteDateOptions!
            .Setup(x => x.Value)
            .Returns(siteDateOptions);
    }
}

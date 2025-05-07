using EPR.Common.Authorization.Models;
using EPR.Common.Authorization.Sessions;
using FluentAssertions;
using FrontendSchemeRegistration.Application.Constants;
using FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;
using FrontendSchemeRegistration.Application.DTOs.ComplianceScheme;
using FrontendSchemeRegistration.Application.DTOs.Organisation;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.DTOs.Subsidiary.OrganisationSubsidiaryList;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.Sessions;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

public class SubsidiaryCompaniesHouseNumberControllerTests
{
    private const string CompaniesHouseNumber = "07073807";
    private const string CompanyName = "MICROTEC INFORMATION SYSTEMS LTD";
    private const string NewSubsidiaryId = "123456";
    private static readonly string[] TestError = { "Test error" };
    private static readonly string[] ErrorMessage = { "Error message" };

    private Mock<HttpContext> _httpContextMock;
    private FrontendSchemeRegistrationSession _session;

    private Mock<ISessionManager<FrontendSchemeRegistrationSession>> _sessionManagerMock;
    private Mock<ICompaniesHouseService> _companiesHouseServiceMock;
    private Mock<ISubsidiaryService> _subsidirayServiceMock;
    private Mock<ClaimsPrincipal> _claimsPrincipalMock;
    private SubsidiaryCompaniesHouseNumberController _subsidiaryCompaniesHouseNumberController;
    private Mock<IOptions<ExternalUrlOptions>> _urlOptionsMock = null;

    private Mock<IUrlHelper> _urlHelperMock;
    private UserData _userData;
    private Guid _organisationId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private readonly Guid OrganisationId = Guid.NewGuid();
    private readonly Guid UserId = Guid.NewGuid();
    private Mock<ITempDataDictionary> _tempDataDictionaryMock = null!;
    private readonly Mock<ClaimsPrincipal> _userMock = new();

    private List<Claim> CreateUserDataClaim(string organisationRole, string serviceRole = null)
    {
        var userData = new UserData
        {
            Organisations = new List<EPR.Common.Authorization.Models.Organisation>
            {
                new()
                    {
                        Id = OrganisationId,
                        OrganisationRole = organisationRole,
                        Name = "Test Name",
                        OrganisationNumber = "Test Number"
                    }
                },
            Id = UserId,
            ServiceRole = serviceRole
        };

        return new List<Claim>
            {
                new(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
            };
    }

    [SetUp]
    public void SetUp()
    {
        _urlOptionsMock = new Mock<IOptions<ExternalUrlOptions>>();
        _httpContextMock = new Mock<HttpContext>();

        SetUpConfigOption();

        _urlHelperMock = new Mock<IUrlHelper>();
        _urlHelperMock.Setup(url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Get")))
            .Returns(PagePaths.FileUpload);
        _urlHelperMock.Setup(x => x.Content(It.IsAny<string>())).Returns((string contentPath) => contentPath);

        var claims = CreateUserDataClaim(OrganisationRoles.Producer);
        _userMock.Setup(x => x.Claims).Returns(claims);

        _claimsPrincipalMock = new Mock<ClaimsPrincipal>();
        _claimsPrincipalMock.Setup(x => x.Claims).Returns(claims);

        _session = new FrontendSchemeRegistrationSession
        {
            SubsidiarySession = new()
            {
                Company = new Company
                {
                    CompaniesHouseNumber = CompaniesHouseNumber,
                    Name = CompanyName
                }
            },
            RegistrationSession = new()
            {
                Journey = new List<string> { PagePaths.FileUploadSubLanding },
                SelectedComplianceScheme = new ComplianceSchemeDto { Id = Guid.NewGuid() }
            },
            UserData = new UserData
            {
                Organisations = new List<EPR.Common.Authorization.Models.Organisation>
            {
                new()
                    {
                        Id = OrganisationId,
                        OrganisationRole = OrganisationRoles.Producer,
                        Name = "MICROTEC INFORMATION SYSTEMS LTD",
                        CompaniesHouseNumber = "07073807",
                        OrganisationNumber = "127516"
                    }
                },
                Id = UserId,
                ServiceRole = null
            }
        };

        _sessionManagerMock = new Mock<ISessionManager<FrontendSchemeRegistrationSession>>();
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(_session);
        _sessionManagerMock.Setup(x => x.UpdateSessionAsync(
            It.IsAny<ISession>(), It.IsAny<Action<FrontendSchemeRegistrationSession>>()))
            .Callback<ISession, Action<FrontendSchemeRegistrationSession>>((_, action) => action.Invoke(_session));

        _companiesHouseServiceMock = new Mock<ICompaniesHouseService>();
        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(CompaniesHouseNumber))
            .ReturnsAsync(new Company());

        _subsidirayServiceMock = new Mock<ISubsidiaryService>();
        var sddf = Guid.NewGuid();

        // Arrange
        var orgModel = new OrganisationRelationshipModel
        {
            Organisation = new OrganisationDetailModel
            {
                Id = Guid.NewGuid(),
                Name = "MICROTEC INFORMATION SYSTEMS LTD",
                OrganisationNumber = "127516"
            },
            Relationships = new List<RelationshipResponseModel>
                {
                    new() { CompaniesHouseNumber ="14796704", OrganisationNumber = "127517", OrganisationName = "BBB ENTERPRISE LTD" , JoinerDate = null},
                    new() { CompaniesHouseNumber ="232147930", OrganisationNumber = "852147930", OrganisationName = "Subsidiary2" , JoinerDate = null},
                    new() { CompaniesHouseNumber ="421229428", OrganisationNumber = "741229428", OrganisationName = "Subsidiary3" , JoinerDate = null},
                }
        };
        _subsidirayServiceMock.Setup(s => s.GetOrganisationSubsidiaries(It.IsAny<Guid>())).ReturnsAsync(orgModel);

        _userData = new UserData
        {
            Email = "test@test.com",
            Organisations = new List<EPR.Common.Authorization.Models.Organisation> { new() { Id = _organisationId } }
        };

        _httpContextMock.Setup(x => x.Session).Returns(Mock.Of<ISession>());

        _tempDataDictionaryMock = new Mock<ITempDataDictionary>();

        _subsidiaryCompaniesHouseNumberController = new SubsidiaryCompaniesHouseNumberController(
            _urlOptionsMock.Object,
            _companiesHouseServiceMock.Object,
            _sessionManagerMock.Object,
            _subsidirayServiceMock.Object,
            new NullLogger<SubsidiaryCompaniesHouseNumberController>());
        _subsidiaryCompaniesHouseNumberController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = _claimsPrincipalMock.Object,
                Session = new Mock<ISession>().Object,
            }
        };

        _subsidiaryCompaniesHouseNumberController.Url = _urlHelperMock.Object;
    }

    [Test]
    public async Task Get_SubsidiaryCompaniesHouseNumberSearch()
    {
        SetupTempData(new Dictionary<string, object>
        {
            { "ModelState", "{\"Errors\":[\"one\",\"two\"]}" },
            { "CompaniesHouseNumber", CompaniesHouseNumber }
        });

        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        result.ViewName.Should().Be("SubsidiaryCompaniesHouseNumber");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubsidiaries}");

        var viewModel = result.Model as SubsidiaryCompaniesHouseNumberViewModel;
        viewModel.CompaniesHouseNumber.Should().Be(CompaniesHouseNumber);
    }

    [Test]
    public async Task Get_When_IsUserChangingDetails_Should_Set_BackLink_To_CheckYourDetails()
    {
        _session.SubsidiarySession.IsUserChangingDetails = true;
        _session.SubsidiarySession.Journey = new List<string> { PagePaths.SubsidiaryCompaniesHouseNumberSearch };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(_session);

        SetupTempData();

        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        result.ViewData["BackLinkToDisplay"].Should().Be(PagePaths.SubsidiaryCheckYourDetails);
    }

    [Test]
    public async Task Get_When_IsUserChangingDetails_Should_Set_BackLink_To_CheckYourDetails_IsUserChangingDetails_false()
    {
        _session.SubsidiarySession.IsUserChangingDetails = false;
        _session.SubsidiarySession.Journey = new List<string> { PagePaths.SubsidiaryCompaniesHouseNumberSearch };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(_session);

        SetupTempData();

        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        result.ViewData["BackLinkToDisplay"].Should().Be("~/subsidiaries-list");
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch()
    {
        // Arrange
        var subsSession = _session.SubsidiarySession;
        var companiesHouseNumber = "14796704";
        var companyName = "BBB ENTERPRISE LTD";

        var subCompany = new Company
        {
            CompaniesHouseNumber = companiesHouseNumber,
            Name = companyName,
            OrganisationId = "127516"
        };

        var parentExpectedOrganisationDto = new OrganisationDto
        {
            CompaniesHouseNumber = CompaniesHouseNumber,
            Id = 28687,
            ExternalId = new Guid("E219D934-F0AC-4DBE-AB4F-BF9E5BC56E21"),
            Name = CompanyName,
            RegistrationNumber = "127516",
            TradingName = "DEF"
        };

        var subsidiaryResponse = new OrganisationRelationshipModel()
        {
            Relationships = new List<RelationshipResponseModel> { new RelationshipResponseModel(), new RelationshipResponseModel() } // count = 2
        };

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(companiesHouseNumber))
            .ReturnsAsync(subCompany);

        _subsidirayServiceMock.Setup(s => s.GetOrganisationByReferenceNumber(parentExpectedOrganisationDto.RegistrationNumber))
            .ReturnsAsync(parentExpectedOrganisationDto);

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = companiesHouseNumber }) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryConfirmCompanyDetails");
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch_When_HasError()
    {
        // Arrange
        _subsidiaryCompaniesHouseNumberController.ModelState.AddModelError("file", "error");

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("SubsidiaryCompaniesHouseNumber");
        result.ViewData.Keys.Should().Contain("BackLinkToDisplay");
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubsidiaries}");
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch_With_No_Relationships()
    {
        // Arrange
        var subsSession = _session.SubsidiarySession;
        var companiesHouseNumber = "14796704";
        var companyName = "BBB ENTERPRISE LTD";

        var subCompany = new Company
        {
            CompaniesHouseNumber = companiesHouseNumber,
            Name = companyName,
            OrganisationId = "127516"
        };

        var parentExpectedOrganisationDto = new OrganisationDto
        {
            CompaniesHouseNumber = CompaniesHouseNumber,
            Id = 28687,
            ExternalId = new Guid("E219D934-F0AC-4DBE-AB4F-BF9E5BC56E21"),
            Name = CompanyName,
            RegistrationNumber = "127516",
            TradingName = "DEF"
        };

        var subsidiaryResponse = new OrganisationRelationshipModel()
        {
            Relationships = new List<RelationshipResponseModel> { new RelationshipResponseModel(), new RelationshipResponseModel() }
        };

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(companiesHouseNumber))
            .ReturnsAsync(subCompany);

        _subsidirayServiceMock.Setup(s => s.GetOrganisationByReferenceNumber(parentExpectedOrganisationDto.RegistrationNumber))
            .ReturnsAsync(parentExpectedOrganisationDto);

        var orgModel = new OrganisationRelationshipModel
        {
            Organisation = new OrganisationDetailModel
            {
                Id = Guid.NewGuid(),
                Name = "MICROTEC INFORMATION SYSTEMS LTD",
                OrganisationNumber = "127516"
            },
            Relationships = null
        };

        _subsidirayServiceMock.Setup(r => r.GetOrganisationSubsidiaries(It.IsAny<Guid>())).ReturnsAsync((OrganisationRelationshipModel)null);

        _subsidirayServiceMock.Setup(s => s.GetOrganisationByReferenceNumber(parentExpectedOrganisationDto.RegistrationNumber))
            .ReturnsAsync(parentExpectedOrganisationDto);

        _subsidiaryCompaniesHouseNumberController.Url = _urlHelperMock.Object;

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = companiesHouseNumber }) as RedirectToActionResult;

        // Assert
        result?.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryConfirmCompanyDetails");
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch_With_CompaniesHouse_Not_Found()
    {
        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ReturnsAsync(default(Company));
        SetupTempData();

        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = CompaniesHouseNumber }) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(SubsidiaryCompaniesHouseNumberController.Get));
    }

    [Test]
    public async Task Post_SubsidiaryCompaniesHouseNumberSearch_With_CompaniesHouse_Exception()
    {
        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(It.IsAny<string>())).ThrowsAsync(new Exception());
        SetupTempData();

        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = CompaniesHouseNumber }) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(CannotVerifyOrganisationController.Get));
        result.ControllerName.Should().Be("CannotVerifyOrganisation");
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<FrontendSchemeRegistrationSession>()), Times.Once);
    }

    [Test]
    public async Task Get_ShouldMergeModelState_WhenTempDataExists()
    {
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string[]>
        {
            { nameof(SubsidiaryCompaniesHouseNumberViewModel.CompaniesHouseNumber), TestError }
        });

        SetupTempData(new Dictionary<string, object>
        {
            { "ModelState", serialized },
            { "CompaniesHouseNumber", CompaniesHouseNumber }
        });

        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        var model = (SubsidiaryCompaniesHouseNumberViewModel)result.Model;
        model.CompaniesHouseNumber.Should().Be(CompaniesHouseNumber);
        result.ViewData.ModelState.ContainsKey(nameof(model.CompaniesHouseNumber)).Should().BeTrue();
    }

    [Test]
    public async Task Post_ShouldSetCompanyAsLinked_WhenAlreadyInParentList()
    {
        // Arrange
        var linkedCHNumber = "232147930";
        var viewModel = new SubsidiaryCompaniesHouseNumberViewModel
        {
            CompaniesHouseNumber = linkedCHNumber
        };

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(linkedCHNumber))
            .ReturnsAsync(new Company { CompaniesHouseNumber = linkedCHNumber, Name = "Subsidiary2" });

        _subsidirayServiceMock.Setup(s => s.GetOrganisationByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(new OrganisationDto { ExternalId = Guid.NewGuid(), Name = "Parent Co" });

        _subsidirayServiceMock.Setup(s => s.GetOrganisationSubsidiaries(It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationRelationshipModel
            {
                Relationships = new List<RelationshipResponseModel>
                {
                new() { CompaniesHouseNumber = linkedCHNumber }
                }
            });

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(viewModel) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be("Get");
        result.ControllerName.Should().Be("SubsidiaryConfirmCompanyDetails");
        _session.SubsidiarySession.Company.IsCompanyAlreadyLinkedToTheParent.Should().BeTrue();
    }

    [Test]
    public async Task Post_ShouldSetCompanyAsNotLinked_WhenNotInParentList()
    {
        // Arrange
        var unlinkedCHNumber = "99999999";
        var viewModel = new SubsidiaryCompaniesHouseNumberViewModel
        {
            CompaniesHouseNumber = unlinkedCHNumber
        };

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(unlinkedCHNumber))
            .ReturnsAsync(new Company { CompaniesHouseNumber = unlinkedCHNumber, Name = "Unlinked Co" });

        _subsidirayServiceMock.Setup(s => s.GetOrganisationByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(new OrganisationDto { ExternalId = Guid.NewGuid(), Name = "Parent Co" });

        _subsidirayServiceMock.Setup(s => s.GetOrganisationSubsidiaries(It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationRelationshipModel
            {
                Relationships = new List<RelationshipResponseModel>
                {
                new() { CompaniesHouseNumber = "12345678" }
                }
            });

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(viewModel) as RedirectToActionResult;

        // Assert
        _session.SubsidiarySession.Company.IsCompanyAlreadyLinkedToTheParent.Should().BeFalse();
    }

    [Test]
    public async Task Get_Should_Populate_ViewModel_With_CompaniesHouseNumber_When_Session_Has_Company()
    {
        // Arrange
        var controller = new SubsidiaryCompaniesHouseNumberController(
            _urlOptionsMock.Object,
            _companiesHouseServiceMock.Object,
            _sessionManagerMock.Object,
            _subsidirayServiceMock.Object,
            new NullLogger<SubsidiaryCompaniesHouseNumberController>());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Session = new Mock<ISession>().Object
            }
        };

        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>()
        );

        controller.Url = _urlHelperMock.Object;

        // Act
        var result = await controller.Get() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result.ViewName.Should().Be("SubsidiaryCompaniesHouseNumber");
        result.Model.Should().BeOfType<SubsidiaryCompaniesHouseNumberViewModel>();

        var viewModel = result.Model as SubsidiaryCompaniesHouseNumberViewModel;
        viewModel.CompaniesHouseNumber.Should().Be(CompaniesHouseNumber);
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubsidiaries}");
    }

    [Test]
    public async Task Get_Should_Merge_ModelState_And_Set_CompaniesHouseNumber_From_TempData()
    {
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string[]> { { "CompaniesHouseNumber", ErrorMessage } });

        SetupTempData(new Dictionary<string, object>
        {
            { "ModelState", serialized },
            { "CompaniesHouseNumber", "TEMP123" }
        });

        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        var viewModel = result.Model as SubsidiaryCompaniesHouseNumberViewModel;
        viewModel.CompaniesHouseNumber.Should().Be("TEMP123");
        result.ViewData.ModelState.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task Post_Should_Initialize_SubsidiarySession_If_Null()
    {
        // Arrange
        _session.SubsidiarySession = null;

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync(new Company());
        _subsidirayServiceMock.Setup(x => x.GetOrganisationByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(new OrganisationDto { ExternalId = Guid.NewGuid(), Name = "ParentCo" });
        _subsidirayServiceMock.Setup(x => x.GetOrganisationSubsidiaries(It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationRelationshipModel { Relationships = new List<RelationshipResponseModel>() });

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = "14796704" });

        // Assert
        _session.SubsidiarySession.Should().NotBeNull();
    }

    [Test]
    public async Task Post_Should_Trim_CompaniesHouseNumber()
    {
        // Arrange
        var model = new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = "  14796704  " };

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber("14796704"))
            .ReturnsAsync(new Company());
        _subsidirayServiceMock.Setup(x => x.GetOrganisationByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(new OrganisationDto { ExternalId = Guid.NewGuid() });
        _subsidirayServiceMock.Setup(x => x.GetOrganisationSubsidiaries(It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationRelationshipModel());

        // Act
        await _subsidiaryCompaniesHouseNumberController.Post(model);

        // Assert
        model.CompaniesHouseNumber.Should().Be("14796704");
    }

    [Test]
    public async Task Post_Should_Set_IsCompanyAlreadyLinked_True_When_Match_Found()
    {
        // Arrange
        var model = new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = "123456" };

        var company = new Company { CompaniesHouseNumber = "123456" };
        var parent = new OrganisationDto { ExternalId = Guid.NewGuid(), Name = "Parent Ltd" };
        var relationships = new List<RelationshipResponseModel> {
            new RelationshipResponseModel { CompaniesHouseNumber = "123456" }
        };

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber("123456"))
            .ReturnsAsync(company);
        _subsidirayServiceMock.Setup(x => x.GetOrganisationByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(parent);
        _subsidirayServiceMock.Setup(x => x.GetOrganisationSubsidiaries(It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationRelationshipModel { Relationships = relationships });

        // Act
        await _subsidiaryCompaniesHouseNumberController.Post(model);

        // Assert
        _session.SubsidiarySession.Company.IsCompanyAlreadyLinkedToTheParent.Should().BeTrue();
        _session.SubsidiarySession.Company.ParentCompanyName.Should().Be("Parent Ltd");
    }

    [Test]
    public async Task Post_Should_Set_IsCompanyAlreadyLinkedToOtherParent_false_When_Match_Found()
    {
        // Arrange
        var model = new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = "123456" };

        var company = new Company { CompaniesHouseNumber = "123456" };
        var parent = new OrganisationDto { ExternalId = Guid.NewGuid(), Name = "Parent Ltd" };
        var relationships = new List<RelationshipResponseModel> {
            new RelationshipResponseModel { CompaniesHouseNumber = "123456" }
        };

        var parentExpectedOrganisationDto = new OrganisationDto
        {
            CompaniesHouseNumber = CompaniesHouseNumber,
            Id = 28687,
            ExternalId = new Guid("E219D934-F0AC-4DBE-AB4F-BF9E5BC56E21"),
            Name = CompanyName,
            RegistrationNumber = "127516",
            TradingName = "DEF"
        };

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber("123456"))
            .ReturnsAsync(company);
        _subsidirayServiceMock.Setup(x => x.GetOrganisationByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(parent);
        _subsidirayServiceMock.Setup(x => x.GetOrganisationSubsidiaries(It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationRelationshipModel { Relationships = relationships });
        _subsidirayServiceMock.Setup(x => x.GetOrganisationsByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync(parentExpectedOrganisationDto);

        // Act
        await _subsidiaryCompaniesHouseNumberController.Post(model);

        // Assert
        _session.SubsidiarySession.Company.IsCompanyAlreadyLinkedToTheParent.Should().BeTrue();
        _session.SubsidiarySession.Company.IsCompanyAlreadyLinkedToOtherParent.Should().BeFalse();
        _session.SubsidiarySession.Company.ParentCompanyName.Should().Be("Parent Ltd");
    }

    [Test]
    public async Task Post_Should_Set_IsCompanyAlreadyLinkedToOtherParent_True_When_Match_Found()
    {
        // Arrange
        var model = new SubsidiaryCompaniesHouseNumberViewModel { CompaniesHouseNumber = "123456" };
        var company = new Company { CompaniesHouseNumber = "12345678", Name = "newCompany" };
        var thisParent = new OrganisationDto { CompaniesHouseNumber = "T1234567", ExternalId = Guid.NewGuid(), Name = "this Parent Ltd" };
        var otherParent = new OrganisationDto { CompaniesHouseNumber="O1234567", ExternalId = Guid.NewGuid(), Name = "Other Parent Ltd" };
        var relationships = new List<RelationshipResponseModel>();
        var childExpectedOrganisationDto = new OrganisationDto
        {
            CompaniesHouseNumber = company.CompaniesHouseNumber,
            Id = 28687,
            ExternalId = new Guid("E219D934-F0AC-4DBE-AB4F-BF9E5BC56E21"),
            Name = company.Name,
            RegistrationNumber = "123456",
            TradingName = "DEF",
            ParentCompanyName = "Other Parent Ltd"
        };

        _companiesHouseServiceMock.Setup(x => x.GetCompanyByCompaniesHouseNumber("123456"))
            .ReturnsAsync(company);
        _subsidirayServiceMock.Setup(x => x.GetOrganisationByReferenceNumber(It.IsAny<string>()))
            .ReturnsAsync(thisParent);
        _subsidirayServiceMock.Setup(x => x.GetOrganisationSubsidiaries(It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationRelationshipModel { Relationships = relationships });
        _subsidirayServiceMock.Setup(x => x.GetOrganisationsByCompaniesHouseNumber(It.IsAny<string>()))
            .ReturnsAsync(childExpectedOrganisationDto);

        // Act
        await _subsidiaryCompaniesHouseNumberController.Post(model);

        // Assert
        _session.SubsidiarySession.Company.IsCompanyAlreadyLinkedToTheParent.Should().BeFalse();
        _session.SubsidiarySession.Company.IsCompanyAlreadyLinkedToOtherParent.Should().BeTrue();
        _session.SubsidiarySession.Company.OtherParentCompanyName.Should().Be("Other Parent Ltd");
    }

    [Test]
    public void SerializeModelState_Should_Return_Serialized_String()
    {
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        modelState.AddModelError("Field", "Error message");

        var result = typeof(SubsidiaryCompaniesHouseNumberController)
            .GetMethod("SerializeModelState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { modelState });

        result.Should().BeOfType<string>();
        result.ToString().Should().Contain("Field");
    }

    [Test]
    public void DeserializeModelState_Should_Return_ModelStateDictionary()
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(new Dictionary<string, string[]> { { "Field", ErrorMessage } });

        var result = typeof(SubsidiaryCompaniesHouseNumberController)
            .GetMethod("DeserializeModelState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Invoke(null, new object[] { json }) as Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary;

        result.Should().NotBeNull();
        result.ContainsKey("Field").Should().BeTrue();
        result["Field"].Errors[0].ErrorMessage.Should().Be("Error message");
    }

    [Test]
    public async Task Post_ShouldHandle_NullCompany_AndSetTempDataAndModelError()
    {
        // Arrange
        var viewModel = new SubsidiaryCompaniesHouseNumberViewModel
        {
            CompaniesHouseNumber = CompaniesHouseNumber
        };

        _subsidirayServiceMock
        .Setup(s => s.GetOrganisationByReferenceNumber(It.IsAny<string>()))
        .ReturnsAsync(new OrganisationDto
        {
            ExternalId = Guid.NewGuid(),
            Name = "Test Parent"
        });

        _subsidirayServiceMock
            .Setup(s => s.GetOrganisationSubsidiaries(It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationRelationshipModel
            {
                Relationships = new List<RelationshipResponseModel>()
            });


        _companiesHouseServiceMock
            .Setup(x => x.GetCompanyByCompaniesHouseNumber(CompaniesHouseNumber))
            .ReturnsAsync((Company)null);

        var tempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );

        _subsidiaryCompaniesHouseNumberController.TempData = tempData;

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Post(viewModel) as RedirectToActionResult;

        result.Should().NotBeNull();
        result.ActionName.Should().Be(nameof(SubsidiaryCompaniesHouseNumberController.Get));

        tempData["CompaniesHouseNumber"].Should().Be(CompaniesHouseNumber);
        tempData["ModelState"].Should().NotBeNull();

        var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string[]>>(tempData["ModelState"].ToString());
        deserialized.Should().ContainKey(nameof(SubsidiaryCompaniesHouseNumberViewModel.CompaniesHouseNumber));
        deserialized[nameof(SubsidiaryCompaniesHouseNumberViewModel.CompaniesHouseNumber)]
            .First().Should().Be("CompaniesHouseNumber.NotFoundError");
    }

    [Test]
    public async Task Get_Should_Handle_Null_SubsidiarySession_And_Company()
    {
        // Arrange
        _session.SubsidiarySession = null;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(_session);
        SetupTempData(); // even if empty, sets it up for consistency

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        var viewModel = result.Model as SubsidiaryCompaniesHouseNumberViewModel;
        viewModel.CompaniesHouseNumber.Should().BeNull();
    }

    [Test]
    public async Task Get_Should_Fallback_BackLink_When_Journey_IsNull_Or_PageNotFound()
    {
        // Arrange
        _session.SubsidiarySession.Journey = null;
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(_session);
        SetupTempData();

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        // Assert
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubsidiaries}");
    }

    [Test]
    public async Task Get_Should_Set_BackLink_To_CheckYourDetails_When_UserChangingDetails_And_NotCurrentPage()
    {
        // Arrange
        _session.SubsidiarySession.IsUserChangingDetails = true;
        _session.SubsidiarySession.Journey = new List<string> { "SomeOtherPage" };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync(_session);
        SetupTempData();

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        // Assert
        result.ViewData["BackLinkToDisplay"].Should().Be(PagePaths.SubsidiaryCheckYourDetails);
    }

    [Test]
    public async Task Get_Should_Handle_Null_Journey_Without_Exception()
    {
        // Arrange
        _session.SubsidiarySession = new SubsidiarySession
        {
            Journey = null,
            Company = new Company { CompaniesHouseNumber = CompaniesHouseNumber }
        };

        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(_session);
        SetupTempData();

        // Act
        var result = await _subsidiaryCompaniesHouseNumberController.Get() as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result.ViewData["BackLinkToDisplay"].Should().Be($"~{PagePaths.FileUploadSubsidiaries}");
    }



    private void SetupTempData(Dictionary<string, object> values = null)
    {
        var tempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());

        values ??= new Dictionary<string, object>();

        foreach (var kvp in values)
        {
            tempData[kvp.Key] = kvp.Value;
        }

        _subsidiaryCompaniesHouseNumberController.TempData = tempData;
    }

    private void SetupTempDataWithModelError(string chNumber, string errorKey, string errorMessage)
    {
        var modelState = new Dictionary<string, string[]>
        {
            { errorKey, new[] { errorMessage } }
        };

        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(modelState);

        SetupTempData(new Dictionary<string, object>
        {
            ["CompaniesHouseNumber"] = chNumber,
            ["ModelState"] = serialized
        });
    }

    private void SetUpConfigOption()
    {
        var externalUrlOptions = new ExternalUrlOptions
        {
            FindAndUpdateCompanyInformation = "https://find-and-update.company-information.service.gov.uk",
            PrivacyDataProtectionPublicRegister = "url2",
            PrivacyDefrasPersonalInformationCharter = "url6",
            PrivacyInformationCommissioner = "url7",
            PrivacyEnvironmentAgency = "url8",
            PrivacyNationalResourcesWales = "url9",
            PrivacyNorthernIrelandEnvironmentAgency = "url10",
            PrivacyScottishEnvironmentalProtectionAgency = "url11"
        };

        _urlOptionsMock!
        .Setup(x => x.Value)
        .Returns(externalUrlOptions);
    }
}

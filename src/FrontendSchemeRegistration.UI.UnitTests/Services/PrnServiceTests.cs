using AutoFixture;
using FluentAssertions;
using FrontendSchemeRegistration.Application.DTOs;
using FrontendSchemeRegistration.Application.DTOs.Prns;
using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Options;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Globalization;
using System.Text;

namespace FrontendSchemeRegistration.UI.UnitTests.Services;

[TestFixture]
public class PrnServiceTests
{
    private Mock<IWebApiGatewayClient> _webApiGatewayClientMock;
    private PrnService _systemUnderTest;
    private static readonly IFixture Fixture = new Fixture();
    private Mock<ILogger<PrnService>> _loggerMock;

    [SetUp]
    public void SetUp()
    {
        // ResourcesPath should be 'Resources' but build environment differs from development environment
        // Work around = set ResourcesPath to non-existent location and test for resource keys rather than resource values
        var options = Options.Create(new LocalizationOptions { ResourcesPath = "Resources_not_found" });
        var factory = new ResourceManagerStringLocalizerFactory(options, NullLoggerFactory.Instance);
        var localizerCsv = new StringLocalizer<PrnCsvResources>(factory);
        var localizerData = new StringLocalizer<PrnDataResources>(factory);

        _webApiGatewayClientMock = new Mock<IWebApiGatewayClient>();
        _loggerMock = new Mock<ILogger<PrnService>>();

        var globalVariables = Options.Create(new GlobalVariables { LogPrefix = "[FrontendSchemaRegistration]" });

        _systemUnderTest = new PrnService(_webApiGatewayClientMock.Object, localizerCsv, localizerData, globalVariables, _loggerMock.Object);
    }

    [Test]
    public async Task GetAllPrnsAsync_ReturnsListOfPrnViewModels()
    {
        var data = Fixture.CreateMany<PrnModel>().ToList();
        data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";
        _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
        var model = await _systemUnderTest.GetAllPrnsAsync();
        model.Should().NotBeNull();
    }

    [Test]
    public async Task GetPrnsAwaitingAcceptanceAsync_ReturnsListOfPrnViewModels()
    {
        var data = Fixture.CreateMany<PrnModel>().ToList();
        data[0].PrnStatus = "AWAITING ACCEPTANCE";
        data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";
        _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
        var model = await _systemUnderTest.GetPrnsAwaitingAcceptanceAsync();
        model.Should().NotBeNull();
    }

    [Test]
    public async Task GetAllAcceptedPrnsAsync_ReturnsListOfAcceptedPrnViewModels()
    {
        var data = Fixture.CreateMany<PrnModel>().ToList();
        data[0].PrnStatus = data[1].PrnStatus = "ACCEPTED";
        data[0].AccreditationYear = data[1].AccreditationYear = data[2].AccreditationYear = "2024";

        _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);
        var model = await _systemUnderTest.GetAllAcceptedPrnsAsync();
        model.Prns.Should().HaveCount(2);
        model.Prns.Should().AllSatisfy(x => x.ApprovalStatus.Should().Be("ACCEPTED"));
    }

    [Test]
    public async Task AcceptPrnsAsync_CallsWebApiClientWIthCorrectParams()
    {
        var ids = Fixture.CreateMany<Guid>().ToArray();
        _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToAcceptedAsync(ids)).Returns(Task.CompletedTask);
        await _systemUnderTest.AcceptPrnsAsync(ids);
        _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToAcceptedAsync(ids), Times.Once);
    }

    [Test]
    public async Task AcceptPrnAsync_CallsWebApiClientWIthCorrectParams()
    {
        var id = Guid.NewGuid();
        _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToAcceptedAsync(id)).Returns(Task.CompletedTask);
        await _systemUnderTest.AcceptPrnAsync(id);
        _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToAcceptedAsync(id), Times.Once);
    }

    [Test]
    public async Task GetPrnByExternalIdAsync_ReturnsPrn()
    {
        var model = Fixture.Create<PrnModel>();
        model.AccreditationYear = "2024";
        _webApiGatewayClientMock.Setup(x => x.GetPrnByExternalIdAsync(model.ExternalId)).ReturnsAsync(model);
        var result = await _systemUnderTest.GetPrnByExternalIdAsync(model.ExternalId);
        _webApiGatewayClientMock.Verify(x => x.GetPrnByExternalIdAsync(model.ExternalId), Times.Once);
        result.Should().BeEquivalentTo((PrnViewModel)model);
    }

    [Test]
    public async Task RejectPrnAsync_CallsWebApiClientWIthCorrectParams()
    {
        var id = Guid.NewGuid();
        _webApiGatewayClientMock.Setup(x => x.SetPrnApprovalStatusToRejectedAsync(id)).Returns(Task.CompletedTask);
        await _systemUnderTest.RejectPrnAsync(id);
        _webApiGatewayClientMock.Verify(x => x.SetPrnApprovalStatusToRejectedAsync(id), Times.Once);
    }

    [Test]
    public async Task GetPrnSearchResultsAsync_ReturnsMatchingPrns()
    {
        SearchPrnsViewModel request = new SearchPrnsViewModel { PageSize = 5, Search = "search me" };
        var pageOne = new List<PrnModel>();
        for (int i = 0; i < request.PageSize; i++)
        {
            pageOne.Add(Fixture.Create<PrnModel>());
        }
        var pageTwo = new List<PrnModel> { Fixture.Create<PrnModel>() };
        PaginatedResponse<PrnModel> paginatedResposne = new PaginatedResponse<PrnModel>();

        paginatedResposne.SearchTerm = request.Search;
        paginatedResposne.Items = pageTwo;
        paginatedResposne.CurrentPage = 2;
        paginatedResposne.TotalItems = pageTwo.Count + pageOne.Count;
        paginatedResposne.PageSize = request.PageSize;
        paginatedResposne.TypeAhead = new List<string> { "prn number", "issued by" };

        _webApiGatewayClientMock.Setup(x => x.GetSearchPrnsAsync(It.IsAny<PaginatedRequest>())).ReturnsAsync(paginatedResposne);

        // Act
        var prnSearchResults = await _systemUnderTest.GetPrnSearchResultsAsync(request);

        // Assert
        prnSearchResults.SearchString.Should().Be(request.Search);
        prnSearchResults.ActivePageOfResults.Count.Should().Be(pageTwo.Count);
        prnSearchResults.PagingDetail.CurrentPage.Should().Be(paginatedResposne.CurrentPage);
        prnSearchResults.PagingDetail.PageSize.Should().Be(request.PageSize);
        prnSearchResults.PagingDetail.TotalItems.Should().Be(paginatedResposne.TotalItems);
        prnSearchResults.TypeAhead.Should().BeEquivalentTo(paginatedResposne.TypeAhead);
    }

    [Test]
    public async Task GetPrnAwaitingAcceptanceSearchResultsAsync__ReturnsMatchingPrns()
    {
        var request = Fixture.Create<SearchPrnsViewModel>();
        var response = Fixture.Create<PaginatedResponse<PrnModel>>();
        _webApiGatewayClientMock.Setup(x => x.GetSearchPrnsAsync(It.IsAny<PaginatedRequest>())).ReturnsAsync(response);

        var awaitngPrns = await _systemUnderTest.GetPrnAwaitingAcceptanceSearchResultsAsync(request);

        awaitngPrns.Prns.Count.Should().Be(response.Items.Count);
        awaitngPrns.PagingDetail.CurrentPage.Should().Be(response.CurrentPage);
        awaitngPrns.PagingDetail.PageSize.Should().Be(request.PageSize);
        awaitngPrns.PagingDetail.TotalItems.Should().Be(response.TotalItems);
    }

    [Test]
    public async Task GetPrnsCsvStreamAsync_WhenZeroPrns_ReturnsEmpty()
    {
        var data = new List<PrnModel>();

        _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync(data);

        var result = await _systemUnderTest.GetPrnsCsvStreamAsync();

        using (var reader = new StreamReader(result))
        {
            var actual = reader.ReadLine();
            actual.Should().BeNull();
        }
    }

    [Test]
    public async Task GetPrnsCsvStreamAsync_ReturnsHeaderRowAsCsvStream()
    {
        var data = Fixture.Create<PrnModel>();

        StringBuilder sb = new StringBuilder();
        sb.Append("column_header_prn_or_pern_number").Append(',');
        sb.Append("column_header_prn_or_pern").Append(',');
        sb.Append("column_header_status").Append(',');
        sb.Append("column_header_issued_by").Append(',');
        sb.Append("column_header_issued_to").Append(',');
        sb.Append("column_header_accreditation_number").Append(',');
        sb.Append("column_header_date_issued").Append(',');
        sb.Append("column_header_december_waste").Append(',');
        sb.Append("column_header_material").Append(',');
        sb.Append("column_header_recycling_process").Append(',');
        sb.Append("column_header_tonnes").Append(',');
        sb.Append("column_header_date_accepted").Append(',');
        sb.Append("column_header_date_cancelled").Append(',');
        sb.Append("column_header_issuer_note");
        var expectedHeaderCsv = sb.ToString();

        _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync([data]);

        var result = await _systemUnderTest.GetPrnsCsvStreamAsync();

        using (var reader = new StreamReader(result))
        {
            var actual = await reader.ReadLineAsync();
            actual.Should().Be(expectedHeaderCsv);
        }
    }

    [Test]
    [TestCase("AWAITING ACCEPTANCE", "AWAITING ACCEPTANCE", "not_accepted", "not_cancelled")]
    [TestCase("ACCEPTED", "ACCEPTED", "01/12/2024", "not_cancelled")]
    [TestCase("CANCELLED", "CANCELLED", "not_accepted", "01/12/2024")]
    [TestCase("REJECTED", "REJECTED", "not_accepted", "not_cancelled")]
    public async Task GetPrnsCsvStreamAsync_ReturnsPrnsAsCsvStream(string status, string displayStatus, string whenAccepted, string whenCancelled)
    {
        var data = Fixture.Create<PrnModel>();
        data.IsExport = true;
        data.DecemberWaste = true;
        data.PrnStatus = status;

        if (DateTime.TryParseExact(whenAccepted,
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime dtAccepted))
        {
            data.StatusUpdatedOn = dtAccepted;
        }

        if (DateTime.TryParseExact(whenCancelled,
           "dd/MM/yyyy",
           CultureInfo.InvariantCulture,
           DateTimeStyles.None,
           out DateTime dtCancelled))
        {
            data.StatusUpdatedOn = dtCancelled;
        }

        var issueDate = data.IssueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        var expectedDataCsv = $@"{data.PrnNumber},PERN,{displayStatus},{data.IssuedByOrg},{data.OrganisationName},{data.AccreditationNumber},{issueDate},Yes,{data.MaterialName},{data.ProcessToBeUsed},{data.TonnageValue},{whenAccepted},{whenCancelled},{data.IssuerNotes}";

        _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync([data]);

        var result = await _systemUnderTest.GetPrnsCsvStreamAsync();

        using (var reader = new StreamReader(result))
        {
            await reader.ReadLineAsync(); // skip the header row
            var actual = await reader.ReadLineAsync();
            actual.Should().Be(expectedDataCsv);
        }
    }

    [Test]
    [TestCase(null, "not_provided")]
    [TestCase("", "not_provided")]
    [TestCase(" ", "not_provided")]
    [TestCase("Important notes", "Important notes")]
    public async Task GetPrnsCsvStreamAsync_ReturnsIssuerNotes(string notes, string expectedResult)
    {
        var data = Fixture.Create<PrnModel>();
        data.IssuerNotes = notes;

        _webApiGatewayClientMock.Setup(x => x.GetPrnsForLoggedOnUserAsync()).ReturnsAsync([data]);

        var result = await _systemUnderTest.GetPrnsCsvStreamAsync();

        using (var reader = new StreamReader(result))
        {
            await reader.ReadLineAsync(); // skip the header row
            reader.ReadLine().Should().EndWith(expectedResult);
        }
    }

    [Test]
    public async Task GetRecyclingObligationsCalculation_Returns_Expected_ViewModel()
    {
        // Arrange
        var year = 2023;
        var expectedNumberOfPrnsAwaitingAcceptance = 2;
        var expectedMaterialsInlcudingTotal = 7;
        var expectedGlassMaterialsInlcudingTotal = 3;
        var organisationId = Guid.NewGuid();

        var materialTypes = new List<MaterialType>
        {
            MaterialType.Aluminium,
            MaterialType.Glass,
            MaterialType.GlassRemelt,
            MaterialType.Paper,
            MaterialType.Plastic,
            MaterialType.Wood,
            MaterialType.Steel
        };

        var prnMaterials = new List<PrnMaterialObligationModel>();
        for (int i = 0; i < 7; i++)
        {
            var model = Fixture.Build<PrnMaterialObligationModel>()
                .With(x => x.MaterialName, materialTypes[i].ToString()) // Assign unique MaterialType)
                .With(x => x.Status, GetRandomObligationStatus())
                .Create();
            prnMaterials.Add(model);
        }

        var prnObligationModel = new PrnObligationModel
        {
            NumberOfPrnsAwaitingAcceptance = expectedNumberOfPrnsAwaitingAcceptance,
            ObligationData = prnMaterials
        };
        _webApiGatewayClientMock.Setup(x => x.GetObligations(year))
            .ReturnsAsync(prnObligationModel);

        // Act
        var result = await _systemUnderTest.GetRecyclingObligationsCalculation(year);

        // Assert
        result.Should().NotBeNull();
        result.NumberOfPrnsAwaitingAcceptance.Should().Be(expectedNumberOfPrnsAwaitingAcceptance);
        result.MaterialObligationViewModels.Should().NotBeNullOrEmpty();
        result.GlassMaterialObligationViewModels.Should().NotBeNullOrEmpty();

        // Check the material obligations (Aluminium)
        result.MaterialObligationViewModels.Count.Should().Be(expectedMaterialsInlcudingTotal);
        var aluminium = result.MaterialObligationViewModels[0];
        var expectedAluminium = prnMaterials.Find(r => r.MaterialName == MaterialType.Aluminium.ToString());
        aluminium.MaterialName.Should().Be(MaterialType.Aluminium);
        aluminium.ObligationToMeet.Should().Be(expectedAluminium.ObligationToMeet);
        aluminium.TonnageAwaitingAcceptance.Should().Be(expectedAluminium.TonnageAwaitingAcceptance);
        aluminium.TonnageAccepted.Should().Be(expectedAluminium.TonnageAccepted);
        aluminium.TonnageOutstanding.Should().Be(expectedAluminium.TonnageOutstanding);
        aluminium.Status.Should().Be((ObligationStatus)Enum.Parse<ObligationStatus>(expectedAluminium.Status));

        // Assert the glass obligations - Glass Remelt
        result.GlassMaterialObligationViewModels.Count.Should().Be(expectedGlassMaterialsInlcudingTotal);
        var glassRemelt = result.GlassMaterialObligationViewModels[0];
        var expectedGlassRemelt = prnMaterials.Find(r => r.MaterialName == MaterialType.GlassRemelt.ToString());
        glassRemelt.MaterialName.Should().Be(MaterialType.GlassRemelt);
        glassRemelt.ObligationToMeet.Should().Be(expectedGlassRemelt.ObligationToMeet);
        glassRemelt.TonnageAwaitingAcceptance.Should().Be(expectedGlassRemelt.TonnageAwaitingAcceptance);
        glassRemelt.TonnageAccepted.Should().Be(expectedGlassRemelt.TonnageAccepted);
        glassRemelt.TonnageOutstanding.Should().Be(expectedGlassRemelt.TonnageOutstanding);
        glassRemelt.Status.Should().Be((ObligationStatus)Enum.Parse<ObligationStatus>(expectedGlassRemelt.Status));

        // Assert the glass obligations - Remaining Glass
        var remainingGlass = result.GlassMaterialObligationViewModels[1];
        var expectedRemainingGlass = prnMaterials.Find(r => r.MaterialName == MaterialType.Glass.ToString());
        remainingGlass.MaterialName.Should().Be(MaterialType.RemainingGlass);
        remainingGlass.ObligationToMeet.Should().Be(expectedRemainingGlass.ObligationToMeet);
        remainingGlass.TonnageAwaitingAcceptance.Should().Be(expectedRemainingGlass.TonnageAwaitingAcceptance);
        remainingGlass.TonnageAccepted.Should().Be(expectedRemainingGlass.TonnageAccepted);
        remainingGlass.TonnageOutstanding.Should().Be(expectedRemainingGlass.TonnageOutstanding);
        remainingGlass.Status.Should().Be((ObligationStatus)Enum.Parse<ObligationStatus>(expectedRemainingGlass.Status));

        // Assert the glass obligations - Totals
        var glassBreakDownTotals = result.GlassMaterialObligationViewModels[2];
        glassBreakDownTotals.MaterialName.Should().Be(MaterialType.Totals);
        glassBreakDownTotals.ObligationToMeet.Should().Be(glassRemelt.ObligationToMeet + remainingGlass.ObligationToMeet);
        glassBreakDownTotals.TonnageAwaitingAcceptance.Should().Be(glassRemelt.TonnageAwaitingAcceptance + remainingGlass.TonnageAwaitingAcceptance);
        glassBreakDownTotals.TonnageAccepted.Should().Be(glassRemelt.TonnageAccepted + remainingGlass.TonnageAccepted);
        glassBreakDownTotals.TonnageOutstanding.Should().Be(glassRemelt.TonnageOutstanding + remainingGlass.TonnageOutstanding);
    }

    [Theory]
    [TestCase(ObligationStatus.NotMet)]
    [TestCase(ObligationStatus.Met)]
    [TestCase(ObligationStatus.NoDataYet)]
    public async Task GetRecyclingObligationsCalculation_CheckIfOverallStatus_ReturnsRightStatus(ObligationStatus status)
    {
        // Arrange
        var year = 2024;
        var materialTypes = new List<MaterialType>
        {
            MaterialType.Aluminium,
            MaterialType.Glass,
            MaterialType.GlassRemelt,
            MaterialType.Paper,
            MaterialType.Plastic,
            MaterialType.Wood,
            MaterialType.Steel,
            MaterialType.Totals
        };

        var prnMaterials = new List<PrnMaterialObligationModel>();
        for (int i = 0; i < 8; i++)
        {
            var model = Fixture.Build<PrnMaterialObligationModel>()
                .With(x => x.MaterialName, materialTypes[i].ToString()) // Assign unique MaterialType)
                .With(x => x.Status, status.ToString())
                .Create();
            prnMaterials.Add(model);
        }

        var prnObligationModel = new PrnObligationModel
        {
            ObligationData = prnMaterials
        };
        _webApiGatewayClientMock.Setup(x => x.GetObligations(year)).ReturnsAsync(prnObligationModel);

        // Act
        var result = await _systemUnderTest.GetRecyclingObligationsCalculation(year);

        // Assert
        result.OverallStatus.Should().Be(status);
    }

    [Test]
    public async Task GetRecyclingObligationsCalculation_Returns_NoDataYet_WhenCallWithNoTotals()
    {
        // Arrange
        var year = 2024;
        var materialTypes = new List<MaterialType>
        {
            MaterialType.Aluminium,
            MaterialType.Glass,
            MaterialType.GlassRemelt,
            MaterialType.Paper,
            MaterialType.Plastic,
            MaterialType.Wood,
            MaterialType.Steel,
        };

        var prnMaterials = new List<PrnMaterialObligationModel>();
        for (int i = 0; i < 7; i++)
        {
            var model = Fixture.Build<PrnMaterialObligationModel>()
                .With(x => x.MaterialName, materialTypes[i].ToString()) // Assign unique MaterialType)
                .With(x => x.Status, ObligationStatus.NoDataYet.ToString())
                .Create();
            prnMaterials.Add(model);
        }

        var prnObligationModel = new PrnObligationModel
        {
            NumberOfPrnsAwaitingAcceptance = 0,
            ObligationData = prnMaterials
        };
        _webApiGatewayClientMock.Setup(x => x.GetObligations(year)).ReturnsAsync(prnObligationModel);

        // Act
        var result = await _systemUnderTest.GetRecyclingObligationsCalculation(year);

        // Assert
        result.OverallStatus.Should().Be(ObligationStatus.NoDataYet);
    }

    [Theory]
    [TestCase(ObligationStatus.NotMet)]
    [TestCase(ObligationStatus.Met)]
    public async Task GetRecyclingObligationsCalculation_Returns_TotalRowStatus_NotMet(ObligationStatus status)
    {
        // Arrange
        var year = 2023;
        var organisationId = Guid.NewGuid();

        var materialTypes = new List<MaterialType>
        {
            MaterialType.Aluminium,
            MaterialType.Glass,
            MaterialType.GlassRemelt,
            MaterialType.Paper,
            MaterialType.Plastic,
            MaterialType.Wood,
            MaterialType.Steel
        };

        var prnMaterials = new List<PrnMaterialObligationModel>();
        for (int i = 0; i < 7; i++)
        {
            var model = Fixture.Build<PrnMaterialObligationModel>()
                .With(x => x.MaterialName, materialTypes[i].ToString()) // Assign unique MaterialType)
                .With(x => x.ObligationToMeet, (int?)null)
                .With(x => x.TonnageOutstanding, (int?)null)
                .With(x => x.Status, status.ToString())
                .Create();
            prnMaterials.Add(model);
        }

        prnMaterials[0].ObligationToMeet = null;
        prnMaterials[0].TonnageOutstanding = null;

        var prnObligationModel = new PrnObligationModel
        {
            NumberOfPrnsAwaitingAcceptance = 2,
            ObligationData = prnMaterials
        };
        _webApiGatewayClientMock.Setup(x => x.GetObligations(year))
            .ReturnsAsync(prnObligationModel);

        // Act
        var result = await _systemUnderTest.GetRecyclingObligationsCalculation(year);

        // Assert
        result.Should().NotBeNull();
        var materialTotals = result.MaterialObligationViewModels.Find(m => m.MaterialName == MaterialType.Totals);
        materialTotals.Status.Should().Be(status);
        materialTotals.ObligationToMeet.Should().BeNull();
        materialTotals.TonnageOutstanding.Should().BeNull();
        var glassTotals = result.GlassMaterialObligationViewModels.Find(m => m.MaterialName == MaterialType.Totals);
        glassTotals.Status.Should().Be(status);
        glassTotals.ObligationToMeet.Should().BeNull();
        glassTotals.TonnageOutstanding.Should().BeNull();
    }

    private static string GetRandomObligationStatus()
    {
        var enumValues = Enum.GetValues(typeof(ObligationStatus)).Cast<ObligationStatus>();
        return enumValues.OrderBy(x => Guid.NewGuid()).FirstOrDefault().ToString();
    }

    [Test]
    public async Task GetRecyclingObligationsCalculation_ReturnsEmptyViewModel_WhenNoData()
    {
        // Arrange
        var year = 2024;
        _webApiGatewayClientMock.Setup(x => x.GetObligations(year))
            .ReturnsAsync(new PrnObligationModel());

        // Act
        var result = await _systemUnderTest.GetRecyclingObligationsCalculation(year);

        // Assert
        result.Should().NotBeNull();
        result.MaterialObligationViewModels.Should().BeNullOrEmpty();
        result.GlassMaterialObligationViewModels.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task GetRecyclingObligationsCalculation_HandlesNullObligations()
    {
        // Arrange
        var year = 2024;
        _webApiGatewayClientMock.Setup(x => x.GetObligations(year))
            .ReturnsAsync((PrnObligationModel)null);

        // Act
        var result = await _systemUnderTest.GetRecyclingObligationsCalculation(year);

        // Assert
        result.Should().NotBeNull();
        result.MaterialObligationViewModels.Should().BeNullOrEmpty();
        result.GlassMaterialObligationViewModels.Should().BeNullOrEmpty();
    }

    [Test]
    public void GetRecyclingObligationsCalculation_Throws_Exception_On_Failure()
    {
        // Arrange
        var year = 2023;

        _webApiGatewayClientMock.Setup(x => x.GetObligations(year))
            .ThrowsAsync(new Exception("API Error"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () =>
            await _systemUnderTest.GetRecyclingObligationsCalculation(year));
    }
}
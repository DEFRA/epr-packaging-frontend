namespace FrontendSchemeRegistration.UI.UnitTests.Controllers.Prns;

using AutoFixture.NUnit3;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Constants;
using FrontendSchemeRegistration.UI.Controllers.Prns;
using FrontendSchemeRegistration.UI.Services.Interfaces;
using FrontendSchemeRegistration.UI.ViewModels.Prns;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Time.Testing;
using Moq;

[TestFixture]
public class PrnsAcceptControllerTests
{
    private Mock<IPrnService> _mockPrnService;
    private FakeTimeProvider _timeProvider;
    private PrnsAcceptController _sut;

    [SetUp]
    public void SetUp()
    {
        _mockPrnService = new Mock<IPrnService>();
        _timeProvider = new();
        _sut = new PrnsAcceptController(_mockPrnService.Object, _timeProvider);
        var tempData = new TempDataDictionary(Mock.Of<HttpContext>(), Mock.Of<ITempDataProvider>());
        _sut.TempData = tempData;
    }

    // Accept single Prn. Step 3 of 5
    [Test]
    public async Task AcceptSinglePrn_LoadTheStandardResponse()
    {
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(new PrnViewModel());

        // Act
        var result = await _sut.AcceptSinglePrn(Guid.NewGuid()) as ViewResult;

        // Assert
        result.ViewName.Should().Be("AcceptSinglePrn");
        result.ViewData.Model.Should().NotBeNull();
    }

    [Theory]
    [InlineAutoData("2024-12-01", "2025-01-01", "AcceptSingle2024DecemberWastePrn")]
    [InlineAutoData("2024-12-15", "2025-01-15", "AcceptSingle2024DecemberWastePrn")]
    [InlineAutoData("2024-12-01", "2025-01-31T23:59:59Z", "AcceptSingle2024DecemberWastePrn")]
    [InlineAutoData("2024-12-01", "2025-02-01T00:00:00Z", "AcceptSinglePrn")]
    [InlineAutoData("2024-12-01", "2025-02-01T00:00:01Z", "AcceptSinglePrn")]
    [InlineAutoData("2025-01-01", "2025-02-01", "AcceptSinglePrn")]
    public async Task AcceptSinglePrn_Returns_CorrectViewIf2024DecemberWasteAndAcceptedOn(string issuedDate, string acceptedOn, string viewName, PrnViewModel prn)
    {
        _timeProvider.SetUtcNow(DateTime.Parse(acceptedOn));
        prn.IsDecemberWaste = true;
        prn.DateIssued = DateTime.Parse(issuedDate);
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(prn);

        // Act
        var result = await _sut.AcceptSinglePrn(Guid.NewGuid()) as ViewResult;

        // Assert
        result.ViewName.Should().Be(viewName);
    }

    // Step 4, return after login timeout
    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnGetRedirectToSelectPrns()
    {
        // Act
        var result = await _sut.ConfirmAcceptSinglePrnPassThrough() as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.SelectMultiplePrns));
        result.ControllerName.Should().Be("Prns");
    }

    // Accept single Prn. Step 4 of 5
    [Test]
    public async Task ConfirmAcceptSinglePrnPassThrough_OnPostRedirectToAcceptedPage()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ExternalId = Guid.NewGuid(),
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.ConfirmAcceptSinglePrnPassThrough(model) as RedirectToActionResult;

        // Assert
        result.ActionName.Should().Be(nameof(PrnsAcceptController.AcceptedPrn));
        result.ControllerName.Should().Be("PrnsAccept");
        _mockPrnService.Verify(x => x.AcceptPrnAsync(model.ExternalId), Times.Once);
    }

    // Accept single Prn. Step 5 of 5
    [Test]
    public async Task AcceptedPrn_ReturnsCorrectView_WhenPernOrPrnIsAccepted()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "ACCEPTED"
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.AcceptedPrn(model.ExternalId) as ViewResult;

        result.ViewName.Should().BeNull();
    }

    // Accept single Prn. Step 5 of 5 incorrect status
    [Test]
    public async Task AcceptedPrn_RedirectToLandingPage_WhenPernOrPrnIsNotAccepted()
    {
        // Arrange
        var model = new PrnViewModel
        {
            ApprovalStatus = "AWAITING ACCEPTANCE"
        };
        _mockPrnService.Setup(x => x.GetPrnByExternalIdAsync(It.IsAny<Guid>())).ReturnsAsync(model);

        // Act
        var result = await _sut.AcceptedPrn(model.ExternalId) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.HomePagePrn));
        result.ControllerName.Should().Be("Prns");
    }

    // Accept single Prn. Step 5 of 5 null status
    [Test]
    public async Task AcceptedPrn_RedirectToLandingPage_WhenPernOrPrnDoesNotExist()
    {
        // Act
        var result = await _sut.AcceptedPrn(Guid.NewGuid()) as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.HomePagePrn));
        result.ControllerName.Should().Be("Prns");
    }

    // Accept multiple Prns. Step 2 of 5 recover from timeout
    [Theory]
    [AutoData]
    public async Task AcceptMultiplePrnsPassThrough_OnGet_RedirectToSelectMultiplePrns()
    {
        var result = await _sut.AcceptMultiplePrnsPassThrough();

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("SelectMultiplePrns");
    }

    // Accept multiple Prns. Step 2 of 5 zero selections error
    [Theory]
    [AutoData]
    public async Task AcceptMultiplePrnsPassThrough_RedirectToSelectMultiplePrns_IfNoneIsSelectedForAcceptance(PrnListViewModel model)
    {
        model.Prns.ForEach(x => x.IsSelected = false);

        var result = await _sut.AcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("SelectMultiplePrns");
        view.RouteValues.Should().ContainSingle().Which.Value.Should().Be("zero-accepted");
    }

    // Accept multiple Prns. Step 2 of 5 zero model is null error
    [Theory]
    public async Task AcceptMultiplePrnsPassThrough_RedirectToSelectMultiplePrns_WhenModelIsNulll()
    {
        PrnListViewModel model = new();
        var result = await _sut.AcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("SelectMultiplePrns");
        view.RouteValues.Should().ContainSingle().Which.Value.Should().Be("zero-accepted");
    }

    // Accept multiple Prns. Step 2 of 5
    [Theory]
    [AutoData]
    public async Task AcceptMultiplePrnsPassThrough_RedirectToAcceptMultiplePrns_IfPrnsAreSelectedForAcceptance(PrnListViewModel model)
    {
        model.Prns[0].IsSelected = true;

        var result = await _sut.AcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("AcceptMultiplePrns");
        _sut.TempData["bulkacceptprn"].Should().BeOfType<string>().Which.Should().Contain(model.Prns[0].ExternalId.ToString());
    }

    // Accept multiple Prns. Step 3 of 5 pass PRN id
    [Theory]
    [AutoData]
    public async Task AcceptMultiplePrns_RemoveIdsFromTempDataIfIdIsNotNull(PrnListViewModel model)
    {
        _sut.TempData["bulkacceptprn"] = string.Join(",", model.Prns.Select(x => x.ExternalId).ToList());
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);
        var expectedTempData = $"{model.Prns[1].ExternalId},{model.Prns[2].ExternalId}";
        var removedPrnNumber = model.Prns[0].PrnOrPernNumber;

        var result = await _sut.AcceptMultiplePrns(model.Prns[0].ExternalId) as ViewResult;

        ((PrnListViewModel)result.Model).RemovedPrn.PrnNumber.Should().Be(removedPrnNumber);
        _sut.TempData["bulkacceptprn"].Should().BeOfType<string>().Which.Should().Be(expectedTempData);
    }

    // Accept multiple Prns. Step 3 of 5
    [Theory]
    [AutoData]
    public async Task AcceptMultiplePrns_SetsInitialNotesTypeInTempDataToPrns_IfAllSelectedArePrn(PrnListViewModel model)
    {
        model.Prns.ForEach(p => p.NoteType = "PRN");
        _sut.TempData["bulkacceptprn"] = string.Join(",", model.Prns.Select(x => x.ExternalId).ToList());
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

        var result = await _sut.AcceptMultiplePrns(Guid.Empty);

        _sut.TempData["InitialNoteTypes"].Should().BeOfType<string>().Which.Should().Be(PrnConstants.PrnsText);
    }

    // Accept multiple Prns. Step 3 of 5 nomenclature check PERNs
    [Theory]
    [AutoData]
    public async Task AcceptMultiplePrns_SetsInitialNotesTypeInTempDataToPerns_IfAllSelectedArePrn(PrnListViewModel model)
    {
        model.Prns.ForEach(p => p.NoteType = "PERN");
        _sut.TempData["bulkacceptprn"] = string.Join(",", model.Prns.Select(x => x.ExternalId).ToList());
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

        var result = await _sut.AcceptMultiplePrns(Guid.Empty);

        _sut.TempData["InitialNoteTypes"].Should().BeOfType<string>().Which.Should().Be(PrnConstants.PernsText);
    }

    // Accept multiple Prns. Step 3 of 5 nomenclature check PRNs and PERNs
    [Theory]
    [AutoData]
    public async Task AcceptMultiplePrns_SetsInitialNotesTypeInTempDataToPrnsAndPerns_IfAllSelectedAreMixOfPrnAndPern(PrnListViewModel model)
    {
        model.Prns.ForEach(p => p.NoteType = "PRN");
        model.Prns[0].NoteType = "PERN";

        _sut.TempData["bulkacceptprn"] = string.Join(",", model.Prns.Select(x => x.ExternalId).ToList());
        _mockPrnService.Setup(x => x.GetPrnsAwaitingAcceptanceAsync()).ReturnsAsync(model);

        var result = await _sut.AcceptMultiplePrns(Guid.Empty);

        _sut.TempData["InitialNoteTypes"].Should().BeOfType<string>().Which.Should().Be(PrnConstants.PrnsAndPernsText);
    }

    // Step 4, return after login timeout
    [Test]
    public async Task ConfirmAcceptMultiplePrnsPassThrough_OnGetRedirectToSelectPrns()
    {
        // Act
        var result = await _sut.ConfirmAcceptMultiplePrnsPassThrough() as RedirectToActionResult;

        result.ActionName.Should().Be(nameof(PrnsController.SelectMultiplePrns));
        result.ControllerName.Should().Be("Prns");
    }

    // Accept multiple Prns. Step 4 of 5
    [Theory]
    [AutoData]
    public async Task ConfirmAcceptMultiplePrnsPassThrough_OnPostRedirectToAcceptedPrnsBySettingTempData(PrnListViewModel model)
    {
        _mockPrnService.Setup(x => x.AcceptPrnsAsync(It.IsAny<Guid[]>())).Returns(Task.CompletedTask);

        var expectedIds = $"{model.Prns[0].ExternalId},{model.Prns[1].ExternalId},{model.Prns[2].ExternalId}";
        var result = await _sut.ConfirmAcceptMultiplePrnsPassThrough(model);

        var view = result.Should().BeOfType<RedirectToActionResult>().Which;
        view.ActionName.Should().Be("AcceptedPrns");
        _sut.TempData["bulkacceptprn"].Should().BeOfType<string>().Which.Should().Be(expectedIds);
        _mockPrnService.VerifyAll();
    }

    // Accept multiple PRNs. Step 5 of 5
    [Theory]
    [AutoData]
    public async Task AcceptedPrns_ConstructCorrectVMFromTempData(List<Guid> acceptedPrns, PrnListViewModel model)
    {
        _sut.TempData["bulkacceptprn"] = string.Join(",", acceptedPrns.ToList());

        model.Prns[0].ExternalId = acceptedPrns[0];
        model.Prns[1].ExternalId = acceptedPrns[1];
        model.Prns[2].ExternalId = acceptedPrns[2];
        model.Prns[0].NoteType = model.Prns[1].NoteType = model.Prns[2].NoteType = "PRN";

        _mockPrnService.Setup(x => x.GetAllAcceptedPrnsAsync()).ReturnsAsync(model);

        var result = await _sut.AcceptedPrns() as ViewResult;

        result.Model.Should().BeEquivalentTo(new AcceptedPrnsModel()
        {
            Count = 3,
            NoteTypes = "PRNs",
            Details = new List<AcceptedDetails>
            {
                new(model.Prns[0].Material, model.Prns[0].Tonnage),
                new(model.Prns[1].Material, model.Prns[1].Tonnage),
                new(model.Prns[2].Material, model.Prns[2].Tonnage)
            }
        });

        _mockPrnService.Verify(x => x.GetAllAcceptedPrnsAsync(), Times.Once);
    }
}
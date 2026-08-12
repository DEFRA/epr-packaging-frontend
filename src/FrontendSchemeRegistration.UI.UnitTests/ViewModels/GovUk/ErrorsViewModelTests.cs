namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels.GovUk;

using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Moq;
using UI.ViewModels.GovUk;

[TestFixture]
public class ErrorsViewModelTests
{
    private Mock<IStringLocalizer<SharedResources>> _localizerMock;
    private Mock<IViewLocalizer> _viewLocalizerMock;
    private Dictionary<string, List<ErrorViewModel>> _errors;

    [SetUp]
    public void SetUp()
    {
        _localizerMock = new Mock<IStringLocalizer<SharedResources>>();
        _viewLocalizerMock = new Mock<IViewLocalizer>();
        _errors = new Dictionary<string, List<ErrorViewModel>>();
    }

    [Test]
    public void Constructor_WhenCalled_SetsErrorsProperty()
    {
        // Arrange
        var localisedString = new LocalizedString("key", "value");
        _localizerMock.Setup(x => x["key"]).Returns(localisedString);

        // Act
        var viewModel = new ErrorsViewModel(_errors, _localizerMock.Object);

        // Assert
        viewModel.Errors.Should().BeEquivalentTo(_errors);
    }

    [Test]
    public void Constructor_WithViewLocalizerAndFieldOrder_OrdersErrorsByFieldOrderAndLocalisesMessages()
    {
        // Arrange
        _errors["zebra"] = new List<ErrorViewModel> { new() { Key = "zebra", Message = "zebra-message" } };
        _errors["alpha"] = new List<ErrorViewModel> { new() { Key = "alpha", Message = "alpha-message" } };
        _viewLocalizerMock.Setup(x => x["zebra-message"]).Returns(new LocalizedHtmlString("zebra-message", "Zebra localised"));
        _viewLocalizerMock.Setup(x => x["alpha-message"]).Returns(new LocalizedHtmlString("alpha-message", "Alpha localised"));

        // Act
        var viewModel = new ErrorsViewModel(_errors, _viewLocalizerMock.Object, "zebra", "alpha");

        // Assert
        viewModel.Errors.Keys.Should().ContainInOrder("zebra", "alpha");
        viewModel.Errors["zebra"][0].Message.Should().Be("Zebra localised");
        viewModel.Errors["alpha"][0].Message.Should().Be("Alpha localised");
    }

    [Test]
    public void Constructor_WithViewLocalizerAndNoFieldOrder_KeepsOriginalOrder()
    {
        // Arrange
        _errors["first"] = new List<ErrorViewModel> { new() { Key = "first", Message = "first-message" } };
        _errors["second"] = new List<ErrorViewModel> { new() { Key = "second", Message = "second-message" } };
        _viewLocalizerMock.Setup(x => x["first-message"]).Returns(new LocalizedHtmlString("first-message", "First"));
        _viewLocalizerMock.Setup(x => x["second-message"]).Returns(new LocalizedHtmlString("second-message", "Second"));

        // Act
        var viewModel = new ErrorsViewModel(_errors, _viewLocalizerMock.Object);

        // Assert
        viewModel.Errors.Keys.Should().ContainInOrder("first", "second");
    }

    [Test]
    public void Indexer_WhenKeyPresent_ReturnsMatchingErrors()
    {
        // Arrange
        _errors["field"] = new List<ErrorViewModel> { new() { Key = "field", Message = "msg" } };
        _localizerMock.Setup(x => x["msg"]).Returns(new LocalizedString("msg", "localised-msg"));
        var viewModel = new ErrorsViewModel(_errors, _localizerMock.Object);

        // Act & Assert
        viewModel["field"].Should().HaveCount(1);
        viewModel["field"][0].Message.Should().Be("localised-msg");
    }

    [Test]
    public void Indexer_WhenKeyMissing_ReturnsNull()
    {
        // Arrange
        var viewModel = new ErrorsViewModel(_errors, _localizerMock.Object);

        // Act & Assert
        viewModel["missing"].Should().BeNull();
    }

    [Test]
    public void HasErrorKey_WhenKeyPresent_ReturnsTrue()
    {
        // Arrange
        _errors["field"] = new List<ErrorViewModel> { new() { Key = "field", Message = "msg" } };
        _localizerMock.Setup(x => x["msg"]).Returns(new LocalizedString("msg", "localised"));
        var viewModel = new ErrorsViewModel(_errors, _localizerMock.Object);

        // Act & Assert
        viewModel.HasErrorKey("field").Should().BeTrue();
    }

    [Test]
    public void HasErrorKey_WhenKeyMissing_ReturnsFalse()
    {
        // Arrange
        var viewModel = new ErrorsViewModel(_errors, _localizerMock.Object);

        // Act & Assert
        viewModel.HasErrorKey("field").Should().BeFalse();
    }
}
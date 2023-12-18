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
}
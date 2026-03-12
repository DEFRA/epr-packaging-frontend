namespace FrontendSchemeRegistration.UI.UnitTests.Resources;

using Application.Options;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using UI.Resources;
using UI.ViewModels.Prns;

public class PrnDataResourcesLocalizerTests
{
    private PrnDataResourcesLocalizer Subject { get; set; }
    private Mock<IStringLocalizer<PrnDataResources>> MockPrnDataResources { get; set; }
    private Mock<IStringLocalizer<PrnDataResourcesPostFibre>> MockPrnDataResourcesPostFibre { get; set; }
    private FibreOptions FibreOptions { get; set; }

    [SetUp]
    public void SetUp()
    {
        MockPrnDataResources = new Mock<IStringLocalizer<PrnDataResources>>();
        MockPrnDataResourcesPostFibre = new Mock<IStringLocalizer<PrnDataResourcesPostFibre>>();
        FibreOptions = new FibreOptions();

        Subject = new PrnDataResourcesLocalizer(
            MockPrnDataResources.Object, 
            MockPrnDataResourcesPostFibre.Object,
            new OptionsWrapper<FibreOptions>(FibreOptions));
    }
    
    [Test]
    public void WhenLaunchDateIsNotSet_ShouldLocalizeFromOriginalResources()
    {
        FibreOptions.LaunchDate = null;
        ConfigureTranslation(MockPrnDataResources, "Paper/board", "Paper and board");
        ConfigureTranslation(MockPrnDataResourcesPostFibre, "Paper/board", "New paper and board");
        
        var result = Subject.Translate(new BasePrnViewModel
        {
            Material = "Paper/board",
            DateIssued = new DateTime(2026, 3, 10)
        }).ToString();

        result.Should().Be("Paper and board");
    }
    
    [TestCase("2026-03-10", 0, "New paper and board")]
    [TestCase("2026-03-10", 1, "New paper and board")]
    [TestCase("2026-03-10", -1, "Paper and board")]
    public void WhenLaunchDateAsSpecified_ShouldLocalizeAsExpected(string launchDate, int msOffset, string expectedTranslation)
    {
        FibreOptions.LaunchDate = launchDate;
        ConfigureTranslation(MockPrnDataResources, "Paper/board", "Paper and board");
        ConfigureTranslation(MockPrnDataResourcesPostFibre, "Paper/board", "New paper and board");

        var result = Subject.Translate(new BasePrnViewModel
        {
            Material = "Paper/board",
            DateIssued = new DateTime(2026, 3, 10).AddMilliseconds(msOffset)
        }).ToString();

        result.Should().Be(expectedTranslation);
    }

    private static void ConfigureTranslation<T>(
        Mock<IStringLocalizer<T>> mockStringLocalizer,
        string name,
        string value) => mockStringLocalizer.Setup(x => x[name]).Returns((string n) => new LocalizedString(n, value));
}
namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using Application.Enums;
using UI.ViewModels.Shared;

public class RegistrationHeadingViewModelTest
{
    [Test]
    public void RegistrationHeadingViewModel()
    {
        var viewModel = new RegistrationHeadingViewModel(true, RegistrationJourney.CsoLargeProducer, "Heading", null, 2025);
        Assert.That(viewModel.Heading, Is.EqualTo("Heading"));
        Assert.That(viewModel.RegistrationYear, Is.EqualTo(2025));
        Assert.That(viewModel.RegistrationJourney, Is.EqualTo(RegistrationJourney.CsoLargeProducer));
    }
}
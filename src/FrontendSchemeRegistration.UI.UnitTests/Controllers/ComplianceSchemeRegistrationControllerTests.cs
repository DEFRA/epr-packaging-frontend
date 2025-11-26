using FluentAssertions;
using FrontendSchemeRegistration.UI.Controllers;
using FrontendSchemeRegistration.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

[TestFixture]
public class ComplianceSchemeRegistrationControllerTests
{
    private ComplianceSchemeRegistrationController _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new();
    }

    [Test]
    public async Task WHEN_ComplianceSchemeRegistrationCalled_THEN_CorrectViewModelReturned()
    {
        const string nation = "foobar";
        const string expectedViewName = "ComplianceSchemeRegistration";
        var expectedViewModel = new ComplianceSchemeRegistrationViewModel("foo cso", nation);
        
        // Act
        var result = await _sut.ComplianceSchemeRegistration(nation) as ViewResult;

        // Assert
        result.Should().NotBeNull();
        result.Model.Should()
            .BeOfType<ComplianceSchemeRegistrationViewModel>()
            .And
            .Be(expectedViewModel);
    }
}
namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using UI.Helpers;

[TestFixture]
public class ModelStateHelpersTests
{
    private ModelStateDictionary _modelStateDictionary;

    [SetUp]
    public void SetUp()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
        _modelStateDictionary = new ModelStateDictionary();
    }

    [Test]
    public void AddExceptionsToModel_AddsErrorMessageToMode_WhenCalled()
    {
        // Arrange
        var exceptionCodes = new List<string> { "99" };

        // Act
        ModelStateHelpers.AddFileUploadExceptionsToModelState(exceptionCodes, _modelStateDictionary);

        // Assert
        GetModelStateErrors()
            .Should()
            .HaveCount(1)
            .And
            .Contain("Sorry, an unexpected issue occurred (code 99).  Please try your upload again later.");
    }

    private IEnumerable<string> GetModelStateErrors()
    {
        return _modelStateDictionary.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
    }
}
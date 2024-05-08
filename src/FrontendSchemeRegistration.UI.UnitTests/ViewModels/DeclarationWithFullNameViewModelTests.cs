using FluentAssertions;
using FrontendSchemeRegistration.UI.ViewModels;

namespace FrontendSchemeRegistration.UI.UnitTests.ViewModels
{
    public class DeclarationWithFullNameViewModelTests
    {
        [Test]
        public void Validate_ReturnsNoErrors_WhenDataIsValid()
        {
            // Arrange
            var systemUnderTest = new DeclarationWithFullNameViewModel { FullName = "Test Test" };

            // Act
            var result = systemUnderTest.Validate(null);

            // Assert
            result.Count().Should().Be(0);
        }

        [Test]
        [TestCase(null, "full_name_error_message.enter_your_full_name")]
        [TestCase("", "full_name_error_message.enter_your_full_name")]
        [TestCase("Long test nameeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee", "full_name_error_message.less_than_200")]
        public void Validate_ReturnsEmptyNameError_WhenNameIsInvalid(string fullName, string errorMessage)
        {
            // Arrange
            var systemUnderTest = new DeclarationWithFullNameViewModel { FullName = fullName };

            // Act
            var result = systemUnderTest.Validate(null);

            // Assert
            result.Count().Should().Be(1);

            var validationResult = result.ElementAt(0);

            validationResult.ErrorMessage.Should().Be(errorMessage);
            validationResult.MemberNames.Should().Contain(nameof(DeclarationWithFullNameViewModel.FullName));
        }
    }
}

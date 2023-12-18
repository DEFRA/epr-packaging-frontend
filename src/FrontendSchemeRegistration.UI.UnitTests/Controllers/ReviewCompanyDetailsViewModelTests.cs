namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.ComponentModel.DataAnnotations;
using Constants;
using FluentAssertions;
using UI.ViewModels;

[TestFixture]
public class ReviewCompanyDetailsViewModelTests
{
    [Test]
    public void Validate_WhenSubmitOrganisationDetailsResponseHasValue_ShouldReturnEmptyValidationResultList()
    {
        // Arrange
        var viewModel = new ReviewCompanyDetailsViewModel
        {
            SubmitOrganisationDetailsResponse = true
        };

        var validationContext = new ValidationContext(viewModel);

        // Act
        var result = viewModel.Validate(validationContext);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void Validate_WhenSubmitOrganisationDetailsResponseIsNullAndIsComplianceScheme_ShouldReturnValidationResultWithResponseErrorMessage()
    {
        // Arrange
        var viewModel = new ReviewCompanyDetailsViewModel
        {
            SubmitOrganisationDetailsResponse = null,
            OrganisationRole = OrganisationRoles.ComplianceScheme
        };

        var validationContext = new ValidationContext(viewModel);

        // Act
        var result = viewModel.Validate(validationContext);

        // Assert
        result.Should().HaveCount(1);
    }

    [Test]
    public void Validate_WhenSubmitOrganisationDetailsResponseIsNullAndIsNotComplianceScheme_ShouldReturnValidationResultWithResponseErrorMessageProducer()
    {
        // Arrange
        var viewModel = new ReviewCompanyDetailsViewModel
        {
            SubmitOrganisationDetailsResponse = null,
            OrganisationRole = OrganisationRoles.Producer
        };

        var validationContext = new ValidationContext(viewModel);

        // Act
        var result = viewModel.Validate(validationContext);

        // Assert
        result.Should().HaveCount(1);
    }
}
namespace FrontendSchemeRegistration.UI.UnitTests.Controllers;

using System.ComponentModel.DataAnnotations;
using Application.Enums;
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

    [Test]
    public void ShowRegistrationCaption_WhenRegistrationJourneyAndRegistrationYearAreBothSet_ShouldBeTrue()
    {
        // Arrange
        var viewModel = new ReviewCompanyDetailsViewModel
        {
            RegistrationJourney = RegistrationJourney.DirectLargeProducer,
            RegistrationYear = 2025
        };

        // Act & Assert
        viewModel.ShowRegistrationCaption.Should().BeTrue();
    }

    [Test]
    public void ShowRegistrationCaption_WhenRegistrationJourneyIsNullAndRegistrationYearIsSet_ShouldBeFalse()
    {
        // Arrange
        var viewModel = new ReviewCompanyDetailsViewModel
        {
            RegistrationJourney = null,
            RegistrationYear = 2025
        };

        // Act & Assert
        viewModel.ShowRegistrationCaption.Should().BeFalse();
    }

    [Test]
    public void ShowRegistrationCaption_WhenRegistrationJourneyIsSetAndRegistrationYearIsNull_ShouldBeFalse()
    {
        // Arrange
        var viewModel = new ReviewCompanyDetailsViewModel
        {
            RegistrationJourney = RegistrationJourney.DirectLargeProducer,
            RegistrationYear = null
        };

        // Act & Assert
        viewModel.ShowRegistrationCaption.Should().BeFalse();
    }

    [Test]
    public void ShowRegistrationCaption_WhenRegistrationJourneyAndRegistrationYearAreBothNull_ShouldBeFalse()
    {
        // Arrange
        var viewModel = new ReviewCompanyDetailsViewModel
        {
            RegistrationJourney = null,
            RegistrationYear = null
        };

        // Act & Assert
        viewModel.ShowRegistrationCaption.Should().BeFalse();
    }
}
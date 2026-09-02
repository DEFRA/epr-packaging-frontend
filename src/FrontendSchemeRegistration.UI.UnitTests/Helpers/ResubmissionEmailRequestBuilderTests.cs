namespace FrontendSchemeRegistration.UI.UnitTests.Helpers;

using Application.DTOs.ComplianceScheme;
using Application.DTOs.Submission;
using Constants;
using EPR.Common.Authorization.Models;
using FluentAssertions;
using UI.Helpers;
using UI.Sessions;

[TestFixture]
public class ResubmissionEmailRequestBuilderTests
{
    [Test]
    public void BuildResubmissionEmail_WhenNotComplianceScheme_UsesOrganisationDetailsDirectly()
    {
        // Arrange
        var organisation = new Organisation
        {
            OrganisationNumber = "123456",
            Name = "Direct Producer Ltd",
            NationId = 1,
            OrganisationRole = OrganisationRoles.Producer
        };

        var userData = new UserData
        {
            FirstName = "Jane",
            LastName = "Doe",
            Organisations = new List<Organisation> { organisation }
        };

        var submission = new PomSubmission
        {
            ActualSubmissionPeriod = "2025-P1",
            SubmissionPeriod = "2025-P2"
        };

        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto
                {
                    Name = "Should Not Be Used",
                    NationId = 99
                }
            }
        };

        // Act
        var result = ResubmissionEmailRequestBuilder.BuildResubmissionEmail(userData, submission, session);

        // Assert
        result.IsComplianceScheme.Should().BeFalse();
        result.OrganisationNumber.Should().Be("123456");
        result.ProducerOrganisationName.Should().Be("Direct Producer Ltd");
        result.NationId.Should().Be(1);
        result.SubmissionPeriod.Should().Be("2025-P1");
        result.ComplianceSchemeName.Should().BeNullOrEmpty();
        result.ComplianceSchemePersonName.Should().BeNullOrEmpty();
    }

    [Test]
    public void BuildResubmissionEmail_WhenComplianceSchemeAndActualSubmissionPeriodIsNull_FallsBackToSubmissionPeriodAndUsesSelectedComplianceScheme()
    {
        // Arrange
        var organisation = new Organisation
        {
            OrganisationNumber = "654321",
            Name = "Compliance Scheme Member Ltd",
            NationId = 2,
            OrganisationRole = OrganisationRoles.ComplianceScheme
        };

        var userData = new UserData
        {
            FirstName = "John",
            LastName = "Smith",
            Organisations = new List<Organisation> { organisation }
        };

        var submission = new PomSubmission
        {
            ActualSubmissionPeriod = null,
            SubmissionPeriod = "2025-P3"
        };

        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession
            {
                SelectedComplianceScheme = new ComplianceSchemeDto
                {
                    Name = "Selected Compliance Scheme",
                    NationId = 3
                }
            }
        };

        // Act
        var result = ResubmissionEmailRequestBuilder.BuildResubmissionEmail(userData, submission, session);

        // Assert
        result.IsComplianceScheme.Should().BeTrue();
        result.SubmissionPeriod.Should().Be("2025-P3");
        result.ProducerOrganisationName.Should().Be("Selected Compliance Scheme");
        result.ComplianceSchemeName.Should().Be("Compliance Scheme Member Ltd");
        result.ComplianceSchemePersonName.Should().Be("John Smith");
        result.NationId.Should().Be(3);
    }

    [Test]
    public void BuildResubmissionEmail_WhenOrganisationsListIsEmpty_DoesNotThrowAndUsesDefaults()
    {
        // Arrange
        var userData = new UserData
        {
            FirstName = "No",
            LastName = "Organisation",
            Organisations = new List<Organisation>()
        };

        var submission = new PomSubmission
        {
            ActualSubmissionPeriod = "2025-P1",
            SubmissionPeriod = "2025-P2"
        };

        var session = new FrontendSchemeRegistrationSession
        {
            RegistrationSession = new RegistrationSession()
        };

        // Act
        Action act = () => ResubmissionEmailRequestBuilder.BuildResubmissionEmail(userData, submission, session);

        // Assert
        act.Should().NotThrow();

        var result = ResubmissionEmailRequestBuilder.BuildResubmissionEmail(userData, submission, session);
        result.OrganisationNumber.Should().BeNull();
        result.ProducerOrganisationName.Should().BeNull();
        result.NationId.Should().Be(0);
        result.IsComplianceScheme.Should().BeFalse();
    }
}

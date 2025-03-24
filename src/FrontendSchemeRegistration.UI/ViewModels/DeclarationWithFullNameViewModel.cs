namespace FrontendSchemeRegistration.UI.ViewModels;

using System.ComponentModel.DataAnnotations;

public class DeclarationWithFullNameViewModel : IValidatableObject
{
    public string? FullName { get; set; } = string.Empty;

    public string OrganisationName { get; set; } = string.Empty;

    public Guid SubmissionId { get; set; }

    public bool IsResubmission { get; set; }

    public string OrganisationDetailsFileId { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return ValidateFullName();
    }

    private IEnumerable<ValidationResult> ValidateFullName()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            yield return new ValidationResult($"full_name_error_message.enter_your_full_name", new[] { nameof(FullName) });
        }

        if (FullName?.Length > 200)
        {
            yield return new ValidationResult($"full_name_error_message.less_than_200", new[] { nameof(FullName) });
        }
    }
}
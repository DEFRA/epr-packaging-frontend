using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class RegistrationValidationError
{
    public List<ColumnValidationError> ColumnErrors { get; set; }

    public string OrganisationId { get; set; }

    public string SubsidiaryId { get; set; }

    public int RowNumber { get; set; }

    public string IssueType { get; set; }
}
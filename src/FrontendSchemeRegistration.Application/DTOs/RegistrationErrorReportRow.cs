using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class RegistrationErrorReportRow
{
    public string Row { get; set; }

    public string OrganisationId { get; set; }

    public string SubsidiaryId { get; set; }

    public string Column { get; set; }

    public string ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the IssueType.
    /// </summary>
    /// <value>can be Error or Warning.</value>
    public string IssueType { get; set; }   

    public string Message { get; set; }

}
namespace FrontendSchemeRegistration.Application.DTOs;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class ColumnValidationError
{
    public string ErrorCode { get; set; }

    public int ColumnIndex { get; set; }

    public string ColumnName { get; set; }
}
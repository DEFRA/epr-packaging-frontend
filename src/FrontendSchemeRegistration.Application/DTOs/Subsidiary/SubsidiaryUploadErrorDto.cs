namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubsidiaryUploadErrorDto
{
    public int FileLineNumber { get; set; }

    public string FileContent { get; set; }

    public string Message { get; set; }

    public bool IsError { get; set; }

    public int ErrorNumber { get; set; }
}

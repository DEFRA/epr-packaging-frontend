namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubsidiaryUploadStatusDto
{
    public string Status { get; set; }

    public int? RowsAdded { get; set; }

    public IEnumerable<SubsidiaryUploadErrorDto> Errors { get; set; }
}

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubsidiaryUploadErrorRow
{
    public string OrganisationId { get; set; }

    public string SubsidiaryId { get; set; }

    public string OrganisationName { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public string ParentChild { get; set; }

    public string FranchiseeLicenseeTenant { get; set; }

    public int RowNumber { get; set; }

    public string Issue { get; set; }

    public string Message { get; set; }
}
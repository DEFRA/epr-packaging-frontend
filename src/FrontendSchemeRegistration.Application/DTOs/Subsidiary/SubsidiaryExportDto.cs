using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs;

[ExcludeFromCodeCoverage]
public class SubsidiaryExportDto
{
    public int Organisation_Id { get; set; }

    public int Subsidiary_Id { get; set; }

    public string Organisation_Name { get; set; }

    public string Companies_House_Number { get; set; }
}

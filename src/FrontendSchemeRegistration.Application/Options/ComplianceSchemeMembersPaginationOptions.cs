using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.Options;

[ExcludeFromCodeCoverage]
public class ComplianceSchemeMembersPaginationOptions
{
    public const string ConfigSection = "ComplianceSchemeMembersPagination";

    public int PageSize { get; set; } = 50;
}
namespace FrontendSchemeRegistration.Application.Options;

public class ComplianceSchemeMembersPaginationOptions
{
    public const string ConfigSection = "ComplianceSchemeMembersPagination";

    public int PageSize { get; set; } = 50;
}
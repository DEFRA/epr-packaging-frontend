namespace FrontendSchemeRegistration.Application.Constants;

public static class ComplianceSchemePaths
{
    public const string Select = "compliance-schemes/select/";
    public const string Update = "compliance-schemes/update/";
    public const string Remove = "compliance-schemes/remove/";
    public const string Get = "compliance-schemes/";
    public const string GetForProducer = "compliance-schemes/get-for-producer/";
    public const string GetForOperator = "compliance-schemes/get-for-operator/";
    public const string GetMemberDetails = "compliance-schemes/{0}/scheme-members/{1}";
    public const string Summary = "compliance-schemes/{0}/summary";
    public const string Members = "compliance-schemes/{0}/schemes/{1}/scheme-members?pagesize={2}&query={3}&page={4}";
    public const string GetReasonsForRemoval = "compliance-schemes/member-removal-reasons/";
    public const string RemoveComplianceSchemeMember = "compliance-schemes/{0}/scheme-members/{1}/removed";
}
namespace FrontendSchemeRegistration.Application.Enums;

using Attributes;

public enum ServiceRole
{
    [LocalizedName("Delegated Person")]
    Delegated,

    [LocalizedName("Basic User")]
    Basic,

    [LocalizedName("Approved Person")]
    Approved
}

public static class ServiceRoleConstants
{
    public const string Delegated = "Delegated Person";
    public const string Basic = "Basic User";
    public const string Approved = "Approved Person";
}
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

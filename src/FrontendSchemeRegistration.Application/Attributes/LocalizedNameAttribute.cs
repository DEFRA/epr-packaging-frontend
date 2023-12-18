using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.Attributes;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Field)]
public class LocalizedNameAttribute : Attribute
{
    public LocalizedNameAttribute(string name)
    {
        Name = name;
    }

    public string? Name { get; }
}
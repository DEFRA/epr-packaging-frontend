namespace FrontendSchemeRegistration.UI.Extensions;

using System.Reflection;
using Application.Attributes;

public static class EnumExtensions
{
    public static string? GetLocalizedName(this Enum enumValue)
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())[0]
            .GetCustomAttribute<LocalizedNameAttribute>()
            .Name;
    }

    public static bool In<T>(this T enumValue, params T[] enumValues)
        => enumValues.ToList().Exists(s => s.Equals(enumValue));

    public static T Parse<T>(this string enumValue)
    {
        return (T)typeof(T).GetFields().First(field =>
            Attribute.GetCustomAttribute(field, typeof(LocalizedNameAttribute)) is LocalizedNameAttribute attribute &&
            attribute.Name.Equals(enumValue)).GetValue(null);
    }
}
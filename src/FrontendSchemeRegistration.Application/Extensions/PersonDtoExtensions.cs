namespace FrontendSchemeRegistration.Application.Extensions;

using System.Diagnostics.CodeAnalysis;
using DTOs.UserAccount;

[ExcludeFromCodeCoverage]
public static class PersonDtoExtensions
{
    public static string GetUserName(this PersonDto person)
    {
        return $"{person.FirstName} {person.LastName}";
    }
}

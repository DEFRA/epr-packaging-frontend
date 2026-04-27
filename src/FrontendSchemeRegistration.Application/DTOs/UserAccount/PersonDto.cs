using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.UserAccount;

public class PersonDto
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string ContactEmail { get; set; }

    public bool IsDeleted { get; set; }
}
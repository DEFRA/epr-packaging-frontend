using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.CompaniesHouse;

[ExcludeFromCodeCoverage]
public class Company
{
    public Company()
    {
    }

    public Company(CompaniesHouseCompany? organisation)
        : this()
    {
        if (organisation == null)
        {
            throw new ArgumentException("Organisation cannot be null.");
        }

        CompaniesHouseNumber = organisation.Organisation?.RegistrationNumber ?? string.Empty;
        Name = organisation.Organisation?.Name ?? string.Empty;
        BusinessAddress = new Addresses.Address(organisation.Organisation?.RegisteredOffice);
        AccountCreatedOn = organisation.AccountCreatedOn;
    }

    public string Name { get; set; }

    public string CompaniesHouseNumber { get; set; }

    public string OrganisationId { get; set; }

    public Addresses.Address BusinessAddress { get; set; }

    public bool AccountExists => AccountCreatedOn is not null;

    public DateTimeOffset? AccountCreatedOn { get; set; }

    public bool? IsCompanyAlreadyLinkedToTheParent { get; set; }

    public string? ParentCompanyName { get; set; }
}

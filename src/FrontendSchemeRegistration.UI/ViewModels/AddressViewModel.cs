using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class AddressViewModel
{
    public string? AddressSingleLine { get; set; }

    public string? Street { get; set; }

    public string? Town { get; set; }

    public string? County { get; set; }

    public string? Country { get; set; }

    public string? Postcode { get; set; }

    public string?[] AddressFields => new[] { AddressSingleLine, Street, Town, County, Country, Postcode };
}

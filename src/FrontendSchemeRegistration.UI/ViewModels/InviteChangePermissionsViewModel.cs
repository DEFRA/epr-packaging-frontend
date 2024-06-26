﻿namespace FrontendSchemeRegistration.UI.ViewModels;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class InviteChangePermissionsViewModel
{
    public Guid Id { get; set; }

    public string Firstname { get; set; }

    public string Lastname { get; set; }

    public string OrganisationName { get; set; }

    public bool IsInCompaniesHouse { get; set; }
}
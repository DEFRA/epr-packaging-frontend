﻿using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels;

[ExcludeFromCodeCoverage]
public class AdditionalInformationViewModel : OrganisationNationViewModel
{
    public bool IsComplianceScheme { get; set; } = false;

    public string ComplianceScheme { get; set; }

    public string OrganisationName { get; set; }

    public string OrganisationNumber { get; set; }

    public string AdditionalInformationText { get; set; }

}
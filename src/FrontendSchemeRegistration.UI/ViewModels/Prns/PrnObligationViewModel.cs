﻿using FrontendSchemeRegistration.Application.Enums;
using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.UI.ViewModels.Prns;

[ExcludeFromCodeCoverage]
public class PrnObligationViewModel
{
    public string OrganisationRole { get; set; }
    public string OrganisationName { get; set; }
    public int? NationId { get; set; }
    public int CurrentYear { get; set; }
    public int DeadlineYear { get; set; }
    public int NumberOfPrnsAwaitingAcceptance { get; set; }

    public ObligationStatus OverallStatus { get; set; }

    public List<PrnMaterialObligationViewModel> MaterialObligationViewModels { get; set; }

    public List<PrnMaterialObligationViewModel> GlassMaterialObligationViewModels { get; set; }
}
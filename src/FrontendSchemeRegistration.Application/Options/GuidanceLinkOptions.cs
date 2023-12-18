namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class GuidanceLinkOptions
{
    public const string ConfigSection = "GuidanceLinks";

    public string WhatPackagingDataYouNeedToCollect { get; set; }

    public string HowToBuildCsvFileToReportYourPackagingData { get; set; }

    public string HowToReportOrganisationDetails { get; set; }

    public string HowToBuildCsvFileToReportYourOrganisationData { get; set; }
}

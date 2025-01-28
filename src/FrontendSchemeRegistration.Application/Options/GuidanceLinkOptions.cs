namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class GuidanceLinkOptions
{
    public const string ConfigSection = "GuidanceLinks";

    public string WhatPackagingDataYouNeedToCollect { get; set; }

    public string HowToBuildCsvFileToReportYourPackagingData { get; set; }

    public string HowToReportOrganisationDetails { get; set; }

    public string HowToReportPackagingData { get; set; }

    public string HowToBuildCsvFileToReportYourOrganisationData { get; set; }

    public string ExampleCsvFile { get; set; }

    public string ProducerResponsibilitiesForPackagingWaste { get; set; }

    public string EPR_IllustrativeFeesAndCharges { get; set; }

    public string HowToCompleteSubsidiaryFile { get; set; }

    public string YouCanPaySEPA { get; set; }
}

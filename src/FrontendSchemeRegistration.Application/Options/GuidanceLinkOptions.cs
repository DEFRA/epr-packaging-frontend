namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class GuidanceLinkOptions
{
    private const string Protocol = "https://";

    public readonly string WhatPackagingDataYouNeedToCollect = Path.Combine(Protocol,"www.gov.uk/guidance/how-to-collect-your-packaging-data-for-extended-producer-responsibility");

    public readonly string HowToBuildCsvFileToReportYourPackagingData = Path.Combine(Protocol,"www.gov.uk/government/publications/packaging-data-how-to-create-your-file-for-extended-producer-responsibility/packaging-data-file-specification-for-extended-producer-responsibility");

    public readonly string HowToReportOrganisationDetails = Path.Combine(Protocol,"www.gov.uk/government/publications/organisation-details-how-to-create-your-file-for-extended-producer-responsibility-epr-for-packaging");
    
    public readonly string HowToReportPackagingData = Path.Combine(Protocol,"www.gov.uk/government/publications/packaging-data-how-to-create-your-file-for-extended-producer-responsibility/packaging-data-file-specification-for-extended-producer-responsibility");
    
    public readonly string HowToBuildCsvFileToReportYourOrganisationData = Path.Combine(Protocol,"www.gov.uk/government/publications/organisation-details-how-to-create-your-file-for-extended-producer-responsibility-epr-for-packaging/organisation-details-file-specification-for-extended-producer-responsibility");

    public readonly string ExampleCsvFile = Path.Combine(Protocol,"l}www.gov.uk/government/publications/extended-producer-responsibility-for-packaging-example-file-for-packaging-data");

    public readonly string ProducerResponsibilitiesForPackagingWaste = Path.Combine(Protocol,"www.gov.uk/guidance/extended-producer-responsibility-for-packaging-who-is-affected-and-what-to-do");

    public readonly string EPR_IllustrativeFeesAndCharges = Path.Combine(Protocol,"www.gov.uk/guidance/extended-producer-responsibility-for-packaging-recycling-obligations-and-waste-disposal-fees");

    public readonly string HowToCompleteSubsidiaryFile = Path.Combine(Protocol,"www.gov.uk/government/publications/groups-and-subsidiaries-how-to-create-your-file-for-extended-producer-responsibility");

    public readonly string YouCanPaySEPA = Path.Combine(Protocol,"beta.sepa.scot/about-sepa/online-payments/");

    public readonly string MakeChangesToYourLimitedCompany = Path.Combine(Protocol,"www.gov.uk/file-changes-to-a-company-with-companies-house");
    
    public readonly string PrnObligation = Path.Combine(Protocol,"www.gov.uk/guidance/extended-producer-responsibility-for-packaging-who-is-affected-and-what-to-do#period-you-must-report-on");
}

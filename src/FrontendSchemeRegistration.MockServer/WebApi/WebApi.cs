using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;

namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class WebApi
{
    public static WireMockServer WithWebApi(this WireMockServer server, WebApiOptions? options = null)
    {
        options ??= new WebApiOptions();
        
        // Actual submission period
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions/actual-submission-period/*")
                .WithParam("submissionPeriod", true, ".+"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { actualSubmissionPeriod = "2024" }));

        // Submit registration application event
        server.Given(Request.Create()
                .UsingPost()
                .WithPath("/api/v1/submissions/*/submit-registration-application"))
            .RespondWith(Response.Create().WithStatusCode(204));

        // Submit (generic)
        server.Given(Request.Create()
                .UsingPost()
                .WithPath("/api/v1/submissions/*/submit"))
            .RespondWith(Response.Create().WithStatusCode(204));

        // Registration application details
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/registration/get-registration-application-details")
                .WithParam(MatchSmallProducerRegistrationJourney)
            ).RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("WebApi/Responses/RegistrationTaskList/SmallProducerRegistrationApplicationDetails.json"));
        
        server.Given(Request.Create()
            .UsingGet()
            .WithPath("/api/v1/registration/get-registration-application-details")
            .WithParam(MatchLargeProducerRegistrationJourney)
        ).RespondWith(Response.Create()
            .WithStatusCode(200)
            .WithHeader("Content-Type", "application/json")
            .WithBodyFromFile("WebApi/Responses/RegistrationTaskList/LargeProducerRegistrationApplicationDetailsInProgress.json"));

        // Producer validations
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions/*/producer-validations"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));

        // Registration validations
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions/*/organisation-details-errors"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));

        // Decisions (kept minimal, duplication avoided if already in Submissions)
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/decisions"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { items = System.Array.Empty<object>() }));

        // Submission Ids
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions/submission-Ids/*"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));

        // Submission history
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions/submission-history/*"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]"));

        // PRN endpoints (minimal)
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/organisation"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("WebApi/Responses/WebApi/v1_prn_organisation.json"));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/search"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("WebApi/Responses/WebApi/v1_prn_search.json"));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/00000000-0000-0000-0000-000000000001"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = 1,
                    externalId = "00000000-0000-0000-0000-000000000001",
                    prnNumber = "PRN-01",
                    materialName = "Paper/board",
                    issueDate = "2026-03-01T13:15:15",
                    prnStatus = "AWAITINGACCEPTANCE",
                    tonnageValue = 1,
                    obligationYear = "2026"
                }));
        
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/00000000-0000-0000-0000-000000000002"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = 1,
                    externalId = "00000000-0000-0000-0000-000000000001",
                    prnNumber = "PRN-02",
                    materialName = "Paper/board",
                    issueDate = "2026-05-01T13:15:15",
                    prnStatus = "AWAITINGACCEPTANCE",
                    tonnageValue = 1,
                    obligationYear = "2026"
                }));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/00000000-0000-0000-0000-000000000003"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = 5,
                    externalId = "00000000-0000-0000-0000-000000000003",
                    prnNumber = "PRN-03",
                    materialName = "Fibre",
                    issueDate = "2026-03-01T13:15:15",
                    prnStatus = "AWAITINGACCEPTANCE",
                    tonnageValue = 1,
                    obligationYear = "2026"
                }));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/00000000-0000-0000-0000-000000000005"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = 5,
                    externalId = "00000000-0000-0000-0000-000000000005",
                    prnNumber = "PRN-05",
                    materialName = "Fibre",
                    issueDate = "2026-05-01T13:15:15",
                    prnStatus = "ACCEPTED",
                    tonnageValue = 1,
                    obligationYear = "2026"
                }));

        // PRN contract test fixtures (fully-populated PRN, PERN, cancelled PRN)
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/00000000-0000-0000-0000-000000000007"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = 7,
                    externalId = "00000000-0000-0000-0000-000000000007",
                    prnNumber = "CONTRACT-PRN-007",
                    materialName = "Aluminium",
                    issueDate = "2025-06-15T10:30:00",
                    prnStatus = "AWAITINGACCEPTANCE",
                    tonnageValue = 999,
                    obligationYear = "2025",
                    issuedByOrg = "Acme Reprocessors Ltd",
                    issuerNotes = "Important note about this PRN",
                    prnSignatory = "Jane Smith",
                    prnSignatoryPosition = "Director",
                    reprocessingSite = "42 Factory Road, Manchester",
                    organisationName = "Test Producer Ltd",
                    isExport = false,
                    decemberWaste = false
                }));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/00000000-0000-0000-0000-000000000008"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = 8,
                    externalId = "00000000-0000-0000-0000-000000000008",
                    prnNumber = "CONTRACT-PERN-008",
                    materialName = "Glass remelt",
                    issueDate = "2025-06-15T10:30:00",
                    prnStatus = "AWAITINGACCEPTANCE",
                    tonnageValue = 500,
                    obligationYear = "2025",
                    isExport = true
                }));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/00000000-0000-0000-0000-000000000009"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = 9,
                    externalId = "00000000-0000-0000-0000-000000000009",
                    prnNumber = "CONTRACT-PRN-009",
                    materialName = "Fibre",
                    issueDate = "2025-06-15T10:30:00",
                    prnStatus = "CANCELED",
                    tonnageValue = 1,
                    obligationYear = "2025",
                    isExport = false,
                    prnSignatoryPosition = (string?)null,
                    processToBeUsed = (string?)null
                }));

        server.Given(Request.Create().UsingPost().WithPath("/api/v1/prn/status"))
            .RespondWith(Response.Create().WithStatusCode(200));
        
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/obligationcalculation/*"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile($"WebApi/Responses/WebApi/{options.PrnObligationCalculationResponseFile}"));

        // Subsidiary (returning empty/default object)
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/subsidiary/*/*"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBodyAsJson(new { }));

        // File download
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/file-download"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/octet-stream")
                .WithBody("dummy-file-content"));

        // Packaging resubmission endpoints
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/packaging-resubmission/get-application-details"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json").WithBody("[]"));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/packaging-resubmission/get-resubmission-member-details/*"))
            .RespondWith(Response.Create().WithStatusCode(204));

        server.Given(Request.Create().UsingPost().WithPath("/api/v1/packaging-resubmission/*/create-packaging-resubmission-reference-number-event"))
            .RespondWith(Response.Create().WithStatusCode(204));

        server.Given(Request.Create().UsingPost().WithPath("/api/v1/packaging-resubmission/*/create-packaging-resubmission-fee-view-event"))
            .RespondWith(Response.Create().WithStatusCode(204));

        server.Given(Request.Create().UsingPost().WithPath("/api/v1/packaging-resubmission/*/create-packaging-resubmission-fee-payment-event"))
            .RespondWith(Response.Create().WithStatusCode(204));

        server.Given(Request.Create().UsingPost().WithPath("/api/v1/packaging-resubmission/*/create-packaging-resubmission-application-submitted-event"))
            .RespondWith(Response.Create().WithStatusCode(204));

        return server;
    }

    

    private static bool MatchSmallProducerRegistrationJourney(IDictionary<string, WireMockList<string>> arg)
    {
        return arg.ContainsKey("RegistrationJourney") && arg["RegistrationJourney"].Count != 0 && arg["RegistrationJourney"][0].Equals("CsoSmallProducer");
    }
    
    private static bool MatchLargeProducerRegistrationJourney(IDictionary<string, WireMockList<string>> arg)
    {
        return arg.ContainsKey("RegistrationJourney") && arg["RegistrationJourney"].Count != 0 && arg["RegistrationJourney"][0].Equals("CsoLargeProducer");
    }
}

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;

namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class WebApi
{
    public static WireMockServer WithWebApi(this WireMockServer server)
    {
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
                .WithHeader("Content-Type", "application/json").WithBody("[]"));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/search"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { items = System.Array.Empty<object>(), pageNumber = 1, pageSize = 50, totalItems = 0, totalPages = 0 }));

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/*"))
            .RespondWith(Response.Create().WithStatusCode(404));

        server.Given(Request.Create().UsingPost().WithPath("/api/v1/prn/status"))
            .RespondWith(Response.Create().WithStatusCode(200));

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

    private static bool MatchOrganisationNumber(IDictionary<string, WireMockList<string>> arg)
    {
        return arg.ContainsKey("OrganisationNumber") && arg["OrganisationNumber"].Count != 0 && arg["OrganisationNumber"][0].Equals("153940");
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

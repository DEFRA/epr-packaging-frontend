using System.Text.Json;
using System.Text.Json.Nodes;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;
using WireMock.Matchers;

[ExcludeFromCodeCoverage]
public static class WebApi
{
    private const string ExtendedJourneyLargeSubmissionId = "e4749e86-54d7-4904-997d-24aa536ab40d";
    private const string ExtendedJourneySmallSubmissionId = "f4749e86-54d7-4904-997d-24aa536ab40d";
    /// <summary>
    /// Supported email keywords that drive registration task-list state.
    /// The mock inspects the email portion of the bearer token (case-insensitive)
    /// and picks the first matching keyword from this list.
    /// </summary>
    private static readonly (string Keyword, string Suffix)[] EmailKeywords =
    [
        ("notstarted", "NotStarted"),
        ("uploaded", "Uploaded"),
        ("fees", "Fees"),
        ("paid", "Paid"),
        ("completed", "Completed"),
        ("accepted", "Accepted"),
        ("rejected", "Rejected"),
        ("queried", "Queried"),
    ];

    public static WireMockServer WithWebApi(this WireMockServer server, WebApiOptions? options = null)
    {
        options ??= new WebApiOptions();
        
        // Person lookup — used by various controllers to display uploader/submitter names.
        // /api/persons/all-persons?userId=... used by FileUploadCheckFileAndSubmit (POM)
        // /api/persons?userId=...            used by CompanyDetailsConfirmation (registration)
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/persons/all-persons"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { firstName = "Test", lastName = "User", contactEmail = "test@test.com", isDeleted = false }));

        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/persons"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { firstName = "Test", lastName = "User", contactEmail = "test@test.com", isDeleted = false }));

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

        // Resubmission email notification to regulator — triggered when LastSubmittedFile != null
        server.Given(Request.Create()
                .UsingPost()
                .WithPath("/api/regulators/resubmission-notify/"))
            .RespondWith(Response.Create().WithStatusCode(200));

        // Submission IDs by organisation — used by IsAnySubmissionAcceptedForDataPeriod
        // Returns empty array so the controller treats this as a first submission and
        // redirects to the confirmation page rather than the resubmission confirmation.
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions/submission-Ids/*"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(Array.Empty<object>()));

        // File upload — returns a Location header with a new submission GUID.
        // During manual testing (no test hook has pre-set a scenario), the FileName and SubmissionType
        // headers are inspected so the outcome can be controlled by the uploaded filename:
        //   *warnings* in filename → warnings response
        //   *errors*   in filename → errors response
        //   anything else          → success (default)
        // This applies independently to POM (SubmissionType: Producer) and
        // registration (SubmissionType: Registration) uploads.
        server.Given(Request.Create()
                .UsingPost()
                .WithPath("/api/v1/file-upload"))
            .RespondWith(Response.Create().WithCallback(req =>
            {
                var fileName = req.Headers.TryGetValue("FileName", out var fileNameValues)
                    ? fileNameValues.FirstOrDefault() ?? string.Empty
                    : string.Empty;

                var submissionType = req.Headers.TryGetValue("SubmissionType", out var submissionTypeValues)
                    ? submissionTypeValues.FirstOrDefault() ?? string.Empty
                    : string.Empty;

                var scenarioFromFileName = fileName.Contains("warnings", StringComparison.OrdinalIgnoreCase)
                    ? "Warnings"
                    : fileName.Contains("errors", StringComparison.OrdinalIgnoreCase)
                        ? "Errors"
                        : "Success";

                // Reuse the existing submission ID (header) so the app stays on the same
                // submission after a re-upload; otherwise mint a new one.
                var existingSubmissionId = req.Headers.TryGetValue("SubmissionId", out var submissionIdValues)
                    ? submissionIdValues.FirstOrDefault()
                    : null;
                var submissionId = !string.IsNullOrWhiteSpace(existingSubmissionId)
                    ? existingSubmissionId
                    : Guid.NewGuid().ToString();

                if (submissionType.Equals("Registration", StringComparison.OrdinalIgnoreCase))
                {
                    if (!RegistrationUploadScenario.LockedByHook)
                        RegistrationUploadScenario.Current = scenarioFromFileName;
                    RegistrationUploadScenario.LastSubmissionId = submissionId;
                }
                else
                {
                    if (!PomUploadScenario.LockedByHook)
                        PomUploadScenario.Current = scenarioFromFileName;
                    PomUploadScenario.LastSubmissionId = submissionId;
                }

                return new WireMock.ResponseMessage
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, WireMockList<string>>
                    {
                        { "Content-Type", new WireMockList<string>("application/json") },
                        { "Location", new WireMockList<string>($"/api/v1/submissions/{submissionId}") }
                    },
                    BodyData = new BodyData
                    {
                        BodyAsString = "{}",
                        DetectedBodyType = BodyType.String
                    }
                };
            }));

        // Registration application details — uses WithCallback to vary by email keyword
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/registration/get-registration-application-details"))
            .RespondWith(Response.Create().WithCallback(req =>
            {
                var journey = GetRegistrationJourney(req);
                var prefix = journey == "CsoSmallProducer" ? "SmallProducer" : "LargeProducer";
                var suffix = ResolveResponseSuffix(req);
                var filePath = $"WebApi/Responses/RegistrationTaskList/{prefix}{suffix}.json";

                if (!File.Exists(filePath))
                    filePath = $"WebApi/Responses/RegistrationTaskList/{prefix}RegistrationApplicationDetails.json";

                var json = File.ReadAllText(filePath);

                var doc = JsonNode.Parse(json)!;
                doc["registrationJourney"] = journey;
                json = doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

                return new WireMock.ResponseMessage
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, WireMockList<string>>
                    {
                        { "Content-Type", new WireMockList<string>("application/json") }
                    },
                    BodyData = new BodyData
                    {
                        BodyAsString = json,
                        DetectedBodyType = BodyType.String
                    }
                };
            }));

        // GET /api/v1/submissions?type=...&periods=...&limit=...&complianceSchemeId=...
        // Returns a POM submission list for email-driven POM status scenarios; empty list otherwise.
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions"))
            .AtPriority(100)
            .RespondWith(Response.Create().WithCallback(req => BuildPomSubmissionsListResponse(req)));

        // GET /api/v1/submissions/{id} — single submission (for registration file upload flow)
        server.Given(Request.Create()
                .UsingGet()
                .WithPath(new RegexMatcher("^/api/v1/submissions/[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$")))
            .AtPriority(10)
            .RespondWith(Response.Create().WithCallback(req => GetSubmissionByIdResponse(req)));

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

        // Decisions — returns email-driven POM decision for accepted/rejected/resubfees scenarios;
        // returns an empty object (null Decision) for all other cases.
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/decisions"))
            .RespondWith(Response.Create().WithCallback(req => BuildPomDecisionResponse(req)));

        // Submission Ids — for pom.resubfees.* emails returns the matching submission ID so that
        // IsAnySubmissionAcceptedForDataPeriod can verify acceptance via submission history.
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions/submission-Ids/*"))
            .RespondWith(Response.Create().WithCallback(req => BuildSubmissionIdsResponse(req)));

        // Submission history — for pom.resubfees.* emails returns an "Accepted" entry so that
        // IsAnySubmissionAcceptedForDataPeriod returns true and the journey reaches the task list.
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/v1/submissions/submission-history/*"))
            .RespondWith(Response.Create().WithCallback(req => BuildSubmissionHistoryResponse(req)));

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

        server.Given(Request.Create().UsingPost().WithPath("/api/v1/prn/status"))
            .RespondWith(Response.Create().WithStatusCode(200));
        
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/obligationcalculation/*"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile($"WebApi/Responses/WebApi/{options.PrnObligationCalculationResponseFile}"));

        object complianceDeclarationsResponse = options.ComplianceDeclarationStatus switch
        {
            WebApiOptions.ComplianceDeclarationStatusType.Submitted => new
            {
                complianceDeclarations = new[] { new { created = "2026-04-27T14:00:00+00:00", status = "Submitted" } }
            },
            WebApiOptions.ComplianceDeclarationStatusType.Cancelled => new
            {
                complianceDeclarations = new[] { new { created = "2026-04-27T14:00:00+00:00", status = "Cancelled" } }
            },
            _ => new { complianceDeclarations = Array.Empty<object>() }
        };

        server.Given(Request.Create().UsingGet().WithPath("/api/v1/prn/compliance-declarations"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(complianceDeclarationsResponse));

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
        // Returns an in-progress resubmission session for resubfees email scenarios; empty list otherwise.
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/packaging-resubmission/get-application-details"))
            .RespondWith(Response.Create().WithCallback(req => BuildPackagingResubmissionDetailsResponse(req)));

        // For pom.resubfees.* emails returns memberCount=1 so GetMemberCount > 0 and the payment
        // facade is actually called. Other emails return 204 (null response → memberCount stays 0).
        server.Given(Request.Create().UsingGet().WithPath("/api/v1/packaging-resubmission/get-resubmission-member-details/*"))
            .RespondWith(Response.Create().WithCallback(req => BuildMemberDetailsResponse(req)));

        // Payment facade — resubmission fees (both producer and compliance-scheme paths).
        // pom.resubfees.pending   → fee applicable (£2,500 outstanding)
        // pom.resubfees.viewed    → fee zero / already settled
        // pom.resubfees.nomembers → memberCount=0, payment facade not reached (early return in service)
        server.Given(Request.Create().UsingPost().WithPath("/producer/resubmission-fee"))
            .RespondWith(Response.Create().WithCallback(req => BuildResubmissionFeeResponse(req)));

        server.Given(Request.Create().UsingPost().WithPath("/compliance-scheme/resubmission-fee"))
            .RespondWith(Response.Create().WithCallback(req => BuildResubmissionFeeResponse(req)));

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


    private static string? GetRegistrationJourney(WireMock.IRequestMessage req)
    {
        if (req.Query == null)
            return null;

        var hasComplianceScheme = req.Query.TryGetValue("ComplianceSchemeId", out var csVals)
                                  && csVals.Count > 0
                                  && !string.IsNullOrWhiteSpace(csVals[0])
                                  && csVals[0] != Guid.Empty.ToString();

        if (!hasComplianceScheme)
            return null;

        if (!IsRegistrationYear2026OrLater(req))
            return null;

        if (req.Query.TryGetValue("RegistrationJourney", out var vals) && vals.Count > 0)
            return vals[0];

        return null;
    }

    private static bool IsRegistrationYear2026OrLater(WireMock.IRequestMessage req)
    {
        if (req.Query != null
            && req.Query.TryGetValue("SubmissionPeriod", out var periodVals)
            && periodVals.Count > 0)
        {
            var period = periodVals[0];
            var yearMatch = System.Text.RegularExpressions.Regex.Match(period, @"\d{4}");
            if (yearMatch.Success && int.TryParse(yearMatch.Value, out var year))
                return year >= 2026;
        }

        return false;
    }

    private static string ResolveResponseSuffix(WireMock.IRequestMessage req)
    {
        var email = StubToken.ExtractEmail(req);
        if (!string.IsNullOrEmpty(email))
        {
            foreach (var (keyword, suffix) in EmailKeywords)
            {
                if (email.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return suffix;
            }
        }

        return "RegistrationApplicationDetails";
    }

    private static WireMock.ResponseMessage GetSubmissionByIdResponse(WireMock.IRequestMessage req)
    {
        var path = req.Path;
        var id = path.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();
        var isExtendedJourneyLargeSubmission = id.Equals(ExtendedJourneyLargeSubmissionId, StringComparison.OrdinalIgnoreCase);
        var isExtendedJourneySmallSubmission = id.Equals(ExtendedJourneySmallSubmissionId, StringComparison.OrdinalIgnoreCase);
        var isExtendedJourneySubmission = isExtendedJourneyLargeSubmission || isExtendedJourneySmallSubmission;
        var registrationJourney = isExtendedJourneySmallSubmission ? "CsoSmallProducer" : "CsoLargeProducer";

        // Route to POM response if this submission ID was created by a POM file upload,
        // OR if a POM hook has set a scenario without an explicit ID (test-only path).
        var isPomById = !string.IsNullOrEmpty(PomUploadScenario.LastSubmissionId) &&
                        id.Equals(PomUploadScenario.LastSubmissionId, StringComparison.OrdinalIgnoreCase);
        var isPomByHookOnly = isPomById is false &&
                              !string.IsNullOrEmpty(PomUploadScenario.Current) &&
                              string.IsNullOrEmpty(PomUploadScenario.LastSubmissionId);

        if (isPomById || isPomByHookOnly)
        {
            return BuildPomSubmissionResponse(id, PomUploadScenario.Current ?? PomUploadScenario.Success);
        }

        var scenario = RegistrationUploadScenario.Current;
        if (string.IsNullOrEmpty(scenario))
        {
            scenario = RegistrationUploadScenario.Success;
        }

        var companyDetailsUploadedBy = "a987eaf3-b117-bbcb-b535-e485aabc6576";
        var companyDetailsFileId = Guid.NewGuid().ToString();
        var lastUploadedValidFiles = new
        {
            companyDetailsFileId,
            companyDetailsFileName = "organisation-details.csv",
            companyDetailsUploadedBy,
            companyDetailsUploadDatetime = DateTime.UtcNow.ToString("o"),
            brandsFileName = isExtendedJourneySubmission ? "brands.csv" : (string?)null,
            brandsUploadedBy = isExtendedJourneySubmission ? companyDetailsUploadedBy : (string?)null,
            brandsUploadDatetime = isExtendedJourneySubmission ? DateTime.UtcNow.ToString("o") : (string?)null,
            partnershipsFileName = isExtendedJourneySubmission ? "partnerships.csv" : (string?)null,
            partnershipsUploadedBy = isExtendedJourneySubmission ? companyDetailsUploadedBy : (string?)null,
            partnershipsUploadDatetime = isExtendedJourneySubmission ? DateTime.UtcNow.ToString("o") : (string?)null
        };

        // lastSubmittedFiles is needed by CompanyDetailsConfirmationController after the
        // declaration is submitted. isSubmitted=true enables the confirmation page to render.
        var lastSubmittedFiles = new
        {
            companyDetailsFileId,
            companyDetailsFileName = "organisation-details.csv",
            brandsFileName = isExtendedJourneySubmission ? "brands.csv" : (string?)null,
            partnersFileName = isExtendedJourneySubmission ? "partnerships.csv" : (string?)null,
            submittedDateTime = DateTime.UtcNow.ToString("o"),
            submittedBy = companyDetailsUploadedBy
        };

        object body = scenario switch
        {
            RegistrationUploadScenario.Success => new
            {
                id,
                type = "Registration",
                registrationYear = 2026,
                registrationJourney = isExtendedJourneySubmission ? registrationJourney : null,
                submissionPeriod = "January to December 2026",
                companyDetailsDataComplete = true,
                validationPass = true,
                hasWarnings = false,
                hasValidFile = true,
                isSubmitted = true,
                errors = Array.Empty<string>(),
                companyDetailsFileName = "organisation-details.csv",
                requiresBrandsFile = isExtendedJourneySubmission,
                requiresPartnershipsFile = isExtendedJourneySubmission,
                brandsDataComplete = true,
                partnershipsDataComplete = true,
                lastUploadedValidFiles,
                lastSubmittedFiles
            },
            RegistrationUploadScenario.Warnings => new
            {
                id,
                type = "Registration",
                registrationYear = 2026,
                registrationJourney = isExtendedJourneySubmission ? registrationJourney : null,
                submissionPeriod = "January to December 2026",
                companyDetailsDataComplete = true,
                validationPass = true,
                hasWarnings = true,
                hasValidFile = true,
                isSubmitted = true,
                errors = Array.Empty<string>(),
                companyDetailsFileName = "organisation-details.csv",
                requiresBrandsFile = isExtendedJourneySubmission,
                requiresPartnershipsFile = isExtendedJourneySubmission,
                brandsDataComplete = true,
                partnershipsDataComplete = true,
                lastUploadedValidFiles,
                lastSubmittedFiles
            },
            RegistrationUploadScenario.Errors => new
            {
                id,
                type = "Registration",
                registrationYear = 2026,
                registrationJourney = isExtendedJourneySubmission ? registrationJourney : null,
                submissionPeriod = "January to December 2026",
                companyDetailsDataComplete = true,
                validationPass = false,
                hasWarnings = false,
                hasValidFile = false,
                errors = Array.Empty<string>(),
                rowErrorCount = 2,
                companyDetailsFileName = "organisation-details.csv",
                requiresBrandsFile = false,
                requiresPartnershipsFile = false,
                brandsDataComplete = false,
                partnershipsDataComplete = false
            },
            // Error code 935: closed-loop registration submitted for a year before ClosedLoopRegistrationFromYear.
            // HasFileErrors returns true (errors.Count > 0) so UploadingOrganisationDetails redirects back to
            // the upload page, where the error message is formatted with the configured year.
            RegistrationUploadScenario.ClosedLoopErrors => new
            {
                id,
                type = "Registration",
                registrationYear = 2026,
                registrationJourney = isExtendedJourneySubmission ? registrationJourney : null,
                submissionPeriod = "January to December 2026",
                companyDetailsDataComplete = true,
                validationPass = false,
                hasWarnings = false,
                hasValidFile = false,
                errors = new[] { "935" },
                companyDetailsFileName = "organisation-details.csv",
                requiresBrandsFile = false,
                requiresPartnershipsFile = false,
                brandsDataComplete = false,
                partnershipsDataComplete = false
            },
            _ => new { id, type = "Registration", companyDetailsDataComplete = false, validationPass = false, hasWarnings = false, hasValidFile = false, errors = Array.Empty<string>() }
        };

        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>>
            {
                { "Content-Type", new WireMockList<string>("application/json") }
            },
            BodyData = new BodyData
            {
                BodyAsJson = body,
                DetectedBodyType = BodyType.Json
            }
        };
    }

    // ─── POM Status (email-driven) helpers ──────────────────────────────────────

    private const string PomStatusSubmissionPeriod = "January to June 2025";
    private const string PomStatusUploadedFileId   = "b0000001-0000-0000-0000-000000000000";
    private const string PomStatusSubmittedFileId  = "c0000001-0000-0000-0000-000000000000";
    private const string PomStatusUploaderUserId   = "a987eaf3-b117-bbcb-b535-e485aabc6576";

    /// <summary>
    /// Extracts the POM status keyword from the bearer token email.
    /// Supported keywords (matched with "pom." prefix to avoid collisions with registration emails):
    ///   pom.accepted, pom.rejected,
    ///   pom.resubfees.pending   – resubmission in progress, memberCount=1, fee £2,500 outstanding
    ///   pom.resubfees.viewed    – resubmission in progress, memberCount=1, fee paid (zero outstanding)
    ///   pom.resubfees.nomembers – resubmission in progress, memberCount=0, no additional fee to pay
    /// </summary>
    private static string? GetPomStatusKeyword(WireMock.IRequestMessage req)
    {
        var email = StubToken.ExtractEmail(req);
        if (string.IsNullOrEmpty(email)) return null;

        // Most-specific matches first to avoid prefix collisions.
        if (email.Contains("pom.resubfees.nomembers", StringComparison.OrdinalIgnoreCase)) return "resubfees.nomembers";
        if (email.Contains("pom.resubfees.viewed",    StringComparison.OrdinalIgnoreCase)) return "resubfees.viewed";
        if (email.Contains("pom.resubfees.pending",   StringComparison.OrdinalIgnoreCase)) return "resubfees.pending";
        if (email.Contains("pom.accepted",            StringComparison.OrdinalIgnoreCase)) return "accepted";
        if (email.Contains("pom.rejected",            StringComparison.OrdinalIgnoreCase)) return "rejected";
        return null;
    }

    private static string GetPomStatusSubmissionId(string keyword) => keyword switch
    {
        "accepted"            => "aaaa0001-0000-0000-0000-000000000000",
        "rejected"            => "aaaa0002-0000-0000-0000-000000000000",
        "resubfees.pending"   => "aaaa0003-0000-0000-0000-000000000000",
        "resubfees.viewed"    => "aaaa0004-0000-0000-0000-000000000000",
        "resubfees.nomembers" => "aaaa0005-0000-0000-0000-000000000000",
        _                     => Guid.NewGuid().ToString()
    };

    private static WireMock.ResponseMessage BuildPomSubmissionsListResponse(WireMock.IRequestMessage req)
    {
        var keyword = GetPomStatusKeyword(req);

        if (keyword is null)
        {
            return new WireMock.ResponseMessage
            {
                StatusCode = 200,
                Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
                BodyData = new BodyData { BodyAsString = "[]", DetectedBodyType = BodyType.String }
            };
        }

        var submissionId = GetPomStatusSubmissionId(keyword);

        var submission = new
        {
            id                  = submissionId,
            type                = "Producer",
            submissionPeriod    = PomStatusSubmissionPeriod,
            pomDataComplete     = true,
            validationPass      = true,
            hasWarnings         = false,
            hasValidFile        = true,
            isSubmitted         = true,
            errors              = Array.Empty<string>(),
            lastUploadedValidFile = new
            {
                fileId              = PomStatusUploadedFileId,
                fileName            = "packaging-data.csv",
                fileUploadDateTime  = "2025-10-01T10:00:00Z",
                uploadedBy          = PomStatusUploaderUserId
            },
            lastSubmittedFile = new
            {
                fileId             = PomStatusSubmittedFileId,
                fileName           = "packaging-data.csv",
                submittedDateTime  = "2025-10-01T10:00:00Z",
                submittedBy        = new Guid(PomStatusUploaderUserId)
            }
        };

        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
            BodyData = new BodyData
            {
                BodyAsString     = JsonSerializer.Serialize(new[] { submission }),
                DetectedBodyType = BodyType.String
            }
        };
    }

    private static WireMock.ResponseMessage BuildPomDecisionResponse(WireMock.IRequestMessage req)
    {
        var keyword = GetPomStatusKeyword(req);

        var decisionValue = keyword switch
        {
            "rejected"                                                   => "Rejected",
            "accepted" or "resubfees.pending"
                        or "resubfees.viewed" or "resubfees.nomembers"  => "Accepted",
            _                                                            => (string?)null
        };

        object body = decisionValue is null
            ? new { }
            : new { decision = decisionValue, isResubmissionRequired = false, comments = (string?)null };

        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
            BodyData = new BodyData { BodyAsJson = body, DetectedBodyType = BodyType.Json }
        };
    }

    private static WireMock.ResponseMessage BuildPackagingResubmissionDetailsResponse(WireMock.IRequestMessage req)
    {
        var keyword = GetPomStatusKeyword(req);

        if (keyword is not ("resubfees.pending" or "resubfees.viewed" or "resubfees.nomembers"))
        {
            return new WireMock.ResponseMessage
            {
                StatusCode = 200,
                Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
                BodyData = new BodyData { BodyAsString = "[]", DetectedBodyType = BodyType.String }
            };
        }

        var submissionId = new Guid(GetPomStatusSubmissionId(keyword));
        var isResubmissionFeeViewed = keyword is "resubfees.viewed" or "resubfees.nomembers";

        // applicationStatus 4 = AcceptedByRegulator (enum ordinal).
        // FileReachedSynapse = true → FileUploadStatus = Completed → IsResubmissionInProgress = true
        // isResubmissionFeeViewed drives InProgressSubmissionPeriodStatus:
        //   false → InProgress_Resubmission_FileInSynapse_FeesNotViewed_NotSubmitted
        //   true  → InProgress_Resubmission_FeesViewed_NotSubmitted
        var session = new
        {
            submissionId,
            isSubmitted             = true,
            applicationStatus       = 4,
            synapseResponse         = new { isFileSynced = true, organisationId = "" },
            isResubmissionFeeViewed,
            resubmissionApplicationSubmittedDate = (DateTime?)null,
            lastSubmittedFile = new
            {
                fileId            = new Guid(PomStatusSubmittedFileId),
                fileName          = "packaging-data.csv",
                submittedDateTime = "2025-10-01T10:00:00Z"
            }
        };

        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
            BodyData = new BodyData
            {
                BodyAsString     = JsonSerializer.Serialize(new[] { session }),
                DetectedBodyType = BodyType.String
            }
        };
    }

    private static WireMock.ResponseMessage BuildPomSubmissionResponse(string id, string scenario)
    {
        var uploadedFileId = Guid.NewGuid().ToString();
        // Use a distinct GUID for the previously-submitted file so that
        // FileUploadCheckFileAndSubmitController does not treat the new upload
        // as "already submitted" (it redirects to sub-landing when both IDs match).
        var previouslySubmittedFileId = Guid.NewGuid().ToString();
        var uploadedByUserId = "a987eaf3-b117-bbcb-b535-e485aabc6576";
        var lastUploadedValidFile = new
        {
            fileId = uploadedFileId,
            fileName = "packaging-data.csv",
            fileUploadDateTime = DateTime.UtcNow.ToString("o"),
            uploadedBy = uploadedByUserId
        };

        // lastSubmittedFile is needed by FileUploadSubmissionConfirmation after the user submits.
        // Its fileId deliberately differs from lastUploadedValidFile.fileId so the
        // check-file-and-submit page does not think this upload has already been submitted.
        var lastSubmittedFile = new
        {
            fileId = previouslySubmittedFileId,
            fileName = "packaging-data-previous.csv",
            submittedDateTime = DateTime.UtcNow.AddDays(-1).ToString("o"),
            submittedBy = new Guid(uploadedByUserId)
        };

        object body = scenario switch
        {
            PomUploadScenario.Success => new
            {
                id,
                type = "Producer",
                submissionPeriod = "January to June 2025",
                pomDataComplete = true,
                validationPass = true,
                hasWarnings = false,
                isSubmitted = true,
                errors = Array.Empty<string>(),
                lastUploadedValidFile,
                lastSubmittedFile
            },
            PomUploadScenario.Warnings => new
            {
                id,
                type = "Producer",
                submissionPeriod = "January to June 2025",
                pomDataComplete = true,
                validationPass = true,
                hasWarnings = true,
                isSubmitted = true,
                errors = Array.Empty<string>(),
                lastUploadedValidFile,
                lastSubmittedFile
            },
            PomUploadScenario.Errors => new
            {
                id,
                type = "Producer",
                submissionPeriod = "January to June 2025",
                pomDataComplete = true,
                validationPass = false,
                hasWarnings = false,
                errors = Array.Empty<string>()
            },
            _ => new { id, type = "Producer", pomDataComplete = false, validationPass = false, hasWarnings = false, errors = Array.Empty<string>() }
        };

        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>>
            {
                { "Content-Type", new WireMockList<string>("application/json") }
            },
            BodyData = new BodyData
            {
                BodyAsJson = body,
                DetectedBodyType = BodyType.Json
            }
        };
    }

    // Returns a SubmissionPeriodId list for pom.resubfees.* emails, empty array otherwise.
    // The list is needed so IsAnySubmissionAcceptedForDataPeriod can locate the submission
    // and then fetch its history.
    private static WireMock.ResponseMessage BuildSubmissionIdsResponse(WireMock.IRequestMessage req)
    {
        var keyword = GetPomStatusKeyword(req);
        string body;
        if (keyword is "resubfees.pending" or "resubfees.viewed" or "resubfees.nomembers")
        {
            var submissionId = GetPomStatusSubmissionId(keyword);
            var entry = new
            {
                submissionId      = new Guid(submissionId),
                submissionPeriod  = PomStatusSubmissionPeriod,
                datePeriodStartMonth = "January",
                datePeriodEndMonth   = "June",
                year              = 2025
            };
            body = JsonSerializer.Serialize(new[] { entry });
        }
        else
        {
            body = "[]";
        }

        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
            BodyData = new BodyData { BodyAsString = body, DetectedBodyType = BodyType.String }
        };
    }

    // Returns a submission history with an "Accepted" entry for pom.resubfees.* emails so
    // IsAnySubmissionAcceptedForDataPeriod returns true and the POST to the sub-landing page
    // redirects to the resubmission task list instead of regular file upload.
    private static WireMock.ResponseMessage BuildSubmissionHistoryResponse(WireMock.IRequestMessage req)
    {
        var keyword = GetPomStatusKeyword(req);
        string body;
        if (keyword is "resubfees.pending" or "resubfees.viewed" or "resubfees.nomembers")
        {
            var entry = new
            {
                submissionId = new Guid(GetPomStatusSubmissionId(keyword)),
                fileId = new Guid(PomStatusSubmittedFileId),
                fileName = "packaging-data.csv",
                userName = "Test User",
                submissionDate = "2025-07-01T10:00:00Z",
                status = "Accepted",
                dateofLatestStatusChange = "2025-07-01T10:00:00Z"
            };
            body = JsonSerializer.Serialize(new[] { entry });
        }
        else
        {
            body = "[]";
        }

        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
            BodyData = new BodyData { BodyAsString = body, DetectedBodyType = BodyType.String }
        };
    }

    // Returns a member-details payload keyed to the POM status keyword:
    //   resubfees.pending / resubfees.viewed → memberCount=1  (payment facade is called)
    //   resubfees.nomembers                  → memberCount=0  (no additional fee to pay)
    //   all other emails                     → 204 No Content (service returns null → memberCount=0)
    private static WireMock.ResponseMessage BuildMemberDetailsResponse(WireMock.IRequestMessage req)
    {
        var keyword = GetPomStatusKeyword(req);
        if (keyword is not ("resubfees.pending" or "resubfees.viewed" or "resubfees.nomembers"))
        {
            return new WireMock.ResponseMessage { StatusCode = 204 };
        }

        var count = keyword == "resubfees.nomembers" ? 0 : 1;
        var body = JsonSerializer.Serialize(new { memberCount = count, referenceNumber = count == 0 ? "EPR-RESUB-000" : "EPR-RESUB-001" });
        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
            BodyData = new BodyData { BodyAsString = body, DetectedBodyType = BodyType.String }
        };
    }

    // Returns a PackagingPaymentResponse for the payment-facade resubmission-fee endpoints.
    //   pom.resubfees.pending → fee of £2,500 outstanding (totalResubmissionFee / outstandingPayment in pence)
    //   pom.resubfees.viewed  → zero outstanding (fee has been settled / was zero)
    // All other emails return 200 with zero values to avoid 5xx that would bubble up as exceptions.
    private static WireMock.ResponseMessage BuildResubmissionFeeResponse(WireMock.IRequestMessage req)
    {
        var keyword = GetPomStatusKeyword(req);
        object feeBody = keyword switch
        {
            "resubfees.pending" => new { totalResubmissionFee = 250000, previousPayments = 0, outstandingPayment = 250000, memberCount = 1 },
            "resubfees.zero" => new { totalResubmissionFee = 0, previousPayments = 0, outstandingPayment = 0, memberCount = 1 },
            _                   => new { totalResubmissionFee = 0,       previousPayments = 0, outstandingPayment = 0,       memberCount = 1 }
        };

        return new WireMock.ResponseMessage
        {
            StatusCode = 200,
            Headers = new Dictionary<string, WireMockList<string>> { { "Content-Type", new WireMockList<string>("application/json") } },
            BodyData = new BodyData
            {
                BodyAsString     = JsonSerializer.Serialize(feeBody),
                DetectedBodyType = BodyType.String
            }
        };
    }
}

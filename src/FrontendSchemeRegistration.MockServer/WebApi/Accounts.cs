namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;
using WireMock.Matchers;

[ExcludeFromCodeCoverage]
public static class Accounts
{
    public static Guid ComplianceSchemeId { get; } = Guid.NewGuid();
    
    public static WireMockServer WithAccounts(this WireMockServer server)
    {
        // GET /api/compliance-schemes/get-for-operator/?operatorOrganisationId={id}
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/compliance-schemes/get-for-operator/"))
            .RespondWith(Response.Create().WithCallback(req =>
                {
                    var userId = StubToken.ExtractUserId(req);
                    var orgId = string.IsNullOrWhiteSpace(userId) ? Guid.NewGuid().ToString() : userId;
                    var nation = StubToken.ResolveNation(req);

                    var body = new[]
                    {
                        new
                        {
                            Id = Guid.NewGuid(),
                            Name = $"Compliance Scheme Ltd {nation.Country}",
                            CreatedOn = DateTimeOffset.UtcNow.AddYears(-3),
                            NationId = nation.NationId,
                            RowNumber = 999,
                        }
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
                }));

        // GET /api/compliance-schemes/{guid}/summary (accept any GUID via regex)
        server.Given(Request.Create()
                .UsingGet()
                .WithPath(new RegexMatcher("^/api/compliance-schemes/[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}/summary$")))
            .RespondWith(Response.Create().WithCallback(req =>
            {
                var nation = StubToken.ResolveNation(req);
                var body = new
                {
                    Name = $"Compliance Scheme Ltd {nation.Country}",
                    Nation = nation.NationName,
                    CreatedOn = DateTimeOffset.UtcNow.AddYears(-3),
                    MembersLastUpdatedOn = DateTimeOffset.UtcNow.AddDays(-7),
                    MemberCount = 123
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
            }));

        // GET /api/compliance-schemes/{orgId}/schemes/{schemeId}/scheme-members (View Members)
        server.Given(Request.Create()
                .UsingGet()
                .WithPath(new RegexMatcher("^/api/compliance-schemes/[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}/schemes/[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}/scheme-members$")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    pagedResult = new
                    {
                        searchTerm = (string?)null,
                        items = new[]
                        {
                            new
                            {
                                selectedSchemeOrganisationExternalId = Guid.Parse("b2222222-3333-4444-5555-666666666666"),
                                selectedSchemeId = Guid.Parse("c3333333-4444-5555-6666-777777777777"),
                                organisationNumber = "123456",
                                organisationName = "Mock Member Ltd",
                                companiesHouseNumber = "12345678",
                                relationships = Array.Empty<object>()
                            }
                        },
                        currentPage = 1,
                        totalItems = 1,
                        pageSize = 50,
                        searchTerms = Array.Empty<string>(),
                        typeAhead = Array.Empty<string>()
                    },
                    schemeName = "Compliance Scheme Ltd",
                    lastUpdated = DateTimeOffset.UtcNow.AddDays(-1),
                    linkedOrganisationCount = 1,
                    subsidiariesCount = 0
                }));

        // POST /api/compliance-schemes/remove/ (stop/remove a compliance scheme for a producer)
        server.Given(Request.Create()
                .UsingPost()
                .WithPath("/api/compliance-schemes/remove/"))
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // GET /api/compliance-schemes/get-for-producer/
        // Returns 204 for self-managed producers (no CS link) or a ProducerComplianceSchemeDto for CS-managed producers.
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/compliance-schemes/get-for-producer/"))
            .RespondWith(Response.Create().WithCallback(req =>
            {
                var userType = StubToken.ResolveUserType(req);
                var isCsMember = userType == StubToken.UserType.CsMemberProducer;

                if (!isCsMember)
                {
                    return new WireMock.ResponseMessage { StatusCode = 204 };
                }

                var body = new
                {
                    selectedSchemeId = Guid.Parse("d4444444-5555-6666-7777-888888888888"),
                    complianceSchemeId = Guid.Parse("e5555555-6666-7777-8888-999999999999"),
                    complianceSchemeName = "Mock Compliance Scheme Ltd",
                    complianceSchemeOperatorName = "COMPLIANCE SCHEME LTD",
                    complianceSchemeOperatorId = Guid.Parse("a1111111-2222-3333-4444-555555555555")
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
            }));

        // GET /api/user-accounts
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/user-accounts"))
            .RespondWith(Response.Create()
                .WithCallback(req =>
                {
                    var userId = StubToken.ExtractUserId(req);
                    var userType = StubToken.ResolveUserType(req);
                    var nation = StubToken.ResolveNation(req);

                    Guid orgId;
                    string orgName;
                    string orgRole;
                    string orgNumber;
                    string companiesHouseNumber;

                    switch (userType)
                    {
                        case StubToken.UserType.DirectProducer:
                            orgId = Guid.Parse("b2222222-3333-4444-5555-666666666666");
                            orgName = "DIRECT PRODUCER LTD";
                            orgRole = "Producer";
                            orgNumber = "100001";
                            companiesHouseNumber = "PRD_GENERATED_100001";
                            break;
                        case StubToken.UserType.CsMemberProducer:
                            orgId = Guid.Parse("c3333333-4444-5555-6666-777777777777");
                            orgName = "CS MEMBER PRODUCER LTD";
                            orgRole = "Producer";
                            orgNumber = "200002";
                            companiesHouseNumber = "PRD_GENERATED_200002";
                            break;
                        default:
                            orgId = Guid.Parse("a1111111-2222-3333-4444-555555555555");
                            orgName = "COMPLIANCE SCHEME LTD";
                            orgRole = "Compliance Scheme";
                            orgNumber = "154977";
                            companiesHouseNumber = "CS_GENERATED_3603716";
                            break;
                    }

                    var body = new
                    {
                        user = new
                        {
                            id = userId ?? Guid.NewGuid().ToString(),
                            firstName = "Test",
                            lastName = userType == StubToken.UserType.DirectProducer ? "Producer" :
                                       userType == StubToken.UserType.CsMemberProducer ? "Member" : "User",
                            email = "test+17122025143216@user.com",
                            roleInOrganisation = "Admin",
                            enrolmentStatus = "Approved",
                            serviceRole = "Approved Person",
                            service = "EPR Packaging",
                            serviceRoleId = 1,
                            telephone = "07774455666",
                            jobTitle = "Director",
                            isChangeRequestPending = false,
                            numberOfOrganisations = 0,
                            organisations = new[]
                            {
                                new
                                {
                                    id = orgId,
                                    name = orgName,
                                    tradingName = (string?)null,
                                    organisationRole = orgRole,
                                    organisationType = "Companies House Company",
                                    OrganisationNumber = orgNumber,
                                    companiesHouseNumber = companiesHouseNumber,
                                    subBuildingName = (string?)null,
                                    buildingName = (string?)null,
                                    buildingNumber = "4",
                                    street = "Address 1",
                                    locality = "Some Road",
                                    dependentLocality = (string?)null,
                                    town = nation.Country,
                                    county = (string?)null,
                                    country = nation.Country,
                                    postcode = "NW1 1HG",
                                    organisationAddress = (string?)null,
                                    jobTitle = (string?)null,
                                    nationId = nation.NationId,
                                    personRoleInOrganisation = (string?)null,
                                    isChangeRequestPending = false,
                                    enrolments = (object?)null
                                }
                            }
                        }
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
                }));

        // GET /api/persons?userId={id}
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/persons")
                .WithParam("userId", true, ".+"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    firstName = "Test",
                    lastName = "User",
                    contactEmail = "test.user@example.com",
                    isDeleted = false
                }));

        // GET /api/persons/all-persons?userId={id}
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/persons/all-persons")
                .WithParam("userId", true, ".+"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    firstName = "Test",
                    lastName = "User",
                    contactEmail = "test.user@example.com",
                    isDeleted = false
                }));

        // GET /api/notifications
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/notifications"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { notifications = Array.Empty<object>() }));

        return server;
    }

}
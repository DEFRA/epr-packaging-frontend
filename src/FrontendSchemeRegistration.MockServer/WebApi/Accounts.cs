namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

[ExcludeFromCodeCoverage]
public static class  Accounts
{
    public static WireMockServer WithAccounts(this WireMockServer server)
    {
        // GET /api/user-accounts 
        server.Given(Request.Create()
                .UsingGet()
                .WithPath("/api/user-accounts"))
            .RespondWith(Response.Create()
                .WithCallback(req =>
                {
                    string? token = null;
                    if (req.Headers != null && req.Headers.TryGetValue("Authorization", out var header))
                    {
                        var authHeader = header.FirstOrDefault();
                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            token = authHeader["Bearer ".Length..].Trim();
                        }
                    }

                    var orgId = string.IsNullOrWhiteSpace(token) ? Guid.NewGuid().ToString() : token;

                    var body = new
                    {
                        user = new
                        {
                            id = "579c319d-d552-47a2-bf4c-5a125a3183bc",
                            firstName = "Test",
                            lastName = "User",
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
                                    name = "SUPER TEST LTD",
                                    tradingName = (string?)null,
                                    organisationRole = "Compliance Scheme",
                                    organisationType = "Companies House Company",
                                    organisationNumber = "154977",
                                    companiesHouseNumber = "CS_GENERATED_3603716",
                                    subBuildingName = (string?)null,
                                    buildingName = (string?)null,
                                    buildingNumber = "4",
                                    street = "Address 1",
                                    locality = "Some Road",
                                    dependentLocality = (string?)null,
                                    town = "London",
                                    county = (string?)null,
                                    country = "England",
                                    postcode = "NW1 1HG",
                                    organisationAddress = (string?)null,
                                    jobTitle = (string?)null,
                                    nationId = 1,
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

        return server;
    }
}
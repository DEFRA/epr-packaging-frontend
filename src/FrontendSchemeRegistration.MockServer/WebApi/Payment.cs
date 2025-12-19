using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;

namespace FrontendSchemeRegistration.MockServer.WebApi;

public static class Payment
{
    public static WireMockServer WithPayments(this WireMockServer server)
    {
        // Compliance scheme registration fee (default + scenario switching)
        MapFeeEndpoint(server, "/compliance-scheme/registration-fee", "WebApi/Responses/payment/compliance-default.json", "WebApi/Responses/payment/compliance-latefee.json");
        MapFeeEndpoint(server, "/v1/compliance-scheme/registration-fee", "WebApi/Responses/payment/compliance-default.json", "WebApi/Responses/payment/compliance-latefee.json");
        MapFeeEndpoint(server, "/v2/compliance-scheme/registration-fee", "WebApi/Responses/payment/compliance-default.json", "WebApi/Responses/payment/compliance-latefee.json");

        return server;
    }

    private static void MapFeeEndpoint(WireMockServer server, string path, string defaultFile, string lateFeeFile)
    {
        // Scenario-specific mapping via querystring
        server.Given(Request.Create()
                .UsingPost()
                .WithPath(path)
                .WithParam("scenario", true, "latefee"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(lateFeeFile));

        // Default mapping (no or unknown scenario)
        server.Given(Request.Create()
                .UsingPost()
                .WithPath(path))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile(defaultFile));
    }
}

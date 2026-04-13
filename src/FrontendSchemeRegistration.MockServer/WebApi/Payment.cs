using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace FrontendSchemeRegistration.MockServer.WebApi;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public static class Payment
{
    public static WireMockServer WithPayments(this WireMockServer server)
    {
        // Initiate online payment (HTML redirect)
        MapInitiatePaymentEndpoint(server, "/online-payments", "WebApi/Responses/Payment/online-payments.html");
        MapInitiatePaymentEndpoint(server, "/v1/online-payments", "WebApi/Responses/Payment/online-payments.html");
        MapInitiatePaymentEndpoint(server, "/v2/online-payments", "WebApi/Responses/Payment/online-payments.html");

        // Producer registration fee
        MapFeeEndpoint(server, "/producer/registration-fee", "WebApi/Responses/Payment/producer-default.json", "WebApi/Responses/Payment/producer-default.json");
        MapFeeEndpoint(server, "/v1/producer/registration-fee", "WebApi/Responses/Payment/producer-default.json", "WebApi/Responses/Payment/producer-default.json");
        MapFeeEndpoint(server, "/v2/producer/registration-fee", "WebApi/Responses/Payment/producer-default.json", "WebApi/Responses/Payment/producer-default.json");

        // Compliance scheme registration fee (default + scenario switching)
        MapFeeEndpoint(server, "/compliance-scheme/registration-fee", "WebApi/Responses/Payment/compliance-default.json", "WebApi/Responses/Payment/compliance-latefee.json");
        MapFeeEndpoint(server, "/v1/compliance-scheme/registration-fee", "WebApi/Responses/Payment/compliance-default.json", "WebApi/Responses/Payment/compliance-latefee.json");
        MapFeeEndpoint(server, "/v2/compliance-scheme/registration-fee", "WebApi/Responses/Payment/compliance-default.json", "WebApi/Responses/Payment/compliance-latefee.json");

        return server;
    }

    private static void MapInitiatePaymentEndpoint(WireMockServer server, string path, string htmlFile)
    {
        server.Given(Request.Create()
                .UsingPost()
                .WithPath(path))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/html; charset=utf-8")
                .WithBodyFromFile(htmlFile));
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

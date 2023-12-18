namespace FrontendSchemeRegistration.UI.Services;

using System.Globalization;
using Application.ClassMaps;
using Application.Services.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Helpers;
using Interfaces;

public class ErrorReportService : IErrorReportService
{
    private readonly IWebApiGatewayClient _webApiGatewayClient;

    public ErrorReportService(IWebApiGatewayClient webApiGatewayClient)
    {
        _webApiGatewayClient = webApiGatewayClient;
    }

    public async Task<Stream> GetErrorReportStreamAsync(Guid submissionId)
    {
        var producerValidationErrors = await _webApiGatewayClient.GetProducerValidationErrorsAsync(submissionId);
        var errorReportRows = producerValidationErrors.ToErrorReportRows();

        var stream = new MemoryStream();

        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.CurrentCulture));

        csv.Context.RegisterClassMap<ErrorReportRowMap>();
        csv.WriteRecordsAsync(errorReportRows);
        await writer.FlushAsync();

        stream.Position = 0;

        return stream;
    }
}
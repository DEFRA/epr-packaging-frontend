namespace FrontendSchemeRegistration.UI.Services;

using System.Globalization;
using Application.ClassMaps;
using Application.Services.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using FrontendSchemeRegistration.UI.Constants;
using Helpers;
using Interfaces;
using Microsoft.FeatureManagement;

public class ErrorReportService : IErrorReportService
{
    private readonly IWebApiGatewayClient _webApiGatewayClient;
    private readonly IFeatureManager _featureManager;

    public ErrorReportService(IWebApiGatewayClient webApiGatewayClient, IFeatureManager featureManager)
    {
        _webApiGatewayClient = webApiGatewayClient;
        _featureManager = featureManager;
    }

    public async Task<Stream> GetErrorReportStreamAsync(Guid submissionId)
    {
        var producerValidationErrors = await _webApiGatewayClient.GetProducerValidationErrorsAsync(submissionId);
        var errorReportRows = producerValidationErrors.ToErrorReportRows();

        var stream = new MemoryStream();

        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.CurrentCulture));

        List<string> optionalHeaders = new List<string>();
        if (await _featureManager.IsEnabledAsync(FeatureFlags.EnableTransitionalPackagingUnitsColumn))
        {
            optionalHeaders.Add("transitional_packaging_units");
        }
        if (await _featureManager.IsEnabledAsync(FeatureFlags.EnableRecyclabilityRatingColumn))
        {
            optionalHeaders.Add("ram_rag_rating");
        }
        csv.Context.RegisterClassMap(new ErrorReportRowMap(optionalHeaders));
        csv.WriteRecordsAsync(errorReportRows);
        await writer.FlushAsync();
        stream.Position = 0;

        return stream;
    }

    public async Task<Stream> GetRegistrationErrorReportStreamAsync(Guid submissionId)
    {
        var registrationValidationErrors = await _webApiGatewayClient.GetRegistrationValidationErrorsAsync(submissionId);
        var registrationErrorReportRows = registrationValidationErrors.ToRegistrationErrorReportRows();

        var stream = new MemoryStream();

        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.CurrentCulture));

        csv.Context.RegisterClassMap<RegistrationErrorReportRowMap>();
        csv.WriteRecordsAsync(registrationErrorReportRows);
        await writer.FlushAsync();

        stream.Position = 0;

        return stream;
    }
}
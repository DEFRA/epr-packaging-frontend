namespace FrontendSchemeRegistration.UI.Services.Interfaces;

public interface IErrorReportService
{
     Task<Stream> GetErrorReportStreamAsync(Guid submissionId);
}
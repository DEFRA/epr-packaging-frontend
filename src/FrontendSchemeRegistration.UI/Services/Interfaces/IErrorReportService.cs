namespace FrontendSchemeRegistration.UI.Services.Interfaces;

public interface IErrorReportService
{
     Task<Stream> GetErrorReportStreamAsync(Guid submissionId);

     Task<Stream> GetRegistrationErrorReportStreamAsync(Guid submissionId);
}
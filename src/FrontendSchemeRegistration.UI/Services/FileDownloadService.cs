using FrontendSchemeRegistration.Application.Enums;
using FrontendSchemeRegistration.Application.Services.Interfaces;
using FrontendSchemeRegistration.UI.Services.Interfaces;

namespace FrontendSchemeRegistration.UI.Services
{
    public class FileDownloadService : IFileDownloadService
    {
        private readonly IWebApiGatewayClient _webApiGatewayClient;

        public FileDownloadService(IWebApiGatewayClient webApiGatewayClient)
        {
            _webApiGatewayClient = webApiGatewayClient;
        }

        public async Task<byte[]?> GetFileAsync(Guid fileId, string fileName, SubmissionType submissionType, Guid submissionId)
        {
            var queryString = $"fileName={fileName}"
                + $"&fileid={fileId}"
                + $"&submissiontype={submissionType}"
                + $"&submissionid={submissionId}";

            return await _webApiGatewayClient.FileDownloadAsync(queryString);
        }
    }
}

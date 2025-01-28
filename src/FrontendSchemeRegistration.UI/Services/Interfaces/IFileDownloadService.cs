using FrontendSchemeRegistration.Application.Enums;

namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IFileDownloadService
    {
        Task<byte[]?> GetFileAsync(Guid fileId, string fileName, SubmissionType submissionType, Guid submissionId);
    }
}

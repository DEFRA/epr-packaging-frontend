using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus
{
    [ExcludeFromCodeCoverage]
    public class UploadFileErrorModel
    {
        public int FileLineNumber { get; set; }

        public string FileContent { get; set; }

        public string Message { get; set; }

        public bool IsError { get; set; }

        public int ErrorNumber { get; set; }
    }
}

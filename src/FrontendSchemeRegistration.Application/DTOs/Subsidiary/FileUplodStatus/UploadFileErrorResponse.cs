using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.Application.DTOs.Subsidiary.FileUploadStatus
{
    [ExcludeFromCodeCoverage]
    public class UploadFileErrorResponse
    {
        public string Status { get; set; }
        public int? RowsAdded { get; set; }
        public List<UploadFileErrorModel> Errors { get; set; }
    }
}

using FrontendSchemeRegistration.UI.Resources.Views.FileUpload;
using FrontendSchemeRegistration.UI.Services.Interfaces;

namespace FrontendSchemeRegistration.UI.Services.Messages
{
    public class SubsidiaryFileUploadMessages : IFileUploadMessages
    {
        public string SelectACsvFile => FileUpload.no_file_uploaded;
        public string TheSelectedFileMustBeACsv => FileUpload.incorrect_file_type;
        public string TheSelectedFileIsEmpty => FileUpload.file_did_not_contain_any_data;
        public string TheSelectedFileMustBeSmallerThan => FileUpload.file_size_exceeded_max_allowed;
    }
}

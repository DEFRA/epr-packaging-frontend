using FrontendSchemeRegistration.UI.Resources.Views.FileUpload;
using FrontendSchemeRegistration.UI.Services.Interfaces;

namespace FrontendSchemeRegistration.UI.Services.Messages
{
    public class DefaultFileUploadMessages : IFileUploadMessages
    {
        public string SelectACsvFile => FileUpload.select_a_csv_file;
        public string TheSelectedFileMustBeACsv => FileUpload.the_selected_file_must_be_a_csv;
        public string TheSelectedFileIsEmpty => FileUpload.the_selected_file_is_empty;
        public string TheSelectedFileMustBeSmallerThan => FileUpload.the_selected_file_must_be_smaller_than;
    }

}

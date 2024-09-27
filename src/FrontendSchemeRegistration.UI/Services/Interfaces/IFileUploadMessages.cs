namespace FrontendSchemeRegistration.UI.Services.Interfaces
{
    public interface IFileUploadMessages
    {
        string SelectACsvFile { get; }
        string TheSelectedFileMustBeACsv { get; }
        string TheSelectedFileIsEmpty { get; }
        string TheSelectedFileMustBeSmallerThan { get; }
    }

}

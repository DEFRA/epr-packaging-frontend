namespace FrontendSchemeRegistration.UI.Extensions;

using Application.Constants;

public static class ListExtension
{
    public static void AddIfNotExists<T>(this List<T> source, T value)
    {
        if (!source.Contains(value))
        {
            source.Add(value);
        }
    }

    public static T? PreviousOrDefault<T>(this List<T?> list, T value)
    {
        var index = list.LastIndexOf(value);
        return index > 0 ? list[index - 1] : default(T);
    }
    
    public static void ClearReportPackagingDataJourney<T>(this List<T> source)
        where T : IComparable<string>
    {
        List<string> reportPackagingDataJourney = new List<string> { PagePaths.FileUploadSubLanding, PagePaths.FileUpload, PagePaths.FileUploading, PagePaths.FileUploadSuccess, PagePaths.FileUploadFailure };

        source.RemoveAll(item => reportPackagingDataJourney.Contains(item.ToString()));
    }
}

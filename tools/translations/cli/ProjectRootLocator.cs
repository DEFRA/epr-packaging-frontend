namespace Translations;

internal static class ProjectRootLocator
{
    public static string Find(string? suppliedProjectRoot)
    {
        if (!string.IsNullOrWhiteSpace(suppliedProjectRoot))
        {
            return Path.GetFullPath(suppliedProjectRoot);
        }

        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);

            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "src", "FrontendSchemeRegistration.UI")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root. Pass --project-root explicitly.");
    }
}

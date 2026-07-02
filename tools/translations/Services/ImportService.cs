using Translations.Configuration;
using Translations.Models;

namespace Translations.Services;

internal sealed class ImportService
{
    public async Task<int> ImportAsync(string projectRoot, TranslationProfile profile, string? inputPath)
    {
        var resolvedInputPath = PathHelpers.ResolvePath(projectRoot, inputPath ?? profile.DefaultOutputPath);
        var translatedRows = await GetTranslatedRowsAsync(resolvedInputPath);
        var nonBlankRows = translatedRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Welsh))
            .ToArray();

        var updatesByTargetFile = BuildUpdatesByTargetFile(projectRoot, profile, nonBlankRows);
        var updatedFileCount = 0;

        foreach (var (targetFile, updates) in updatesByTargetFile.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (ResxResourceFile.UpdateValues(targetFile, updates))
            {
                updatedFileCount++;
                Console.WriteLine($"Updated {targetFile} ({updates.Count} value{Plural(updates.Count)})");
            }
        }

        Console.WriteLine($"Imported {nonBlankRows.Length} translated value{Plural(nonBlankRows.Length)}");
        Console.WriteLine($"Updated {updatedFileCount} resource file{Plural(updatedFileCount)}");
        return 0;
    }

    private static async Task<IReadOnlyList<TranslatedWorkbookRow>> GetTranslatedRowsAsync(string inputPath)
    {
        var workbookPaths = GetWorkbookPaths(inputPath);
        var rows = new List<TranslatedWorkbookRow>();

        foreach (var workbookPath in workbookPaths)
        {
            rows.AddRange(await XlsxWorkbookReader.ReadTranslatedRowsAsync(workbookPath));
        }

        RejectConflictingTranslations(rows);
        return rows;
    }

    private static IReadOnlyList<string> GetWorkbookPaths(string inputPath)
    {
        if (File.Exists(inputPath))
        {
            return [inputPath];
        }

        if (!Directory.Exists(inputPath))
        {
            throw new DirectoryNotFoundException($"Input path \"{inputPath}\" does not exist.");
        }

        return Directory
            .EnumerateFiles(inputPath, "*.xlsx", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, Dictionary<string, string>> BuildUpdatesByTargetFile(
        string projectRoot,
        TranslationProfile profile,
        IReadOnlyList<TranslatedWorkbookRow> translatedRows)
    {
        var updatesByTargetFile = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in translatedRows)
        {
            var resourceKey = ResourceKey.Parse(row.TranslationKey);
            var sourceResourcePath = resourceKey.ResourceFile;
            var sourceResourceFullPath = PathHelpers.ResolvePath(projectRoot, sourceResourcePath);

            if (!File.Exists(sourceResourceFullPath))
            {
                throw new FileNotFoundException($"Source RESX file in translation key was not found: {sourceResourcePath}", sourceResourceFullPath);
            }

            var sourceEntries = ResxResourceFile.Read(sourceResourceFullPath);
            if (!sourceEntries.ContainsKey(resourceKey.Key))
            {
                throw new InvalidOperationException($"Source RESX file \"{sourceResourcePath}\" does not contain key \"{resourceKey.Key}\".");
            }

            var targetResourcePath = PathHelpers.ToTargetCulturePath(sourceResourcePath, profile.SourceCulture, profile.TargetCulture);
            var targetResourceFullPath = PathHelpers.ResolvePath(projectRoot, targetResourcePath);

            if (!updatesByTargetFile.TryGetValue(targetResourceFullPath, out var updates))
            {
                updates = new Dictionary<string, string>(StringComparer.Ordinal);
                updatesByTargetFile[targetResourceFullPath] = updates;
            }

            updates[resourceKey.Key] = row.Welsh;
        }

        return updatesByTargetFile;
    }

    private static void RejectConflictingTranslations(IEnumerable<TranslatedWorkbookRow> translatedRows)
    {
        var valuesByKey = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in translatedRows)
        {
            if (string.IsNullOrWhiteSpace(row.Welsh))
            {
                continue;
            }

            if (valuesByKey.TryGetValue(row.TranslationKey, out var existingValue) &&
                !string.Equals(existingValue, row.Welsh, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Conflicting Welsh values found for translation key \"{row.TranslationKey}\".");
            }

            valuesByKey[row.TranslationKey] = row.Welsh;
        }
    }

    private static string Plural(int count) => count == 1 ? string.Empty : "s";
}

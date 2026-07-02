using Translations.Configuration;
using Translations.Models;

namespace Translations.Services;

internal sealed class ExportService
{
    private static readonly string[] DefaultTranslatorInstructions =
    [
        "Preserve placeholders such as {0}, {1}, and format suffixes such as {2:d MMMM yyyy}.",
        "Preserve inline HTML tags such as <strong>...</strong> where they appear."
    ];

    public async Task<int> ExportAsync(string projectRoot, TranslationProfile profile, string? outputPath)
    {
        var resolvedOutputPath = PathHelpers.ResolvePath(projectRoot, outputPath ?? profile.DefaultOutputPath);
        Directory.CreateDirectory(resolvedOutputPath);

        var groups = BuildPageTranslationGroups(projectRoot, profile);
        var totalRows = 0;

        foreach (var group in groups)
        {
            var workbookPath = Path.Combine(resolvedOutputPath, group.FileName);
            var instructions = DefaultTranslatorInstructions
                .Concat(profile.TranslatorInstructions)
                .Concat(group.TranslatorNotes)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            await XlsxWorkbookWriter.WriteAsync(workbookPath, group, instructions);
            totalRows += group.Rows.Count;
            Console.WriteLine($"Created {workbookPath} ({group.Rows.Count} row{Plural(group.Rows.Count)})");
        }

        Console.WriteLine($"Created {groups.Count} translation workbook{Plural(groups.Count)}");
        Console.WriteLine($"Included {totalRows} translation row{Plural(totalRows)}");
        return 0;
    }

    private static IReadOnlyList<PageTranslationGroup> BuildPageTranslationGroups(string projectRoot, TranslationProfile profile)
    {
        var owners = new Dictionary<string, PageProfile>(StringComparer.OrdinalIgnoreCase);
        var groups = new List<PageTranslationGroup>();

        foreach (var page in profile.Pages)
        {
            var rows = new List<TranslationRow>();
            var reusedOwners = new Dictionary<string, PageProfile>(StringComparer.OrdinalIgnoreCase);

            foreach (var resource in page.Resources)
            {
                var sourceResourcePath = PathHelpers.Normalize(resource.Source);
                var sourceResourceFullPath = PathHelpers.ResolvePath(projectRoot, sourceResourcePath);
                var targetResourceFullPath = PathHelpers.ResolvePath(
                    projectRoot,
                    PathHelpers.ToTargetCulturePath(sourceResourcePath, profile.SourceCulture, profile.TargetCulture));

                var sourceEntries = ResxResourceFile.Read(sourceResourceFullPath);
                var targetEntries = ResxResourceFile.ReadIfExists(targetResourceFullPath);
                var selectedKeys = SelectKeys(sourceEntries, resource).ToArray();

                foreach (var key in selectedKeys)
                {
                    var resourceKey = new ResourceKey(sourceResourcePath, key);
                    if (owners.TryGetValue(resourceKey.TranslationKey, out var owner))
                    {
                        if (!string.Equals(owner.Id, page.Id, StringComparison.Ordinal))
                        {
                            reusedOwners[owner.Id] = owner;
                        }

                        continue;
                    }

                    owners[resourceKey.TranslationKey] = page;
                    var english = sourceEntries[key];
                    var existingWelsh = targetEntries.GetValueOrDefault(key);
                    var welsh = !string.IsNullOrWhiteSpace(existingWelsh) && !string.Equals(existingWelsh, english, StringComparison.Ordinal)
                        ? existingWelsh
                        : string.Empty;

                    rows.Add(new TranslationRow(
                        resourceKey,
                        page.Id,
                        page.Route,
                        string.IsNullOrWhiteSpace(resource.Section) ? page.Notes : resource.Section,
                        english,
                        welsh,
                        page.FigmaUrl,
                        BuildContext(page, resource)));
                }
            }

            var translatorNotes = page.TranslatorNotes
                .Concat(BuildReusedContentTranslatorNotes(reusedOwners.Values))
                .ToArray();

            groups.Add(new PageTranslationGroup(
                page.Id,
                string.IsNullOrWhiteSpace(page.FileName) ? $"{page.Id}.xlsx" : page.FileName,
                page.Route,
                page.Notes,
                page.FigmaUrl,
                translatorNotes,
                rows));
        }

        return groups;
    }

    private static IEnumerable<string> SelectKeys(IReadOnlyDictionary<string, string> entries, ResourceSelection resource)
    {
        if (resource.Keys.Count == 0 && resource.KeyPrefixes.Count == 0)
        {
            return entries.Keys;
        }

        var selected = new HashSet<string>(resource.Keys, StringComparer.Ordinal);

        foreach (var prefix in resource.KeyPrefixes)
        {
            foreach (var key in entries.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
            {
                selected.Add(key);
            }
        }

        var missingKeys = resource.Keys.Where(key => !entries.ContainsKey(key)).ToArray();
        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Resource \"{resource.Source}\" does not contain configured key(s): {string.Join(", ", missingKeys)}.");
        }

        return entries.Keys.Where(selected.Contains);
    }

    private static string BuildContext(PageProfile page, ResourceSelection resource)
    {
        var contextParts = new List<string>
        {
            page.Notes
        };

        if (!string.IsNullOrWhiteSpace(resource.Section))
        {
            contextParts.Add(resource.Section);
        }

        if (page.FeatureFlags.Count > 0)
        {
            contextParts.Add($"Feature flag(s): {string.Join(", ", page.FeatureFlags)}");
        }

        if (page.AppSettings.Count > 0)
        {
            contextParts.Add($"App setting(s): {string.Join(", ", page.AppSettings.Select(setting => $"{setting.Key} - {setting.Value}"))}");
        }

        return string.Join(Environment.NewLine, contextParts);
    }

    private static IEnumerable<string> BuildReusedContentTranslatorNotes(IEnumerable<PageProfile> reusedOwners)
    {
        var ownerFiles = reusedOwners
            .Select(owner => owner.FileName)
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return ownerFiles.Length == 0
            ? []
            : [$"Reusable content rendered on this page is translated in: {string.Join(", ", ownerFiles)}."];
    }

    private static string Plural(int count) => count == 1 ? string.Empty : "s";
}

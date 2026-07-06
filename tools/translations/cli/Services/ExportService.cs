using System.Text.Encodings.Web;
using System.Text.Json;
using Translations.Configuration;
using Translations.Models;

namespace Translations.Services;

internal static class ExportService
{
    private const string WorkbookOutputDirectoryName = "xlsx";
    private const string TextExportOutputDirectoryName = "json";

    private static readonly string[] DefaultTranslatorInstructions =
    [
        "Preserve placeholders such as {0}, {1}, and format suffixes such as {2:d MMMM yyyy}.",
        "Preserve inline HTML tags such as <strong>...</strong> where they appear."
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<int> ExportAsync(string projectRoot, TranslationProfile profile, string? outputPath)
    {
        var resolvedOutputPath = PathHelpers.ResolvePath(projectRoot, outputPath ?? profile.DefaultOutputPath);
        Directory.CreateDirectory(resolvedOutputPath);
        var workbookOutputDirectory = Path.Combine(resolvedOutputPath, WorkbookOutputDirectoryName);
        var textExportOutputDirectory = Path.Combine(resolvedOutputPath, TextExportOutputDirectoryName);
        Directory.CreateDirectory(workbookOutputDirectory);
        Directory.CreateDirectory(textExportOutputDirectory);

        var groups = BuildPageTranslationGroups(projectRoot, profile);
        var workbookCounts = new ExportStatusCounts();
        var textExportCounts = new ExportStatusCounts();
        var skippedPageCount = 0;
        var totalRows = 0;

        foreach (var group in groups)
        {
            var workbookPath = Path.Combine(workbookOutputDirectory, group.FileName);
            var textExportPath = Path.Combine(textExportOutputDirectory, GetTextExportFileName(group.FileName));

            if (group.Rows.Count == 0)
            {
                if (File.Exists(workbookPath))
                {
                    File.Delete(workbookPath);
                }

                if (File.Exists(textExportPath))
                {
                    File.Delete(textExportPath);
                }

                skippedPageCount++;
                Console.WriteLine($"Skipped {group.Id}: no translation entries to include in this page; no workbook generated.");
                continue;
            }

            var instructions = DefaultTranslatorInstructions
                .Concat(profile.TranslatorInstructions)
                .Concat(group.TranslatorNotes)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var workbookExportData = BuildWorkbookExportData(group, instructions);
            var textExport = BuildTextExport(group, instructions);
            var workbookStatus = await WriteWorkbookIfChangedAsync(workbookPath, group, instructions, workbookExportData);
            var textExportStatus = await WriteTextExportIfChangedAsync(textExportPath, textExport);

            workbookCounts.Add(workbookStatus);
            textExportCounts.Add(textExportStatus);
            totalRows += group.Rows.Count;
            Console.WriteLine($"Processed {workbookPath} ({group.Rows.Count} row{Plural(group.Rows.Count)}; workbook: {workbookStatus}; JSON: {textExportStatus})");
        }

        Console.WriteLine($"Workbooks: {workbookCounts}");
        Console.WriteLine($"JSON sidecars: {textExportCounts}");
        Console.WriteLine($"Skipped {skippedPageCount} page{Plural(skippedPageCount)} with no translation entries");
        Console.WriteLine($"Included {totalRows} translation row{Plural(totalRows)}");
        return 0;
    }

    private static async Task<string> WriteWorkbookIfChangedAsync(
        string workbookPath,
        PageTranslationGroup group,
        IReadOnlyList<string> instructions,
        WorkbookExportData expectedExportData)
    {
        var outputExists = File.Exists(workbookPath);
        if (outputExists && await WorkbookMatchesExportDataAsync(workbookPath, expectedExportData))
        {
            return ExportStatus.Unchanged;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(workbookPath)!);
        await XlsxWorkbookWriter.WriteAsync(workbookPath, group, instructions);
        return outputExists ? ExportStatus.Updated : ExportStatus.Created;
    }

    private static async Task<bool> WorkbookMatchesExportDataAsync(string workbookPath, WorkbookExportData expectedExportData)
    {
        try
        {
            var actualExportData = await XlsxWorkbookReader.ReadExportDataAsync(workbookPath);
            return ExportDataEquals(actualExportData, expectedExportData);
        }
        catch
        {
            return false;
        }
    }

    private static bool ExportDataEquals(WorkbookExportData actual, WorkbookExportData expected)
    {
        return actual.TranslatorNotes.SequenceEqual(expected.TranslatorNotes, StringComparer.Ordinal) &&
               actual.Rows.SequenceEqual(expected.Rows);
    }

    private static async Task<string> WriteTextExportIfChangedAsync(string textExportPath, TextExport textExport)
    {
        var outputExists = File.Exists(textExportPath);
        var content = $"{JsonSerializer.Serialize(textExport, JsonOptions)}\n";

        if (outputExists && string.Equals(await File.ReadAllTextAsync(textExportPath), content, StringComparison.Ordinal))
        {
            return ExportStatus.Unchanged;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(textExportPath)!);
        await File.WriteAllTextAsync(textExportPath, content);
        return outputExists ? ExportStatus.Updated : ExportStatus.Created;
    }

    private static WorkbookExportData BuildWorkbookExportData(PageTranslationGroup group, IReadOnlyList<string> instructions)
    {
        return new WorkbookExportData(
            instructions,
            group.Rows
                .Select(row => new WorkbookExportRow(
                    row.ResourceKey.TranslationKey,
                    row.ResourceKey.ResourceFile,
                    row.ResourceKey.Key,
                    row.PageId,
                    row.Route,
                    row.English,
                    row.Welsh,
                    row.FigmaUrl ?? string.Empty))
                .ToArray());
    }

    private static TextExport BuildTextExport(PageTranslationGroup group, IReadOnlyList<string> instructions)
    {
        return new TextExport(
            instructions,
            group.Rows
                .Select(row => new TextExportRow(
                    row.ResourceKey.TranslationKey,
                    row.ResourceKey.ResourceFile,
                    row.ResourceKey.Key,
                    row.PageId,
                    row.Route,
                    row.Section,
                    row.English,
                    row.Welsh,
                    row.FigmaUrl ?? string.Empty,
                    row.Context))
                .ToArray());
    }

    private static string GetTextExportFileName(string workbookFileName)
    {
        return $"{Path.GetFileNameWithoutExtension(workbookFileName)}.json";
    }

    private static List<PageTranslationGroup> BuildPageTranslationGroups(string projectRoot, TranslationProfile profile)
    {
        var owners = new Dictionary<string, PageProfile>(StringComparer.OrdinalIgnoreCase);
        var groups = new List<PageTranslationGroup>();
        var invalidEnglishValues = new SortedSet<string>(StringComparer.Ordinal);
        var context = new ExportBuildContext(projectRoot, profile, owners, invalidEnglishValues);

        foreach (var page in profile.Pages)
        {
            groups.Add(BuildPageTranslationGroup(context, page));
        }

        if (invalidEnglishValues.Count > 0)
        {
            throw new InvalidOperationException(
                $"English translation values must not include leading or trailing whitespace. Move spacing into the layout before exporting translations:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", invalidEnglishValues)}");
        }

        return groups;
    }

    private static PageTranslationGroup BuildPageTranslationGroup(ExportBuildContext context, PageProfile page)
    {
        var pageContext = new PageBuildContext(
            page,
            new Dictionary<string, PageProfile>(StringComparer.OrdinalIgnoreCase),
            []);

        foreach (var resource in page.Resources)
        {
            AddResourceRows(context, pageContext, resource);
        }

        var translatorNotes = page.TranslatorNotes
            .Concat(BuildReusedContentTranslatorNotes(pageContext.ReusedOwners.Values))
            .ToArray();

        return new PageTranslationGroup(
            page.Id,
            string.IsNullOrWhiteSpace(page.FileName) ? $"{page.Id}.xlsx" : page.FileName,
            page.Route,
            page.Notes,
            page.FigmaUrl,
            translatorNotes,
            pageContext.Rows);
    }

    private static void AddResourceRows(ExportBuildContext context, PageBuildContext pageContext, ResourceSelection resource)
    {
        var resourceEntries = ReadResourceEntries(context, resource);
        var selectedKeys = SelectKeys(resourceEntries.SourceEntries, resource).ToArray();
        AddInvalidExportValues(
            context.InvalidEnglishValues,
            resourceEntries.SourceResourcePath,
            resourceEntries.SourceEntries,
            selectedKeys);

        foreach (var key in selectedKeys)
        {
            AddResourceRow(context, pageContext, resource, resourceEntries, key);
        }
    }

    private static ResourceEntries ReadResourceEntries(ExportBuildContext context, ResourceSelection resource)
    {
        var sourceResourcePath = PathHelpers.Normalize(resource.Source);
        var sourceResourceFullPath = PathHelpers.ResolvePath(context.ProjectRoot, sourceResourcePath);
        var targetResourceFullPath = PathHelpers.ResolvePath(
            context.ProjectRoot,
            PathHelpers.ToTargetCulturePath(sourceResourcePath, context.Profile.SourceCulture, context.Profile.TargetCulture));

        return new ResourceEntries(
            sourceResourcePath,
            ResxResourceFile.Read(sourceResourceFullPath),
            ResxResourceFile.ReadIfExists(targetResourceFullPath));
    }

    private static void AddResourceRow(
        ExportBuildContext context,
        PageBuildContext pageContext,
        ResourceSelection resource,
        ResourceEntries resourceEntries,
        string key)
    {
        var page = pageContext.Page;
        var resourceKey = new ResourceKey(resourceEntries.SourceResourcePath, key);
        if (IsOwnedByEarlierPage(resourceKey, pageContext, context.Owners))
        {
            return;
        }

        context.Owners[resourceKey.TranslationKey] = page;
        var english = resourceEntries.SourceEntries[key];

        pageContext.Rows.Add(new TranslationRow(
            resourceKey,
            page.Id,
            page.Route,
            string.IsNullOrWhiteSpace(resource.Section) ? page.Notes : resource.Section,
            english,
            GetExistingWelshValue(resourceEntries.TargetEntries, key, english),
            page.FigmaUrl,
            BuildContext(page, resource)));
    }

    private static bool IsOwnedByEarlierPage(
        ResourceKey resourceKey,
        PageBuildContext pageContext,
        Dictionary<string, PageProfile> owners)
    {
        if (!owners.TryGetValue(resourceKey.TranslationKey, out var owner))
        {
            return false;
        }

        if (!string.Equals(owner.Id, pageContext.Page.Id, StringComparison.Ordinal))
        {
            pageContext.ReusedOwners[owner.Id] = owner;
        }

        return true;
    }

    private static string GetExistingWelshValue(IReadOnlyDictionary<string, string> targetEntries, string key, string english)
    {
        var existingWelsh = targetEntries.GetValueOrDefault(key);
        return !string.IsNullOrWhiteSpace(existingWelsh) && !string.Equals(existingWelsh, english, StringComparison.Ordinal)
            ? existingWelsh
            : string.Empty;
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

    private static void AddInvalidExportValues(
        SortedSet<string> invalidEnglishValues,
        string sourceResourcePath,
        IReadOnlyDictionary<string, string> sourceEntries,
        IEnumerable<string> selectedKeys)
    {
        foreach (var key in selectedKeys)
        {
            var value = sourceEntries[key];
            if (!string.Equals(value.Trim(), value, StringComparison.Ordinal))
            {
                invalidEnglishValues.Add($"{sourceResourcePath}::{key}");
            }
        }
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

    private static class ExportStatus
    {
        public const string Created = "created";
        public const string Updated = "updated";
        public const string Unchanged = "unchanged";
    }

    private sealed class ExportStatusCounts
    {
        private int Created { get; set; }

        private int Updated { get; set; }

        private int Unchanged { get; set; }

        public void Add(string status)
        {
            switch (status)
            {
                case ExportStatus.Created:
                    Created++;
                    break;
                case ExportStatus.Updated:
                    Updated++;
                    break;
                case ExportStatus.Unchanged:
                    Unchanged++;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown export status \"{status}\".");
            }
        }

        public override string ToString()
        {
            return $"created {Created}, updated {Updated}, unchanged {Unchanged}";
        }
    }

    private sealed record TextExport(
        IReadOnlyList<string> TranslatorNotes,
        IReadOnlyList<TextExportRow> Rows);

    private sealed record TextExportRow(
        string TranslationKey,
        string ResourceFile,
        string ResourceKey,
        string PageId,
        string Route,
        string Section,
        string English,
        string Welsh,
        string FigmaUrl,
        string Context);

    private sealed record ExportBuildContext(
        string ProjectRoot,
        TranslationProfile Profile,
        Dictionary<string, PageProfile> Owners,
        SortedSet<string> InvalidEnglishValues);

    private sealed record PageBuildContext(
        PageProfile Page,
        Dictionary<string, PageProfile> ReusedOwners,
        List<TranslationRow> Rows);

    private sealed record ResourceEntries(
        string SourceResourcePath,
        IReadOnlyDictionary<string, string> SourceEntries,
        IReadOnlyDictionary<string, string> TargetEntries);
}

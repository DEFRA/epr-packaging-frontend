namespace Translations.Models;

internal sealed record WorkbookExportData(
    IReadOnlyList<string> TranslatorNotes,
    IReadOnlyList<WorkbookExportRow> Rows);

internal sealed record WorkbookExportRow(
    string TranslationKey,
    string ResourceFile,
    string ResourceKey,
    string PageId,
    string Route,
    string English,
    string Welsh,
    string FigmaUrl);

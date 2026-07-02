namespace Translations.Models;

internal sealed record PageTranslationGroup(
    string Id,
    string FileName,
    string Route,
    string Notes,
    string FigmaUrl,
    IReadOnlyList<string> TranslatorNotes,
    IReadOnlyList<TranslationRow> Rows);

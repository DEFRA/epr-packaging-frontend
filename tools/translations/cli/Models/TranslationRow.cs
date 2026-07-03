namespace Translations.Models;

internal sealed record TranslationRow(
    ResourceKey ResourceKey,
    string PageId,
    string Route,
    string Section,
    string English,
    string Welsh,
    string? FigmaUrl,
    string Context);

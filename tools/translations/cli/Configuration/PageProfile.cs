namespace Translations.Configuration;

internal sealed class PageProfile
{
    public string Id { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string Route { get; init; } = string.Empty;

    public string View { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string? FigmaUrl { get; init; }

    public IReadOnlyList<string> FeatureFlags { get; init; } = [];

    public IReadOnlyDictionary<string, string> AppSettings { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> TranslatorNotes { get; init; } = [];

    public IReadOnlyList<ResourceSelection> Resources { get; init; } = [];
}

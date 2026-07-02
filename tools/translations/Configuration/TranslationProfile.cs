namespace Translations.Configuration;

internal sealed class TranslationProfile
{
    public string Name { get; init; } = string.Empty;

    public string SourceCulture { get; init; } = "en";

    public string TargetCulture { get; init; } = "cy";

    public string DefaultOutputPath { get; init; } = "translations/welsh-translations";

    public IReadOnlyList<string> TranslatorInstructions { get; init; } = [];

    public IReadOnlyList<PageProfile> Pages { get; init; } = [];
}

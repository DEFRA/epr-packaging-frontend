namespace Translations.Models;

internal sealed record ResourceKey(string ResourceFile, string Key)
{
    private const string Separator = "::";

    public string TranslationKey => $"{PathHelpers.Normalize(ResourceFile)}{Separator}{Key}";

    public static ResourceKey Parse(string translationKey)
    {
        var parts = translationKey.Split(Separator, 2, StringSplitOptions.None);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new InvalidOperationException($"Translation key \"{translationKey}\" must use the format resource-file::{nameof(Key).ToLowerInvariant()}.");
        }

        return new ResourceKey(PathHelpers.Normalize(parts[0]), parts[1]);
    }
}

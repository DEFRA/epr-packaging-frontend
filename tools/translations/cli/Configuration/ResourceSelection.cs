namespace Translations.Configuration;

internal sealed class ResourceSelection
{
    public string Source { get; init; } = string.Empty;

    public string Section { get; init; } = string.Empty;

    public IReadOnlyList<string> Keys { get; init; } = [];

    public IReadOnlyList<string> KeyPrefixes { get; init; } = [];
}

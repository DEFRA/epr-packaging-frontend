using System.Text.Json;

namespace Translations.Configuration;

internal static class ProfileLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static TranslationProfile Load(string projectRoot, string profile)
    {
        var profilePath = ResolveProfilePath(projectRoot, profile);
        if (!File.Exists(profilePath))
        {
            throw new FileNotFoundException($"Profile \"{profile}\" was not found.", profilePath);
        }

        var json = File.ReadAllText(profilePath);
        var loadedProfile = JsonSerializer.Deserialize<TranslationProfile>(json, Options)
                            ?? throw new InvalidOperationException($"Profile \"{profilePath}\" could not be read.");

        if (loadedProfile.Pages.Count == 0)
        {
            throw new InvalidOperationException($"Profile \"{profilePath}\" does not define any pages.");
        }

        return loadedProfile;
    }

    private static string ResolveProfilePath(string projectRoot, string profile)
    {
        if (profile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(profile, projectRoot);
        }

        return Path.Combine(projectRoot, "tools", "translations", "profiles", $"{profile}.json");
    }
}

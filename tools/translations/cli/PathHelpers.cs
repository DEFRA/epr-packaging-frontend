namespace Translations;

internal static class PathHelpers
{
    public static string Normalize(string path) => path.Replace('\\', '/');

    public static string ResolvePath(string projectRoot, string path)
    {
        return Path.GetFullPath(path, projectRoot);
    }

    public static string ToTargetCulturePath(string sourcePath, string sourceCulture, string targetCulture)
    {
        var normalizedPath = Normalize(sourcePath);
        var sourceSuffix = $".{sourceCulture}.resx";

        if (normalizedPath.EndsWith(sourceSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(normalizedPath.AsSpan(0, normalizedPath.Length - sourceSuffix.Length), $".{targetCulture}.resx");
        }

        if (normalizedPath.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(normalizedPath.AsSpan(0, normalizedPath.Length - ".resx".Length), $".{targetCulture}.resx");
        }

        throw new InvalidOperationException($"Resource file \"{sourcePath}\" is not a .resx file.");
    }
}

using Translations.Configuration;
using Translations.Services;

namespace Translations;

internal static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
        {
            WriteUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var options = CommandOptions.Parse(args.Skip(1).ToArray());
        var projectRoot = ProjectRootLocator.Find(options.ProjectRoot);
        var profile = ProfileLoader.Load(projectRoot, options.Profile);

        try
        {
            return command switch
            {
                "export" => await ExportService.ExportAsync(projectRoot, profile, options.Output),
                "import" => await ImportService.ImportAsync(projectRoot, profile, options.Input),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command \"{command}\".");
        WriteUsage();
        return 1;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("Translation workbook export/import tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools/translations/cli/cli.csproj -- export --profile csoc [--output translations/welsh-translations/csoc]");
        Console.WriteLine("  dotnet run --project tools/translations/cli/cli.csproj -- import --profile csoc [--input translations/welsh-translations/csoc]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --profile       Profile name or profile JSON path. Defaults to csoc.");
        Console.WriteLine("  --output        Export output directory. Defaults to the profile defaultOutputPath.");
        Console.WriteLine("  --input         Import workbook or directory. Defaults to the profile defaultOutputPath.");
        Console.WriteLine("  --project-root  Repository root. Defaults to auto-detection from the current directory.");
    }
}

namespace Translations;

internal sealed class CommandOptions
{
    public string Profile { get; private init; } = "csoc";

    public string? Output { get; private init; }

    public string? Input { get; private init; }

    public string? ProjectRoot { get; private init; }

    public static CommandOptions Parse(IReadOnlyList<string> args)
    {
        var profile = "csoc";
        string? output = null;
        string? input = null;
        string? projectRoot = null;

        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            switch (option)
            {
                case "--profile":
                    profile = ReadValue(args, ref index, option);
                    break;
                case "--output":
                    output = ReadValue(args, ref index, option);
                    break;
                case "--input":
                    input = ReadValue(args, ref index, option);
                    break;
                case "--project-root":
                    projectRoot = ReadValue(args, ref index, option);
                    break;
                default:
                    throw new ArgumentException($"Unknown option \"{option}\".");
            }
        }

        return new CommandOptions
        {
            Profile = profile,
            Output = output,
            Input = input,
            ProjectRoot = projectRoot
        };
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string option)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Missing value for {option}.");
        }

        index++;
        return args[index];
    }
}

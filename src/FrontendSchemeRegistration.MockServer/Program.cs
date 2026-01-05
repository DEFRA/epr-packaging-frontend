using System.Diagnostics.CodeAnalysis;

namespace FrontendSchemeRegistration.MockServer;

[ExcludeFromCodeCoverage]
public static class Program
{
    private static void Main()
    {
        Console.WriteLine("FrontendSchemeRegistration.MockServer starting on http://localhost:9091");

        MockApiServer.Start();

        Console.WriteLine("Press any key to stop.");
        Console.ReadKey();
    }
}
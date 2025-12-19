namespace FrontendSchemeRegistration.MockServer;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine($"FrontendSchemeRegistration.MockServer starting on http://localhost:9091");

        MockApiServer.Start();

        Console.WriteLine("Press any key to stop.");
        Console.ReadKey();
    }
}

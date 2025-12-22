using System.Diagnostics.CodeAnalysis;
using FrontendSchemeRegistration.MockServer;

[assembly:ExcludeFromCodeCoverage]
Console.WriteLine("FrontendSchemeRegistration.MockServer starting on http://localhost:9091");

MockApiServer.Start();

Console.WriteLine("Press any key to stop.");
Console.ReadKey();
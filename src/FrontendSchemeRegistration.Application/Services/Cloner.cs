namespace FrontendSchemeRegistration.Application.Services;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Interfaces;

[ExcludeFromCodeCoverage]
public class Cloner : ICloner
{
    public T Clone<T>(T source)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source));
    }
}
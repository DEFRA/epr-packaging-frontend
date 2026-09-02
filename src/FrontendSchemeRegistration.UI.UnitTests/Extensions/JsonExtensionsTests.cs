namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using System.Text.Json;
using FluentAssertions;
using FrontendSchemeRegistration.UI.Extensions;

[TestFixture]
public class JsonExtensionsTests
{
    private record Payload(string Name, int Value);

    [Test]
    public async Task ToJsonContent_SerializesObjectAsJsonWithUtf8Encoding()
    {
        var payload = new Payload("Test", 42);

        var content = payload.ToJsonContent();

        content.Headers.ContentType!.MediaType.Should().Be("application/json");
        content.Headers.ContentType.CharSet.Should().Be("utf-8");
        var json = await content.ReadAsStringAsync();
        json.Should().Be("{\"Name\":\"Test\",\"Value\":42}");
    }

    [Test]
    public async Task ToJsonContent_WhenOptionsProvided_UsesThemForSerialization()
    {
        var payload = new Payload("Test", 42);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var content = payload.ToJsonContent(options);

        var json = await content.ReadAsStringAsync();
        json.Should().Be("{\"name\":\"Test\",\"value\":42}");
    }
}

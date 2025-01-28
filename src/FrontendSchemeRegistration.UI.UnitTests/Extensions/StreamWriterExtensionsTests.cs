namespace FrontendSchemeRegistration.UI.UnitTests.Extensions;

using FluentAssertions;
using FrontendSchemeRegistration.UI.Extensions;

[TestFixture]
public class StreamWriterExtensionsTests
{

    [Test]
    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("O'Mally", "O'Mally")]
    [TestCase("Eats shoots and leaves", "Eats shoots and leaves")]
    [TestCase("Eats, shoots, and leaves", "\"Eats, shoots, and leaves\"")]
    [TestCase("Hello\r there", "\"Hello\r there\"")]
    [TestCase("Hello\n there", "\"Hello\n there\"")]
    [TestCase("Eats \"shoots\" and leaves", "\"Eats \"\"shoots\"\" and leaves\"")]
    public void CleanCsv_ReturnsCorrectlyFormattedString(string input, string expectedResult)
    {
        var actual = StreamWriterExtensions.CleanCsv(input);

        actual.Should().Be(expectedResult);
    }

    [Test]
    public async Task WriteCsvCellAsync_ReturnsCommaSeparatedValue()
    {
        var stream = new MemoryStream();

        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            await writer.WriteCsvCellAsync("Greetings");
            await writer.FlushAsync();
        }

        string result = System.Text.Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Length);

        result.Should().StartWith("Greetings");
        result.Should().EndWith(",");
    }
}
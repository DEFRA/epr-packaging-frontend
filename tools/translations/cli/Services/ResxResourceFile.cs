using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Translations.Services;

internal static class ResxResourceFile
{
    public static IReadOnlyDictionary<string, string> ReadIfExists(string path)
    {
        return File.Exists(path)
            ? Read(path)
            : new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, string> Read(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"RESX file \"{path}\" was not found.", path);
        }

        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        return document
            .Root?
            .Elements("data")
            .Where(IsStringDataElement)
            .Select(element => new
            {
                Name = element.Attribute("name")?.Value,
                Value = element.Element("value")?.Value
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name) && entry.Value is not null)
            .ToDictionary(entry => entry.Name!, entry => entry.Value!, StringComparer.Ordinal)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public static int UpdateValues(string path, IReadOnlyDictionary<string, string> updates)
    {
        if (updates.Count == 0)
        {
            return 0;
        }

        var document = File.Exists(path)
            ? XDocument.Load(path, LoadOptions.PreserveWhitespace)
            : CreateEmptyDocument();

        var root = document.Root ?? throw new InvalidOperationException($"RESX file \"{path}\" does not contain a root element.");
        var changedValueCount = 0;

        foreach (var (key, value) in updates.OrderBy(update => update.Key, StringComparer.Ordinal))
        {
            var dataElement = root
                .Elements("data")
                .FirstOrDefault(element => string.Equals(element.Attribute("name")?.Value, key, StringComparison.Ordinal));

            if (dataElement is null)
            {
                dataElement = new XElement(
                    "data",
                    new XAttribute("name", key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value));
                root.Add(dataElement);
                changedValueCount++;
                continue;
            }

            var valueElement = dataElement.Element("value");
            if (valueElement is null)
            {
                dataElement.Add(new XElement("value", value));
                changedValueCount++;
                continue;
            }

            if (!string.Equals(valueElement.Value, value, StringComparison.Ordinal))
            {
                valueElement.Value = value;
                changedValueCount++;
            }
        }

        if (changedValueCount == 0)
        {
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        using var writer = XmlWriter.Create(path, new XmlWriterSettings
        {
            Encoding = GetOutputEncoding(path),
            Indent = true,
            NewLineChars = "\n"
        });
        document.Save(writer);
        return changedValueCount;
    }

    private static bool IsStringDataElement(XElement element)
    {
        return element.Attribute("name") is not null &&
               element.Attribute("type") is null &&
               element.Attribute("mimetype") is null &&
               element.Element("value") is not null;
    }

    private static UTF8Encoding GetOutputEncoding(string path)
    {
        return File.Exists(path) && HasUtf8ByteOrderMark(path)
            ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }

    private static bool HasUtf8ByteOrderMark(string path)
    {
        Span<byte> buffer = stackalloc byte[3];
        using var stream = File.OpenRead(path);
        return stream.Read(buffer) == 3 &&
               buffer[0] == 0xEF &&
               buffer[1] == 0xBB &&
               buffer[2] == 0xBF;
    }

    private static XDocument CreateEmptyDocument()
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(
                "root",
                new XElement("resheader", new XAttribute("name", "resmimetype"), new XElement("value", "text/microsoft-resx")),
                new XElement("resheader", new XAttribute("name", "version"), new XElement("value", "2.0")),
                new XElement("resheader", new XAttribute("name", "reader"), new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")),
                new XElement("resheader", new XAttribute("name", "writer"), new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))));
    }
}

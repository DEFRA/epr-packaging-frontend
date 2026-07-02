using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using Translations.Models;

namespace Translations.Services;

internal static class XlsxWorkbookReader
{
    private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static async Task<IReadOnlyList<TranslatedWorkbookRow>> ReadTranslatedRowsAsync(string workbookPath)
    {
        using var archive = ZipFile.OpenRead(workbookPath);
        var sharedStrings = await ReadSharedStringsAsync(archive);
        var worksheet = await ReadFirstWorksheetAsync(archive);
        var rows = ReadRows(worksheet, sharedStrings);
        var header = FindHeader(rows, workbookPath);

        return rows
            .Where(row => row.RowNumber > header.RowNumber)
            .Select(row => new TranslatedWorkbookRow(
                row.Cells.GetValueOrDefault(header.TranslationKeyColumn) ?? string.Empty,
                row.Cells.GetValueOrDefault(header.WelshColumn) ?? string.Empty))
            .Where(row => !string.IsNullOrWhiteSpace(row.TranslationKey))
            .ToArray();
    }

    private static async Task<IReadOnlyList<string>> ReadSharedStringsAsync(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        await using var stream = entry.Open();
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

        return document
            .Root?
            .Elements(SpreadsheetNamespace + "si")
            .Select(ReadSharedStringItem)
            .ToArray()
            ?? [];
    }

    private static async Task<XDocument> ReadFirstWorksheetAsync(ZipArchive archive)
    {
        var worksheetPath = await ResolveFirstWorksheetPathAsync(archive);
        var worksheetEntry = archive.GetEntry(worksheetPath)
                             ?? throw new InvalidOperationException($"Workbook is missing worksheet entry \"{worksheetPath}\".");

        await using var stream = worksheetEntry.Open();
        return await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
    }

    private static async Task<string> ResolveFirstWorksheetPathAsync(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml")
                            ?? throw new InvalidOperationException("Workbook is missing xl/workbook.xml.");
        var relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
                                 ?? throw new InvalidOperationException("Workbook is missing xl/_rels/workbook.xml.rels.");

        await using var workbookStream = workbookEntry.Open();
        await using var relationshipsStream = relationshipsEntry.Open();

        var workbook = await XDocument.LoadAsync(workbookStream, LoadOptions.None, CancellationToken.None);
        var relationships = await XDocument.LoadAsync(relationshipsStream, LoadOptions.None, CancellationToken.None);
        var firstSheetRelationshipId = workbook
            .Root?
            .Element(SpreadsheetNamespace + "sheets")?
            .Elements(SpreadsheetNamespace + "sheet")
            .Select(sheet => sheet.Attribute(RelationshipsNamespace + "id")?.Value)
            .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));

        if (string.IsNullOrWhiteSpace(firstSheetRelationshipId))
        {
            throw new InvalidOperationException("Workbook does not contain any sheets.");
        }

        var target = relationships
            .Root?
            .Elements(PackageRelationshipsNamespace + "Relationship")
            .FirstOrDefault(relationship => relationship.Attribute("Id")?.Value == firstSheetRelationshipId)?
            .Attribute("Target")?
            .Value;

        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException($"Workbook relationship \"{firstSheetRelationshipId}\" does not point to a worksheet.");
        }

        return PathHelpers.Normalize(Path.Combine("xl", target));
    }

    private static IReadOnlyList<WorksheetRow> ReadRows(XDocument worksheet, IReadOnlyList<string> sharedStrings)
    {
        return worksheet
            .Descendants(SpreadsheetNamespace + "row")
            .Select(row => new WorksheetRow(
                ParseRowNumber(row),
                row.Elements(SpreadsheetNamespace + "c")
                    .Where(cell => cell.Attribute("r") is not null)
                    .ToDictionary(
                        cell => ColumnNameToNumber(new string(cell.Attribute("r")!.Value.TakeWhile(char.IsLetter).ToArray())),
                        cell => ReadCellValue(cell, sharedStrings))))
            .ToArray();
    }

    private static HeaderColumns FindHeader(IReadOnlyList<WorksheetRow> rows, string workbookPath)
    {
        foreach (var row in rows)
        {
            var translationKeyColumn = row.Cells.FirstOrDefault(cell => cell.Value == "Translation key").Key;
            var welshColumn = row.Cells.FirstOrDefault(cell => cell.Value == "Welsh").Key;

            if (translationKeyColumn != 0 && welshColumn != 0)
            {
                return new HeaderColumns(row.RowNumber, translationKeyColumn, welshColumn);
            }
        }

        throw new InvalidOperationException($"Workbook \"{workbookPath}\" is missing required Translation key and Welsh columns.");
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var cellType = cell.Attribute("t")?.Value;

        if (cellType == "inlineStr")
        {
            return cell
                .Element(SpreadsheetNamespace + "is")?
                .Descendants(SpreadsheetNamespace + "t")
                .Aggregate(string.Empty, (current, text) => current + text.Value)
                .Trim()
                ?? string.Empty;
        }

        var value = cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty;

        if (cellType == "s" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedStringIndex))
        {
            return sharedStringIndex >= 0 && sharedStringIndex < sharedStrings.Count
                ? sharedStrings[sharedStringIndex].Trim()
                : string.Empty;
        }

        return value.Trim();
    }

    private static string ReadSharedStringItem(XElement item)
    {
        return item
            .Descendants(SpreadsheetNamespace + "t")
            .Aggregate(string.Empty, (current, text) => current + text.Value);
    }

    private static int ParseRowNumber(XElement row)
    {
        var rowNumberText = row.Attribute("r")?.Value;
        return int.TryParse(rowNumberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rowNumber)
            ? rowNumber
            : 0;
    }

    private static int ColumnNameToNumber(string columnName)
    {
        var columnNumber = 0;

        foreach (var character in columnName.ToUpperInvariant())
        {
            columnNumber *= 26;
            columnNumber += character - 'A' + 1;
        }

        return columnNumber;
    }

    private sealed record WorksheetRow(int RowNumber, IReadOnlyDictionary<int, string> Cells);

    private sealed record HeaderColumns(int RowNumber, int TranslationKeyColumn, int WelshColumn);
}

using System.IO.Compression;
using System.Text;
using Translations.Models;

namespace Translations.Services;

internal static class XlsxWorkbookWriter
{
    private const string SheetName = "Welsh translations";

    public static async Task WriteAsync(string path, PageTranslationGroup group, IReadOnlyList<string> translatorInstructions)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        await using var fileStream = File.Create(path);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
        var headerRowNumber = translatorInstructions.Count + 3;

        await AddEntryAsync(archive, "[Content_Types].xml", ContentTypesXml());
        await AddEntryAsync(archive, "_rels/.rels", RootRelationshipsXml());
        await AddEntryAsync(archive, "docProps/app.xml", AppPropertiesXml());
        await AddEntryAsync(archive, "docProps/core.xml", CorePropertiesXml());
        await AddEntryAsync(archive, "xl/workbook.xml", WorkbookXml());
        await AddEntryAsync(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml());
        await AddEntryAsync(archive, "xl/styles.xml", StylesXml());
        await AddEntryAsync(archive, "xl/worksheets/sheet1.xml", WorksheetXml(group, translatorInstructions, headerRowNumber));
    }

    private static async Task AddEntryAsync(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteAsync(content);
    }

    private static string WorksheetXml(PageTranslationGroup group, IReadOnlyList<string> translatorInstructions, int headerRowNumber)
    {
        var rows = new StringBuilder();
        var mergeCells = new StringBuilder();

        rows.Append(Row(1, Cell("F", 1, "Translator notes", style: 2)));
        mergeCells.Append("""<mergeCell ref="F1:H1"/>""");

        for (var index = 0; index < translatorInstructions.Count; index++)
        {
            var instructionRowNumber = index + 2;
            rows.Append(Row(instructionRowNumber, Cell("F", instructionRowNumber, translatorInstructions[index])));
            mergeCells.Append($"""<mergeCell ref="F{instructionRowNumber}:H{instructionRowNumber}"/>""");
        }

        rows.Append(Row(
            headerRowNumber,
            Cell("A", headerRowNumber, "Translation key", style: 1),
            Cell("B", headerRowNumber, "Resource file", style: 1),
            Cell("C", headerRowNumber, "Resource key", style: 1),
            Cell("D", headerRowNumber, "Page id", style: 1),
            Cell("E", headerRowNumber, "Route", style: 1),
            Cell("F", headerRowNumber, "English", style: 1),
            Cell("G", headerRowNumber, "Welsh", style: 1),
            Cell("H", headerRowNumber, "Figma link", style: 1)));

        var rowNumber = headerRowNumber;
        foreach (var row in group.Rows)
        {
            rowNumber++;
            rows.Append(Row(
                rowNumber,
                Cell("A", rowNumber, row.ResourceKey.TranslationKey),
                Cell("B", rowNumber, row.ResourceKey.ResourceFile),
                Cell("C", rowNumber, row.ResourceKey.Key),
                Cell("D", rowNumber, row.PageId),
                Cell("E", rowNumber, row.Route),
                Cell("F", rowNumber, row.English),
                Cell("G", rowNumber, row.Welsh),
                Cell("H", rowNumber, row.FigmaUrl)));
        }

        var finalRowNumber = Math.Max(rowNumber, headerRowNumber);
        var mergeCellsXml = mergeCells.Length == 0
            ? string.Empty
            : $"""<mergeCells count="{translatorInstructions.Count + 1}">{mergeCells}</mergeCells>""";

        return $$"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <dimension ref="A1:H{{finalRowNumber}}"/>
  <sheetViews>
    <sheetView workbookViewId="0">
      <pane ySplit="{{headerRowNumber}}" topLeftCell="A{{headerRowNumber + 1}}" activePane="bottomLeft" state="frozen"/>
    </sheetView>
  </sheetViews>
  <sheetFormatPr defaultRowHeight="15"/>
  <cols>
    <col min="1" max="5" width="24" customWidth="1" hidden="1"/>
    <col min="6" max="7" width="70" customWidth="1"/>
    <col min="8" max="8" width="45" customWidth="1"/>
  </cols>
  <sheetData>
{{rows}}
  </sheetData>
  <autoFilter ref="A{{headerRowNumber}}:H{{finalRowNumber}}"/>
  {{mergeCellsXml}}
  <pageMargins left="0.7" right="0.7" top="0.75" bottom="0.75" header="0.3" footer="0.3"/>
</worksheet>
""";
    }

    private static string Row(int rowNumber, params string[] cells)
    {
        return $"""    <row r="{rowNumber}" ht="{(rowNumber == 1 ? 24 : 48)}" customHeight="1">{string.Concat(cells)}</row>""" + Environment.NewLine;
    }

    private static string Cell(string column, int row, string? value, int style = 0)
    {
        var styleAttribute = style > 0 ? $""" s="{style}" """ : " ";
        return $"""<c r="{column}{row}"{styleAttribute}t="inlineStr"><is><t xml:space="preserve">{Escape(value ?? string.Empty)}</t></is></c>""";
    }

    private static string Escape(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }

    private static string ContentTypesXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
</Types>
""";
    }

    private static string RootRelationshipsXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
""";
    }

    private static string WorkbookRelationshipsXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
""";
    }

    private static string WorkbookXml()
    {
        return $$"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="{{SheetName}}" sheetId="1" r:id="rId1"/>
  </sheets>
</workbook>
""";
    }

    private static string StylesXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <fonts count="3">
    <font><sz val="11"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
    <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/><family val="2"/></font>
    <font><b/><sz val="14"/><color theme="1"/><name val="Calibri"/><family val="2"/></font>
  </fonts>
  <fills count="3">
    <fill><patternFill patternType="none"/></fill>
    <fill><patternFill patternType="gray125"/></fill>
    <fill><patternFill patternType="solid"><fgColor rgb="FF1D70B8"/><bgColor indexed="64"/></patternFill></fill>
  </fills>
  <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
  <cellXfs count="3">
    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf>
    <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf>
    <xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf>
  </cellXfs>
  <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
</styleSheet>
""";
    }

    private static string AppPropertiesXml()
    {
        return """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>translations</Application>
</Properties>
""";
    }

    private static string CorePropertiesXml()
    {
        return $$"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:creator>epr-packaging-frontend translations tool</dc:creator>
  <cp:lastModifiedBy>epr-packaging-frontend translations tool</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">{{DateTime.UtcNow:O}}</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">{{DateTime.UtcNow:O}}</dcterms:modified>
</cp:coreProperties>
""";
    }
}

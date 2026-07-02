using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using NUnit.Framework;
using Translations.Configuration;
using Translations.Models;
using Translations.Services;

namespace Translations.UnitTests;

[TestFixture]
[NonParallelizable]
public sealed class TranslationExportImportTests
{
    private static readonly string[] SharedResourceTranslationKeys =
    [
        "Resources/Shared.en.resx::heading",
        "Resources/Shared.en.resx::body"
    ];

    private string _projectRoot = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _projectRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_projectRoot))
        {
            Directory.Delete(_projectRoot, recursive: true);
        }
    }

    [Test]
    public async Task ExportAsync_WhenLaterPageOnlyReusesRows_SkipsEmptyWorkbook()
    {
        var resourcePath = "Resources/Shared.en.resx";
        WriteResx(
            resourcePath,
            ("heading", "Submit <strong>{0}</strong>"),
            ("body", "Read the guidance"));
        WriteResx(
            "Resources/Shared.cy.resx",
            ("heading", "Cyflwyno <strong>{0}</strong>"),
            ("body", "Read the guidance"));

        var profile = CreateProfile(
            CreatePage("first-page", "01-first.xlsx", resourcePath),
            CreatePage("second-page", "02-second.xlsx", resourcePath));

        var consoleOutput = await CaptureConsoleOutputAsync(() =>
            ExportService.ExportAsync(_projectRoot, profile, "exports"));

        var firstWorkbook = Path.Combine(_projectRoot, "exports", "01-first.xlsx");
        var secondWorkbook = Path.Combine(_projectRoot, "exports", "02-second.xlsx");
        Assert.That(File.Exists(firstWorkbook), Is.True);
        Assert.That(File.Exists(secondWorkbook), Is.False);
        Assert.That(consoleOutput, Does.Contain("Skipped second-page: no translation entries to include in this page"));

        var rows = await XlsxWorkbookReader.ReadTranslatedRowsAsync(firstWorkbook);
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows.Select(row => row.TranslationKey), Is.EquivalentTo(SharedResourceTranslationKeys));
        Assert.That(rows.Single(row => row.TranslationKey.EndsWith("::heading", StringComparison.Ordinal)).Welsh, Is.EqualTo("Cyflwyno <strong>{0}</strong>"));
        Assert.That(rows.Single(row => row.TranslationKey.EndsWith("::body", StringComparison.Ordinal)).Welsh, Is.Empty);
    }

    [Test]
    public async Task ImportAsync_WhenWelshValueChanges_UpdatesTargetResx()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(resourcePath, ("message", "Submit <strong>{0}</strong>"));

        var workbookPath = Path.Combine(_projectRoot, "input.xlsx");
        await WriteWorkbookAsync(
            workbookPath,
            resourcePath,
            "message",
            "Submit <strong>{0}</strong>",
            "Cyflwyno <strong>{0}</strong>");

        var profile = CreateProfile(CreatePage("page", "input.xlsx", resourcePath));
        var consoleOutput = await CaptureConsoleOutputAsync(() =>
            ImportService.ImportAsync(_projectRoot, profile, workbookPath));

        var targetPath = Path.Combine(_projectRoot, "Resources", "Page.cy.resx");
        var values = ResxResourceFile.Read(targetPath);
        Assert.That(values["message"], Is.EqualTo("Cyflwyno <strong>{0}</strong>"));
        Assert.That(File.ReadAllText(targetPath), Does.Contain("&lt;strong&gt;{0}&lt;/strong&gt;"));
        Assert.That(consoleOutput, Does.Contain("Imported 1 changed value"));
        Assert.That(consoleOutput, Does.Contain("Updated 1 resource file"));
    }

    [Test]
    public async Task ImportAsync_WhenTranslatedValueLosesPlaceholderAndMarkup_FailsWithoutWritingTargetResx()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(resourcePath, ("message", "Submit <strong>{0}</strong>"));

        var workbookPath = Path.Combine(_projectRoot, "input.xlsx");
        await WriteWorkbookAsync(
            workbookPath,
            resourcePath,
            "message",
            "Submit <strong>{0}</strong>",
            "Cyflwyno");

        var profile = CreateProfile(CreatePage("page", "input.xlsx", resourcePath));
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            ImportService.ImportAsync(_projectRoot, profile, workbookPath));

        Assert.That(exception!.Message, Does.Contain("missing placeholder {0}"));
        Assert.That(exception.Message, Does.Contain("missing markup tags <strong>, </strong>"));
        Assert.That(File.Exists(Path.Combine(_projectRoot, "Resources", "Page.cy.resx")), Is.False);
    }

    [Test]
    public void ExportAsync_WhenEnglishValueHasOuterWhitespace_FailsBeforeCreatingWorkbook()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(resourcePath, ("message", " Submit"));

        var profile = CreateProfile(CreatePage("page", "page.xlsx", resourcePath));
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExportService.ExportAsync(_projectRoot, profile, "exports"));

        Assert.That(exception!.Message, Does.Contain("English translation values must not include leading or trailing whitespace"));
        Assert.That(exception.Message, Does.Contain("Resources/Page.en.resx::message"));
        Assert.That(File.Exists(Path.Combine(_projectRoot, "exports", "page.xlsx")), Is.False);
    }

    [Test]
    public async Task WriteAsync_WhenTranslationTextIsLong_IncreasesDataRowHeight()
    {
        var workbookPath = Path.Combine(_projectRoot, "long-text.xlsx");
        var longEnglish = string.Join(
            " ",
            Enumerable.Repeat("This sentence should wrap inside the English translation column.", 8));

        await WriteWorkbookAsync(
            workbookPath,
            "Resources/Page.en.resx",
            "message",
            longEnglish,
            string.Empty);

        using var archive = ZipFile.OpenRead(workbookPath);
        var worksheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")!;
        using var worksheetStream = worksheetEntry.Open();
        var worksheet = XDocument.Load(worksheetStream);
        XNamespace spreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var dataRow = worksheet
            .Descendants(spreadsheetNamespace + "row")
            .Single(row => row.Attribute("r")?.Value == "4");

        var rowHeight = double.Parse(dataRow.Attribute("ht")!.Value, CultureInfo.InvariantCulture);
        Assert.That(rowHeight, Is.GreaterThan(48));
    }

    private static TranslationProfile CreateProfile(params PageProfile[] pages)
    {
        return new TranslationProfile
        {
            Name = "test",
            SourceCulture = "en",
            TargetCulture = "cy",
            DefaultOutputPath = "exports",
            Pages = pages
        };
    }

    private static PageProfile CreatePage(string id, string fileName, string resourcePath)
    {
        return new PageProfile
        {
            Id = id,
            FileName = fileName,
            Route = $"/{id}",
            View = $"Views/{id}.cshtml",
            FigmaUrl = null,
            Notes = $"{id} notes",
            Resources =
            [
                new ResourceSelection
                {
                    Source = resourcePath,
                    Section = $"{id} section"
                }
            ]
        };
    }

    private static async Task WriteWorkbookAsync(
        string workbookPath,
        string resourcePath,
        string resourceKey,
        string english,
        string welsh)
    {
        var rowResourceKey = new ResourceKey(resourcePath, resourceKey);
        var group = new PageTranslationGroup(
            "page",
            Path.GetFileName(workbookPath),
            "/page",
            "Page notes",
            null,
            [],
            [
                new TranslationRow(
                    rowResourceKey,
                    "page",
                    "/page",
                    "Page section",
                    english,
                    welsh,
                    null,
                    "Page context")
            ]);

        await XlsxWorkbookWriter.WriteAsync(workbookPath, group, []);
    }

    private void WriteResx(string relativePath, params (string Key, string Value)[] values)
    {
        var fullPath = Path.Combine(_projectRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var document = new XDocument(
            new XElement(
                "root",
                values.Select(value =>
                    new XElement(
                        "data",
                        new XAttribute("name", value.Key),
                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                        new XElement("value", value.Value)))));

        document.Save(fullPath);
    }

    private static async Task<string> CaptureConsoleOutputAsync(Func<Task> action)
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            await action();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        return writer.ToString();
    }
}

using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using Translations;
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
        var staleSecondWorkbook = Path.Combine(_projectRoot, "exports", "02-second.xlsx");
        Directory.CreateDirectory(Path.GetDirectoryName(staleSecondWorkbook)!);
        File.WriteAllText(staleSecondWorkbook, "stale workbook");

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
        File.WriteAllText(workbookPath, "stale workbook");
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

    [Test]
    public void CommandOptions_Parse_WhenOptionsAreProvided_ReadsValues()
    {
        var options = CommandOptions.Parse(
            [
                "--profile",
                "custom",
                "--output",
                "exports",
                "--input",
                "imports",
                "--project-root",
                _projectRoot
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(options.Profile, Is.EqualTo("custom"));
            Assert.That(options.Output, Is.EqualTo("exports"));
            Assert.That(options.Input, Is.EqualTo("imports"));
            Assert.That(options.ProjectRoot, Is.EqualTo(_projectRoot));
        });
    }

    [Test]
    public void CommandOptions_Parse_WhenOptionValueIsMissing_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => CommandOptions.Parse(["--profile"]));

        Assert.That(exception!.Message, Is.EqualTo("Missing value for --profile."));
    }

    [Test]
    public void CommandOptions_Parse_WhenOptionIsUnknown_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => CommandOptions.Parse(["--unexpected"]));

        Assert.That(exception!.Message, Is.EqualTo("Unknown option \"--unexpected\"."));
    }

    [Test]
    public void ProjectRootLocator_Find_WhenProjectRootIsSupplied_ReturnsFullPath()
    {
        var relativeRoot = Path.GetRelativePath(Directory.GetCurrentDirectory(), _projectRoot);

        var projectRoot = ProjectRootLocator.Find(relativeRoot);

        Assert.That(projectRoot, Is.EqualTo(Path.GetFullPath(_projectRoot)));
    }

    [Test]
    public void ProjectRootLocator_Find_WhenCurrentDirectoryIsInsideRepository_ReturnsRepositoryRoot()
    {
        var markerDirectory = Path.Combine(_projectRoot, "src", "FrontendSchemeRegistration.UI");
        var nestedDirectory = Path.Combine(_projectRoot, "tools", "translations", "cli");
        Directory.CreateDirectory(markerDirectory);
        Directory.CreateDirectory(nestedDirectory);
        var originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(nestedDirectory);

            var projectRoot = ProjectRootLocator.Find(null);
            var expectedRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));

            Assert.That(projectRoot, Is.EqualTo(expectedRoot));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Test]
    public void ProfileLoader_Load_WhenProfileNameIsProvided_ReadsProfileFromProfilesFolder()
    {
        var profilePath = Path.Combine(_projectRoot, "tools", "translations", "profiles", "local.json");
        WriteProfileJson(profilePath, "local", "exports", "page.xlsx", "Resources/Page.en.resx");

        var profile = ProfileLoader.Load(_projectRoot, "local");

        Assert.Multiple(() =>
        {
            Assert.That(profile.Name, Is.EqualTo("local"));
            Assert.That(profile.DefaultOutputPath, Is.EqualTo("exports"));
            Assert.That(profile.Pages, Has.Count.EqualTo(1));
            Assert.That(profile.Pages[0].Resources[0].Source, Is.EqualTo("Resources/Page.en.resx"));
        });
    }

    [Test]
    public void ProfileLoader_Load_WhenExplicitJsonPathIsProvided_ReadsProfileFromPath()
    {
        var profilePath = Path.Combine(_projectRoot, "profiles", "explicit.json");
        WriteProfileJson(profilePath, "explicit", "exports", "page.xlsx", "Resources/Page.en.resx");

        var profile = ProfileLoader.Load(_projectRoot, profilePath);

        Assert.That(profile.Name, Is.EqualTo("explicit"));
    }

    [Test]
    public void ProfileLoader_Load_WhenProfileIsMissing_Throws()
    {
        var exception = Assert.Throws<FileNotFoundException>(() => ProfileLoader.Load(_projectRoot, "missing"));

        Assert.That(exception!.Message, Does.Contain("Profile \"missing\" was not found."));
    }

    [Test]
    public void ProfileLoader_Load_WhenProfileHasNoPages_Throws()
    {
        var profilePath = Path.Combine(_projectRoot, "empty.json");
        Directory.CreateDirectory(Path.GetDirectoryName(profilePath)!);
        File.WriteAllText(profilePath, """{ "name": "empty", "pages": [] }""");

        var exception = Assert.Throws<InvalidOperationException>(() => ProfileLoader.Load(_projectRoot, profilePath));

        Assert.That(exception!.Message, Does.Contain("does not define any pages"));
    }

    [Test]
    public void PathHelpers_ToTargetCulturePath_MapsCultureSpecificAndNeutralResxPaths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PathHelpers.Normalize(@"Resources\Page.en.resx"), Is.EqualTo("Resources/Page.en.resx"));
            Assert.That(PathHelpers.ToTargetCulturePath("Resources/Page.en.resx", "en", "cy"), Is.EqualTo("Resources/Page.cy.resx"));
            Assert.That(PathHelpers.ToTargetCulturePath("Resources/Page.resx", "en", "cy"), Is.EqualTo("Resources/Page.cy.resx"));
        });
    }

    [Test]
    public void PathHelpers_ToTargetCulturePath_WhenPathIsNotResx_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PathHelpers.ToTargetCulturePath("Resources/Page.txt", "en", "cy"));

        Assert.That(exception!.Message, Is.EqualTo("Resource file \"Resources/Page.txt\" is not a .resx file."));
    }

    [Test]
    public void ResourceKey_Parse_WhenKeyIsValid_ReturnsResourceFileAndKey()
    {
        var resourceKey = ResourceKey.Parse(@"Resources\Page.en.resx::heading");

        Assert.Multiple(() =>
        {
            Assert.That(resourceKey.ResourceFile, Is.EqualTo("Resources/Page.en.resx"));
            Assert.That(resourceKey.Key, Is.EqualTo("heading"));
            Assert.That(resourceKey.TranslationKey, Is.EqualTo("Resources/Page.en.resx::heading"));
        });
    }

    [Test]
    public void ResourceKey_Parse_WhenKeyIsInvalid_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ResourceKey.Parse("Resources/Page.en.resx"));

        Assert.That(exception!.Message, Does.Contain("must use the format resource-file::key"));
    }

    [Test]
    public void TranslationValueValidator_Validate_WhenWelshAddsPlaceholderMarkupAndEncodedHtml_ReturnsErrors()
    {
        var errors = TranslationValueValidator.Validate(
            "Resources/Page.en.resx::message",
            "Submit <strong>{0}</strong>",
            " Cyflwyno &lt;strong&gt;{0}&lt;/strong&gt; {1}");

        Assert.That(errors, Has.Count.EqualTo(4));
        Assert.That(errors[0], Does.Contain("must not include leading or trailing whitespace"));
        Assert.That(errors[1], Does.Contain("contains encoded HTML"));
        Assert.That(errors[2], Does.Contain("unexpected placeholder {1}"));
        Assert.That(errors[3], Does.Contain("missing markup tags <strong>, </strong>"));
    }

    [Test]
    public async Task ExportAsync_WhenResourceSelectionUsesKeysAndPrefixes_ExportsSelectedRowsOnly()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(
            resourcePath,
            ("heading", "Heading"),
            ("body_first", "First body"),
            ("body_second", "Second body"),
            ("other", "Other"));
        WriteResx(
            "Resources/Page.cy.resx",
            ("heading", "Pennawd"),
            ("body_first", "First body"),
            ("body_second", "Ail gorff"),
            ("other", "Arall"));

        var profile = CreateProfile(
            CreatePageWithResources(
                "page",
                "page.xlsx",
                new ResourceSelection
                {
                    Source = resourcePath,
                    Section = "Selected content",
                    Keys = ["heading"],
                    KeyPrefixes = ["body_"]
                }));

        await ExportService.ExportAsync(_projectRoot, profile, "exports");

        var rows = await XlsxWorkbookReader.ReadTranslatedRowsAsync(Path.Combine(_projectRoot, "exports", "page.xlsx"));
        Assert.That(
            rows.Select(row => row.TranslationKey),
            Is.EqualTo(new[]
            {
                "Resources/Page.en.resx::heading",
                "Resources/Page.en.resx::body_first",
                "Resources/Page.en.resx::body_second"
            }));
        Assert.That(rows.Single(row => row.TranslationKey.EndsWith("::heading", StringComparison.Ordinal)).Welsh, Is.EqualTo("Pennawd"));
        Assert.That(rows.Single(row => row.TranslationKey.EndsWith("::body_first", StringComparison.Ordinal)).Welsh, Is.Empty);
        Assert.That(rows.Single(row => row.TranslationKey.EndsWith("::body_second", StringComparison.Ordinal)).Welsh, Is.EqualTo("Ail gorff"));
    }

    [Test]
    public void ExportAsync_WhenConfiguredKeyIsMissing_Throws()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(resourcePath, ("heading", "Heading"));

        var profile = CreateProfile(
            CreatePageWithResources(
                "page",
                "page.xlsx",
                new ResourceSelection
                {
                    Source = resourcePath,
                    Keys = ["missing"]
                }));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExportService.ExportAsync(_projectRoot, profile, "exports"));

        Assert.That(exception!.Message, Is.EqualTo("Resource \"Resources/Page.en.resx\" does not contain configured key(s): missing."));
    }

    [Test]
    public void ImportAsync_WhenInputPathDoesNotExist_Throws()
    {
        var profile = CreateProfile(CreatePage("page", "page.xlsx", "Resources/Page.en.resx"));

        var exception = Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            ImportService.ImportAsync(_projectRoot, profile, "missing"));

        Assert.That(exception!.Message, Does.Contain("Input path"));
        Assert.That(exception.Message, Does.Contain("does not exist"));
    }

    [Test]
    public async Task ImportAsync_WhenDirectoryContainsConflictingWelshValues_Throws()
    {
        var resourcePath = "Resources/Page.en.resx";
        var inputDirectory = Path.Combine(_projectRoot, "imports");
        Directory.CreateDirectory(inputDirectory);
        await WriteWorkbookAsync(
            Path.Combine(inputDirectory, "01-page.xlsx"),
            resourcePath,
            "message",
            "Submit",
            "Cyflwyno");
        await WriteWorkbookAsync(
            Path.Combine(inputDirectory, "02-page.xlsx"),
            resourcePath,
            "message",
            "Submit",
            "Anfon");

        var profile = CreateProfile(CreatePage("page", "page.xlsx", resourcePath));
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            ImportService.ImportAsync(_projectRoot, profile, inputDirectory));

        Assert.That(exception!.Message, Is.EqualTo("Conflicting Welsh values found for translation key \"Resources/Page.en.resx::message\"."));
    }

    [Test]
    public async Task ImportAsync_WhenSourceResourceKeyIsMissing_ThrowsWithoutWritingTargetResx()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(resourcePath, ("other", "Other"));

        var workbookPath = Path.Combine(_projectRoot, "input.xlsx");
        await WriteWorkbookAsync(
            workbookPath,
            resourcePath,
            "message",
            "Submit",
            "Cyflwyno");

        var profile = CreateProfile(CreatePage("page", "input.xlsx", resourcePath));
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            ImportService.ImportAsync(_projectRoot, profile, workbookPath));

        Assert.That(exception!.Message, Is.EqualTo("Source RESX file \"Resources/Page.en.resx\" does not contain key \"message\"."));
        Assert.That(File.Exists(Path.Combine(_projectRoot, "Resources", "Page.cy.resx")), Is.False);
    }

    [Test]
    public void ResxResourceFile_ReadIfExists_WhenFileDoesNotExist_ReturnsEmptyDictionary()
    {
        var entries = ResxResourceFile.ReadIfExists(Path.Combine(_projectRoot, "missing.resx"));

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public void ResxResourceFile_Read_IgnoresNonStringDataEntries()
    {
        var fullPath = Path.Combine(_projectRoot, "Resources", "Mixed.en.resx");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(
            fullPath,
            """
            <root>
              <data name="visible" xml:space="preserve"><value>Visible</value></data>
              <data name="typed" type="System.String"><value>Typed</value></data>
              <data name="binary" mimetype="application/x-microsoft.net.object.binary.base64"><value>AAAA</value></data>
              <data name="missingValue" xml:space="preserve" />
            </root>
            """);

        var entries = ResxResourceFile.Read(fullPath);

        Assert.That(entries, Is.EquivalentTo(new Dictionary<string, string> { ["visible"] = "Visible" }));
    }

    [Test]
    public void ResxResourceFile_UpdateValues_WhenExistingValueIsUnchanged_ReturnsZero()
    {
        var fullPath = Path.Combine(_projectRoot, "Resources", "Page.cy.resx");
        WriteResx("Resources/Page.cy.resx", ("message", "Cyflwyno"));

        var changedValueCount = ResxResourceFile.UpdateValues(
            fullPath,
            new Dictionary<string, string> { ["message"] = "Cyflwyno" });

        Assert.That(changedValueCount, Is.Zero);
    }

    [Test]
    public void ResxResourceFile_UpdateValues_WhenValueElementIsMissing_AddsValueElement()
    {
        var fullPath = Path.Combine(_projectRoot, "Resources", "Page.cy.resx");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(
            fullPath,
            """
            <root>
              <data name="message" xml:space="preserve" />
            </root>
            """);

        var changedValueCount = ResxResourceFile.UpdateValues(
            fullPath,
            new Dictionary<string, string> { ["message"] = "Cyflwyno" });

        Assert.That(changedValueCount, Is.EqualTo(1));
        Assert.That(ResxResourceFile.Read(fullPath)["message"], Is.EqualTo("Cyflwyno"));
    }

    [Test]
    public async Task XlsxWorkbookReader_ReadTranslatedRowsAsync_WhenWorkbookUsesSharedStrings_ReadsRows()
    {
        var workbookPath = Path.Combine(_projectRoot, "shared-strings.xlsx");
        WriteSharedStringWorkbook(workbookPath);

        var rows = await XlsxWorkbookReader.ReadTranslatedRowsAsync(workbookPath);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].TranslationKey, Is.EqualTo("Resources/Page.en.resx::message"));
            Assert.That(rows[0].Welsh, Is.EqualTo("Cyfieithiad"));
        });
    }

    [Test]
    public async Task Cli_RunAsync_WhenHelpIsRequested_WritesUsageAndReturnsZero()
    {
        var console = await CaptureConsoleAsync(() => Cli.RunAsync(["--help"]));

        Assert.Multiple(() =>
        {
            Assert.That(console.ExitCode, Is.Zero);
            Assert.That(console.Out, Does.Contain("Translation workbook export/import tool"));
            Assert.That(console.Error, Is.Empty);
        });
    }

    [Test]
    public async Task Cli_RunAsync_WhenUnknownCommandIsProvided_WritesErrorAndReturnsOne()
    {
        var profilePath = Path.Combine(_projectRoot, "profile.json");
        WriteProfileJson(profilePath, "cli", "exports", "page.xlsx", "Resources/Page.en.resx");

        var console = await CaptureConsoleAsync(() =>
            Cli.RunAsync(["missing", "--project-root", _projectRoot, "--profile", profilePath]));

        Assert.Multiple(() =>
        {
            Assert.That(console.ExitCode, Is.EqualTo(1));
            Assert.That(console.Error, Does.Contain("Unknown command \"missing\"."));
            Assert.That(console.Out, Does.Contain("Usage:"));
        });
    }

    [Test]
    public async Task Cli_RunAsync_WhenExportCommandIsProvided_CreatesWorkbook()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(resourcePath, ("message", "Submit"));
        var profilePath = Path.Combine(_projectRoot, "profile.json");
        WriteProfileJson(profilePath, "cli", "exports", "page.xlsx", resourcePath);

        var console = await CaptureConsoleAsync(() =>
            Cli.RunAsync(["export", "--project-root", _projectRoot, "--profile", profilePath, "--output", "cli-exports"]));

        Assert.Multiple(() =>
        {
            Assert.That(console.ExitCode, Is.Zero);
            Assert.That(File.Exists(Path.Combine(_projectRoot, "cli-exports", "page.xlsx")), Is.True);
            Assert.That(console.Out, Does.Contain("Created 1 translation workbook"));
            Assert.That(console.Error, Is.Empty);
        });
    }

    [Test]
    public async Task Cli_RunAsync_WhenImportCommandIsProvided_UpdatesTargetResx()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(resourcePath, ("message", "Submit"));
        var profilePath = Path.Combine(_projectRoot, "profile.json");
        WriteProfileJson(profilePath, "cli", "exports", "page.xlsx", resourcePath);
        var workbookPath = Path.Combine(_projectRoot, "input.xlsx");
        await WriteWorkbookAsync(workbookPath, resourcePath, "message", "Submit", "Cyflwyno");

        var console = await CaptureConsoleAsync(() =>
            Cli.RunAsync(["import", "--project-root", _projectRoot, "--profile", profilePath, "--input", workbookPath]));

        Assert.Multiple(() =>
        {
            Assert.That(console.ExitCode, Is.Zero);
            Assert.That(ResxResourceFile.Read(Path.Combine(_projectRoot, "Resources", "Page.cy.resx"))["message"], Is.EqualTo("Cyflwyno"));
            Assert.That(console.Out, Does.Contain("Imported 1 changed value"));
            Assert.That(console.Error, Is.Empty);
        });
    }

    [Test]
    public async Task Cli_RunAsync_WhenCommandFails_WritesErrorAndReturnsOne()
    {
        var profilePath = Path.Combine(_projectRoot, "profile.json");
        WriteProfileJson(profilePath, "cli", "exports", "page.xlsx", "Resources/Missing.en.resx");

        var console = await CaptureConsoleAsync(() =>
            Cli.RunAsync(["export", "--project-root", _projectRoot, "--profile", profilePath]));

        Assert.Multiple(() =>
        {
            Assert.That(console.ExitCode, Is.EqualTo(1));
            Assert.That(console.Error, Does.Contain("RESX file"));
            Assert.That(console.Error, Does.Contain("was not found"));
        });
    }

    [Test]
    public async Task ImportAsync_WhenSourceResourceFileIsMissing_ThrowsWithoutWritingTargetResx()
    {
        var resourcePath = "Resources/Missing.en.resx";
        var workbookPath = Path.Combine(_projectRoot, "input.xlsx");
        await WriteWorkbookAsync(
            workbookPath,
            resourcePath,
            "message",
            "Submit",
            "Cyflwyno");

        var profile = CreateProfile(CreatePage("page", "input.xlsx", resourcePath));
        var exception = Assert.ThrowsAsync<FileNotFoundException>(() =>
            ImportService.ImportAsync(_projectRoot, profile, workbookPath));

        Assert.That(exception!.Message, Does.Contain("Source RESX file in translation key was not found: Resources/Missing.en.resx"));
        Assert.That(File.Exists(Path.Combine(_projectRoot, "Resources", "Missing.cy.resx")), Is.False);
    }

    [Test]
    public async Task ImportAsync_WhenConflictingBlankWelshValueExists_IgnoresBlankValue()
    {
        var resourcePath = "Resources/Page.en.resx";
        WriteResx(resourcePath, ("message", "Submit"));
        var inputDirectory = Path.Combine(_projectRoot, "imports");
        Directory.CreateDirectory(inputDirectory);
        await WriteWorkbookAsync(
            Path.Combine(inputDirectory, "01-page.xlsx"),
            resourcePath,
            "message",
            "Submit",
            "Cyflwyno");
        await WriteWorkbookAsync(
            Path.Combine(inputDirectory, "02-page.xlsx"),
            resourcePath,
            "message",
            "Submit",
            string.Empty);

        var profile = CreateProfile(CreatePage("page", "page.xlsx", resourcePath));
        await ImportService.ImportAsync(_projectRoot, profile, inputDirectory);

        Assert.That(ResxResourceFile.Read(Path.Combine(_projectRoot, "Resources", "Page.cy.resx"))["message"], Is.EqualTo("Cyflwyno"));
    }

    [Test]
    public void ResxResourceFile_Read_WhenFileDoesNotExist_Throws()
    {
        var missingPath = Path.Combine(_projectRoot, "Resources", "Missing.en.resx");

        var exception = Assert.Throws<FileNotFoundException>(() => ResxResourceFile.Read(missingPath));

        Assert.That(exception!.Message, Does.Contain("was not found"));
    }

    [Test]
    public void ResxResourceFile_UpdateValues_WhenUpdatesAreEmpty_ReturnsZero()
    {
        var changedValueCount = ResxResourceFile.UpdateValues(
            Path.Combine(_projectRoot, "Resources", "Page.cy.resx"),
            new Dictionary<string, string>());

        Assert.That(changedValueCount, Is.Zero);
    }

    [Test]
    public void ResxResourceFile_UpdateValues_WhenExistingValueChanges_UpdatesValue()
    {
        var fullPath = Path.Combine(_projectRoot, "Resources", "Page.cy.resx");
        WriteResx("Resources/Page.cy.resx", ("message", "Old"));

        var changedValueCount = ResxResourceFile.UpdateValues(
            fullPath,
            new Dictionary<string, string> { ["message"] = "New" });

        Assert.That(changedValueCount, Is.EqualTo(1));
        Assert.That(ResxResourceFile.Read(fullPath)["message"], Is.EqualTo("New"));
    }

    [Test]
    public void XlsxWorkbookReader_ReadTranslatedRowsAsync_WhenWorkbookHasNoSheets_Throws()
    {
        var workbookPath = Path.Combine(_projectRoot, "no-sheets.xlsx");
        WriteWorkbookWithoutSheets(workbookPath);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            XlsxWorkbookReader.ReadTranslatedRowsAsync(workbookPath));

        Assert.That(exception!.Message, Is.EqualTo("Workbook does not contain any sheets."));
    }

    [Test]
    public void XlsxWorkbookReader_ReadTranslatedRowsAsync_WhenSheetRelationshipHasNoTarget_Throws()
    {
        var workbookPath = Path.Combine(_projectRoot, "missing-target.xlsx");
        WriteWorkbookWithMissingWorksheetTarget(workbookPath);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            XlsxWorkbookReader.ReadTranslatedRowsAsync(workbookPath));

        Assert.That(exception!.Message, Is.EqualTo("Workbook relationship \"rId1\" does not point to a worksheet."));
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

    private static PageProfile CreatePageWithResources(string id, string fileName, params ResourceSelection[] resources)
    {
        return new PageProfile
        {
            Id = id,
            FileName = fileName,
            Route = $"/{id}",
            View = $"Views/{id}.cshtml",
            FigmaUrl = null,
            Notes = $"{id} notes",
            FeatureFlags = ["FeatureManagement:Test"],
            AppSettings = new Dictionary<string, string> { ["ExampleSetting"] = "true" },
            TranslatorNotes = ["Translator note"],
            Resources = resources
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

    private static void WriteProfileJson(
        string profilePath,
        string name,
        string defaultOutputPath,
        string pageFileName,
        string sourceResourcePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(profilePath)!);
        File.WriteAllText(
            profilePath,
            $$"""
            {
              "name": "{{name}}",
              "sourceCulture": "en",
              "targetCulture": "cy",
              "defaultOutputPath": "{{defaultOutputPath}}",
              "pages": [
                {
                  "id": "page",
                  "fileName": "{{pageFileName}}",
                  "route": "/page",
                  "notes": "Page notes",
                  "figmaUrl": null,
                  "resources": [
                    {
                      "source": "{{sourceResourcePath}}",
                      "section": "Page section"
                    }
                  ]
                }
              ]
            }
            """);
    }

    private static void WriteSharedStringWorkbook(string workbookPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(workbookPath)!);

        using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Create);
        AddArchiveEntry(
            archive,
            "xl/workbook.xml",
            """
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                      xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets>
                <sheet name="Translations" sheetId="1" r:id="rId1" />
              </sheets>
            </workbook>
            """);
        AddArchiveEntry(
            archive,
            "xl/_rels/workbook.xml.rels",
            """
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml" />
            </Relationships>
            """);
        AddArchiveEntry(
            archive,
            "xl/sharedStrings.xml",
            """
            <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <si><t>Translation key</t></si>
              <si><t>Welsh</t></si>
              <si><t>Resources/Page.en.resx::message</t></si>
              <si><t>Cyfieithiad</t></si>
            </sst>
            """);
        AddArchiveEntry(
            archive,
            "xl/worksheets/sheet1.xml",
            """
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>
                <row r="1">
                  <c r="A1" t="s"><v>0</v></c>
                  <c r="B1" t="s"><v>1</v></c>
                </row>
                <row r="2">
                  <c r="A2" t="s"><v>2</v></c>
                  <c r="B2" t="s"><v>3</v></c>
                </row>
              </sheetData>
            </worksheet>
            """);
    }

    private static void WriteWorkbookWithoutSheets(string workbookPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(workbookPath)!);

        using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Create);
        AddArchiveEntry(
            archive,
            "xl/workbook.xml",
            """
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                      xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets />
            </workbook>
            """);
        AddArchiveEntry(
            archive,
            "xl/_rels/workbook.xml.rels",
            """
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships" />
            """);
    }

    private static void WriteWorkbookWithMissingWorksheetTarget(string workbookPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(workbookPath)!);

        using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Create);
        AddArchiveEntry(
            archive,
            "xl/workbook.xml",
            """
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                      xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets>
                <sheet name="Translations" sheetId="1" r:id="rId1" />
              </sheets>
            </workbook>
            """);
        AddArchiveEntry(
            archive,
            "xl/_rels/workbook.xml.rels",
            """
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" />
            </Relationships>
            """);
    }

    private static void AddArchiveEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
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

    private static async Task<(int ExitCode, string Out, string Error)> CaptureConsoleAsync(Func<Task<int>> action)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        Console.SetOut(outputWriter);
        Console.SetError(errorWriter);

        try
        {
            var exitCode = await action();
            return (exitCode, outputWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}

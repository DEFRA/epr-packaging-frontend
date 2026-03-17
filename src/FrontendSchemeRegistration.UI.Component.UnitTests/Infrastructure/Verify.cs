namespace FrontendSchemeRegistration.UI.Component.UnitTests.Infrastructure;

using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using VerifyTests.AngleSharp;

public static class VerifyHtml
{
    [ModuleInitializer]
    public static void Init() => VerifyAngleSharpDiffing.Initialize();

    public const string Extension = "html";

    public static readonly VerifySettings DefaultSettings = new();

    static VerifyHtml()
    {
        DefaultSettings.PrettyPrintHtml(nodes =>
        {
            nodes.ScrubAttributes("nonce");
            
            foreach (var node in nodes.QuerySelectorAll("input[name=\"__RequestVerificationToken\"]"))
                node.Attributes.GetNamedItem("value").Value = "[Scrubbed]";
        });
    }
}

public static class VerifyCsv
{
    [ModuleInitializer]
    public static void Init() => VerifyCsvHelper.Initialize();

    public const string Extension = "csv";
}

public class VerifyCheckTests
{
    [Test]
    public Task VerifyShouldBeConfigured() => VerifyChecks.Run();
}
using System.ComponentModel;
using System.Diagnostics;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright.NUnit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Example;

[TestFixture]
public class ExampleTest : PageTest
{
    private const string TestSuiteName = "Test";
    private const string WorkshopUrl = "https://broken-workshop.dequelabs.com/";

    // Resolve paths relative to the project root (tests run from bin/<config>/<tfm>/).
    private static readonly string ProjectRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    private static readonly string JsonReportDir = Path.Combine(ProjectRoot, "a11y-json");
    private static readonly string AxeCoreJsonDir = Path.Combine(ProjectRoot, "a11y-axe-core-json");
    private static readonly string ReportsDir = Path.Combine(ProjectRoot, "a11y-reports");

    // Report formats passed to `axe reporter` (html, csv, junit).
    private const string ReportFormats = "html,csv";

    [OneTimeSetUp]
    public void InitiateReporting()
    {
        // Equivalent of AxeConfiguration.configure().testSuiteName("Test").outputDirectory("a11y-json/")
        // Start from empty folders so old scans aren't reported or uploaded again.
        foreach (var dir in new[] { JsonReportDir, AxeCoreJsonDir })
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
            Directory.CreateDirectory(dir);
        }
    }

    [Test]
    public async Task test_a11y_workshop_homepage()
    {
        await Page.GotoAsync(WorkshopUrl);

        AxeResult results1 = await Page.RunAxe();
        LogResults(results1, "initial_scan");
    }

    [Test]
    public async Task test_recipe_card_a11y_workshop_homepage()
    {
        await Page.GotoAsync(WorkshopUrl);
        await Page.Locator(
            "#main-content > div.Recipes > div:nth-child(1) > div.Recipes__card-foot > button"
        ).ClickAsync();

        var context = new AxeRunContext
        {
            Include = new List<AxeSelector>
            {
                new AxeSelector("#main-content > div.Recipes > div:nth-child(2) > div"),
            },
        };

        AxeResult results2 = await Page.RunAxe(context);
        LogResults(results2, "modal_scan");
    }

    [OneTimeTearDown]
    public void Reporting()
    {
        // Turns the JSON files into HTML and CSV reports with the axe DevTools CLI:
        // https://docs.deque.com/devtools-for-web/4/en/cli-reports/#the-axe-reporter-command
        if (Directory.Exists(ReportsDir))
        {
            Directory.Delete(ReportsDir, recursive: true);
        }
        Directory.CreateDirectory(ReportsDir);

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "axe",
                ArgumentList = { "reporter", JsonReportDir, ReportsDir, "--format", ReportFormats },
                UseShellExecute = false,
            })!;
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                TestContext.Progress.WriteLine($"axe reporter exited with code {process.ExitCode}.");
            }
        }
        catch (Win32Exception)
        {
            TestContext.Progress.WriteLine(
                "axe DevTools CLI not found on PATH; skipping HTML/CSV report generation.");
        }
    }

    // Stand-in for axeSelenium.logResults(_reportOptions.uiState(...)). Writes two JSON files per
    // UI state:
    //   a11y-axe-core-json/ - the plain axe-core results, as axe.run() returns them
    //   a11y-json/          - the same results in the format the axe DevTools CLI saves, so that
    //                         `axe reporter` and `axe devhub` can read it
    private static void LogResults(AxeResult results, string uiState)
    {
        var name = $"{TestSuiteName}-{uiState}";

        // Every selector (target, xpath, ancestry) is written as an array, the way axe-core
        // returns them. axe DevTools requires this.
        var findings = JObject.Parse(JsonConvert.SerializeObject(
            results, AxeJsonSerializerSettings.WithFormatting(Formatting.None, arraySelectors: true)));

        var axeCoreFile = Path.Combine(AxeCoreJsonDir, $"{name}.json");
        File.WriteAllText(axeCoreFile, findings.ToString(Formatting.Indented));

        var devtoolsResult = new JObject
        {
            ["type"] = "axe-devtools-result",
            ["name"] = name,
            ["id"] = Guid.NewGuid().ToString(),
            ["endTime"] = (results.Timestamp ?? DateTimeOffset.Now).ToString("yyyy-MM-ddTHH:mm:sszzz"),
            ["platform"] = new JObject { ["userAgent"] = "", ["testMachine"] = "" },
            ["testSubject"] = new JObject { ["fileName"] = results.Url, ["lineNum"] = -1 },
            ["findings"] = findings,
        };

        var file = Path.Combine(JsonReportDir, $"{name}.json");
        File.WriteAllText(file, devtoolsResult.ToString(Formatting.Indented));
        TestContext.Progress.WriteLine(
            $"[{uiState}] {results.Violations.Length} violation(s) on {results.Url} -> {file}");
    }
}

using System.Diagnostics;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright.NUnit;

namespace Example;

[TestFixture]
public class ExampleTest : PageTest
{
    private const string TestSuiteName = "Test";
    private const string WorkshopUrl = "https://broken-workshop.dequelabs.com/";

    // Resolve paths relative to the project root (tests run from bin/<config>/<tfm>/).
    private static readonly string ProjectRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    private static readonly string JsonReportDir = Path.Combine(ProjectRoot, "axe-json-reports");
    private static readonly string ResultsDir = Path.Combine(ProjectRoot, "a11y-results");
    private static readonly string ReporterPath =
        Path.Combine(ProjectRoot, "src", "test", "resources", "reporter-cli-macos");

    [OneTimeSetUp]
    public void InitiateReporting()
    {
        // Equivalent of AxeConfiguration.configure().testSuiteName("Test").outputDirectory("axe-json-reports/")
        Directory.CreateDirectory(JsonReportDir);
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
        if (!File.Exists(ReporterPath))
        {
            TestContext.Progress.WriteLine(
                $"Reporter not found at {ReporterPath}; skipping HTML/CSV/XML report generation.");
            return;
        }

        Directory.CreateDirectory(ResultsDir);
        foreach (var format in new[] { "html", "csv", "xml" })
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = ReporterPath,
                    ArgumentList = { JsonReportDir, "--destination", ResultsDir, "--format", format },
                    UseShellExecute = false,
                });
                process?.WaitForExit();
            }
            catch (Exception e)
            {
                TestContext.Progress.WriteLine(e);
            }
        }
    }

    // Stand-in for axeSelenium.logResults(_reportOptions.uiState(...)): writes the raw
    // axe-core results as JSON into axe-json-reports/, one file per UI state.
    private static void LogResults(AxeResult results, string uiState)
    {
        var file = Path.Combine(JsonReportDir, $"{TestSuiteName}-{uiState}.json");
        File.WriteAllText(file, results.ToString());
        TestContext.Progress.WriteLine(
            $"[{uiState}] {results.Violations.Length} violation(s) on {results.Url} -> {file}");
    }
}

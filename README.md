# Axe + Playwright (C#) accessibility tests

Accessibility scans of the [Deque broken workshop site](https://broken-workshop.dequelabs.com/), using
[Playwright for .NET](https://playwright.dev/dotnet/) and
[Deque.AxeCore.Playwright](https://github.com/dequelabs/axe-core-nuget/tree/develop/packages/playwright).

This is a C# port of an earlier Java Selenium + axe DevTools example.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- PowerShell (`pwsh`), used to install the Playwright browsers

## Setup

```sh
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

If you don't have PowerShell, use the Node.js copy that ships with Playwright:

```sh
bin/Debug/net9.0/.playwright/node/*/node bin/Debug/net9.0/.playwright/package/cli.js install chromium
```

## Running the tests

```sh
dotnet test
```

To watch the browser while the tests run:

```sh
HEADED=1 dotnet test
```

## What the tests do

| Test | Steps | Results file name |
|---|---|---|
| `test_a11y_workshop_homepage` | Opens the homepage and scans the whole page. | `Test-initial_scan.json` |
| `test_recipe_card_a11y_workshop_homepage` | Opens the homepage, clicks the first recipe card's button and scans only the second recipe card. | `Test-modal_scan.json` |

Each scan is saved twice, with the same file name in two folders. Both hold the full results (violations, passes,
incomplete and inapplicable rules):

| Folder | Format | Use it for |
|---|---|---|
| `a11y-json/` | The format the axe DevTools CLI saves: the axe-core results wrapped with a name, id and page info | `axe reporter` and `axe devhub` |
| `a11y-axe-core-json/` | Plain axe-core results, as `axe.run()` returns them | Other tools that read axe-core results |

Both folders are emptied at the start of each run.

The tests don't fail when they find violations. To make them fail, add an assertion such as:

```cs
Assert.That(results1.Violations, Is.Empty);
```

## HTML and CSV reports

When all tests finish, the `[OneTimeTearDown]` step runs
[`axe reporter`](https://docs.deque.com/devtools-for-web/4/en/cli-reports/#the-axe-reporter-command) to turn the
JSON files into reports in `a11y-reports/`:

| File | Contents |
|---|---|
| `executive-report.html` | Summary across all scans |
| `Test-<ui-state>-<id>.html` | One detailed report per scan |
| `axe-devtools-results.csv` | Every result from every scan, one row per element |

This step needs the axe DevTools CLI on your `PATH` (check with `axe --version`). Without it, the step prints a
message and skips. `a11y-reports/` is emptied before each run.

To change the formats, edit `ReportFormats` in `ExampleTest.cs`. Add `junit` for an XML report.

You can also generate reports from existing results without running the tests:

```sh
axe reporter ./a11y-json ./a11y-reports --format=html,csv
```

## Uploading results to axe Developer Hub

`devhub.sh` sends the JSON files in `a11y-json/` to axe Developer Hub with the axe DevTools CLI
(`axe devhub`). You need:

- The axe DevTools CLI installed, so that the `axe` command is on your `PATH`
- Two environment variables:

| Variable | Value |
|---|---|
| `AXE_DEVHUB_API_KEY` | Your axe Developer Hub API key |
| `AXE_DEVHUB_CSHARP_PLAYWRIGHT` | The ID of the axe Developer Hub project to upload to |

Run the tests first, then upload:

```sh
export AXE_DEVHUB_API_KEY=<your-api-key>
export AXE_DEVHUB_CSHARP_PLAYWRIGHT=<your-project-id>

dotnet test
./devhub.sh
```

If the script isn't executable yet, run `chmod +x devhub.sh` first.

## GitHub Actions

`.github/workflows/axe-devhub.yml` runs on pushes and pull requests to `main`, and can also be started by hand. It:

1. Builds the tests and installs Playwright's Chromium
2. Runs `dotnet test`, which also generates the HTML and CSV reports
3. Uploads the results with `devhub.sh`
4. Saves `a11y-json/`, `a11y-axe-core-json/` and `a11y-reports/` as workflow artifacts
5. Runs `dequelabs/axe-devhub-action`, which fails the build if there are violations and comments on pull requests

Add these repository secrets:

| Secret | Value |
|---|---|
| `AXE_DEVHUB_API_KEY` | Your axe Developer Hub API key |
| `AXE_DEVHUB_CSHARP_PLAYWRIGHT` | The ID of the axe Developer Hub project |
| `AGORA_AUTH_TOKEN` | Token for Deque's Agora npm registry, used to install `@axe-devtools/cli` |
| `AGORA_EMAIL` | Email address for the Agora registry |

The CLI install step reads registry settings from an `.npmrc` at the repo root, which must use the two `AGORA_*`
variables.

The workflow also has commented-out steps that build and serve a local demo site on port 8888. They're there as an
example of scanning an app built from the repo instead of the public site.

## Project layout

```
ExampleTest.cs            # the tests
AxePlaywrightTests.csproj # project and NuGet package references
devhub.sh                 # uploads a11y-json/ to axe Developer Hub
a11y-json/                # JSON results, axe DevTools format (generated, git-ignored)
a11y-axe-core-json/       # JSON results, plain axe-core format (generated, git-ignored)
a11y-reports/             # reporter output (generated, git-ignored)
```

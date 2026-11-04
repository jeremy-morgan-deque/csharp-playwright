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

| Test | Steps | Results file |
|---|---|---|
| `test_a11y_workshop_homepage` | Opens the homepage and scans the whole page. | `axe-json-reports/Test-initial_scan.json` |
| `test_recipe_card_a11y_workshop_homepage` | Opens the homepage, clicks the first recipe card's button and scans only the second recipe card. | `axe-json-reports/Test-modal_scan.json` |

Each file holds the full axe-core results: violations, passes, incomplete and inapplicable rules. The tests don't
fail when they find violations. To make them fail, add an assertion such as:

```cs
Assert.That(results1.Violations, Is.Empty);
```

## HTML/CSV/XML reports (optional)

When all tests finish, the `[OneTimeTearDown]` step looks for the axe DevTools reporter at
`src/test/resources/reporter-cli-macos`. If the file is there, it uses the reporter to turn the JSON files into
HTML, CSV and XML reports in `a11y-results/`. If the file isn't there, the step prints a message and skips.

> **Note:** the axe DevTools reporter may expect results in the axe DevTools format. These tests save plain
> axe-core results, so the reporter may not accept them. This hasn't been tested.

## Project layout

```
ExampleTest.cs            # the tests
AxePlaywrightTests.csproj # project and NuGet package references
axe-json-reports/         # JSON results (generated, git-ignored)
a11y-results/             # reporter output (generated, git-ignored)
```

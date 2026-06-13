# Bankroll Manager

A maintainable C#/.NET bankroll manager for online poker tracking. The app replaces a spreadsheet-style bankroll tracker with a local WinForms desktop app, JSON persistence, clean first-run data, bankroll rules, stop-loss checks, and calculation tests.

## Projects

- `BankrollManager.Core` - models, bankroll calculations, rule engine, validation, JSON/CSV persistence.
- `BankrollManager.App` - .NET 8 WinForms desktop UI.
- `BankrollManager.Tests` - MSTest calculation tests.

## Requirements

- Windows
- .NET 8 SDK or newer

The app targets `net8.0-windows` and uses WinForms.

## Run

```powershell
dotnet run --project .\BankrollManager.App\BankrollManager.App.csproj
```

Or open `BankrollManager.sln` in Visual Studio and run `BankrollManager.App`.

## Test

```powershell
.\scripts\verify.ps1
```

The verify script runs the full solution test suite with artifacts written under `.verify/`, which keeps tests working even when the desktop app is open and locking the normal `bin` output.

## Publish

```powershell
.\scripts\publish.ps1 -Version v0.4.10
```

The publish script creates a self-contained `win-x64` package under `.verify\release` and Velopack installer/update assets under `.verify\release\velopack`. GitHub Actions also has a Release workflow: run it manually with a version label to produce artifacts, or push a `v*` tag to build the package and create a GitHub Release.

For the full Codex release flow, including version bumps, commits, tags, pushes, and workflow checks, see `RELEASE.md`.

Install `BankrollManager-win-Setup.exe` from a GitHub Release to enable in-app updates. The app's Updates button checks GitHub Releases, downloads the newest Velopack package, applies it, and restarts the app. Portable/debug copies can still run, but they cannot update themselves.

## Code Signing

Release builds can be signed automatically when GitHub Actions has a Windows code-signing certificate. Without these secrets, the release still builds, but Velopack leaves the installer and app files unsigned.

Add these repository secrets under GitHub Actions:

- `WINDOWS_SIGNING_CERTIFICATE_PFX_BASE64` - base64 text for the `.pfx` certificate file.
- `WINDOWS_SIGNING_CERTIFICATE_PASSWORD` - password for the `.pfx` file.
- `WINDOWS_SIGNING_TIMESTAMP_URL` - optional RFC 3161 timestamp server. If omitted, the workflow uses `http://timestamp.digicert.com`.

To convert a `.pfx` file to the base64 secret value:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\path\to\certificate.pfx")) | Set-Clipboard
```

The release workflow imports the certificate into the current-user certificate store, passes the thumbprint to Velopack/SignTool, signs with SHA-256, and timestamps the signature. Code signing improves Windows trust prompts, but SmartScreen reputation can still take time to build, especially for a new publisher certificate.

## Data

On first launch, the app creates:

```text
%APPDATA%\BankrollManager\Data\bankroll-data.json
```

Set `BANKROLL_MANAGER_DATA_DIR` to use a custom data folder. If the new AppData file does not exist yet, the app will copy an existing legacy file from `BankrollManager.App\bin\Debug\net8.0-windows\Data\bankroll-data.json`; otherwise, it creates a clean empty bankroll file.

On a clean empty bankroll, the app opens Quick Setup so you can set currency, enabled platforms, opening cash-bankroll funding, and optional starting wallet balances.

The JSON file includes a `DataSchemaVersion` marker. Older files are upgraded when loaded, and files from a future schema are rejected with a clear error instead of being overwritten.

## Main Features

- Overview with overall bankroll value, cash bankroll, ticket value, stop-loss/protect-mode status, open tournaments, recent activity, and value-inclusive charts.
- Quick Setup for first launch, with currency, platform, opening cash bankroll, first deposit, and starting wallet balance options.
- In-app update checks and one-click updates for installer-based builds.
- About dialog with app version, update mode, update source, pending restart status, and data file path.
- Interactive tutorial covering setup, platform filters, logging, reviews, safety controls, backups, updates, and GGPoker-specific formats.
- Platform wallets with expected cash, reconciled actual cash, cash exposure, ticket value, combined platform value, and transfer support.
- Tournament log with cash bounty, ticket wins, ticket buy-ins, cash cost, net profit, ROI, risk percentage, rule result, and cash-bankroll-after tracking.
- Cash log with session cost, net profit, BB won, BB/100, risk percentage, and profit-lock warning support.
- Ledger for deposits, withdrawals, bonuses, rakeback, ticket credits, corrections, and other bankroll movements.
- Daily, monthly, yearly, platform, format, and category reviews with separate cash P/L, ticket P/L, combined value P/L, hours played, hourly rate, and selected-day replay.
- Decision engine with PLAY / OK, SHOT OK, SHOT ONLY, PASS, TAKE BREAK, and FUND FIRST labels.
- EV Check tab for ticket or cash-prize tournaments, including breakeven entry count and positive/negative EV thresholds.
- Editable bankroll settings and category defaults.
- Save button plus autosave after add/edit/delete/settings changes.
- JSON import/export, CSV import/export, ChatGPT-readable Markdown export, and timestamped JSON backups.

## Notes

Deposits and withdrawals affect cash bankroll but are excluded from poker P/L. Cash bankroll is withdrawable cash plus cash poker results. Overall bankroll value is cash bankroll plus available ticket value, and value P/L includes ticket wins and ticket buy-ins.

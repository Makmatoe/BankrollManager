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
.\scripts\publish.ps1 -Version v0.3.0
```

The publish script creates a self-contained `win-x64` package under `.verify\release` and Velopack installer/update assets under `.verify\release\velopack`. GitHub Actions also has a Release workflow: run it manually with a version label to produce artifacts, or push a `v*` tag to build the package and create a GitHub Release.

Install `BankrollManager-win-Setup.exe` from a GitHub Release to enable in-app updates. The app's Updates button checks GitHub Releases, downloads the newest Velopack package, applies it, and restarts the app. Portable/debug copies can still run, but they cannot update themselves.

## Data

On first launch, the app creates:

```text
%APPDATA%\BankrollManager\Data\bankroll-data.json
```

Set `BANKROLL_MANAGER_DATA_DIR` to use a custom data folder. If the new AppData file does not exist yet, the app will copy an existing legacy file from `BankrollManager.App\bin\Debug\net8.0-windows\Data\bankroll-data.json`; otherwise, it creates a clean empty bankroll file.

On a clean empty bankroll, the app opens Quick Setup so you can set currency, enabled platforms, opening bankroll funding, and optional starting wallet balances.

The JSON file includes a `DataSchemaVersion` marker. Older files are upgraded when loaded, and files from a future schema are rejected with a clear error instead of being overwritten.

## Main Features

- Overview with bankroll KPIs, stop-loss/protect-mode status, open tournaments, recent activity, and charts.
- Quick Setup for first launch, with currency, platform, opening bankroll, first deposit, and starting wallet balance options.
- In-app update checks and one-click updates for installer-based builds.
- Platform wallets with expected cash, reconciled actual cash, differences, ticket value, and transfer support.
- Tournament log with cash bounty, ticket wins, ticket buy-ins, cash cost, net profit, ROI, risk percentage, rule result, and bankroll-after tracking.
- Cash log with session cost, net profit, BB won, BB/100, risk percentage, and profit-lock warning support.
- Ledger for deposits, withdrawals, bonuses, rakeback, ticket credits, corrections, and other bankroll movements.
- Daily, monthly, yearly, platform, format, and category reviews.
- Decision engine with PLAY / OK, SHOT OK, SHOT ONLY, PASS, TAKE BREAK, and FUND FIRST labels.
- Editable bankroll settings and category defaults.
- Save button plus autosave after add/edit/delete/settings changes.
- JSON import/export, CSV import/export, and timestamped JSON backups.

## Notes

Deposits and withdrawals affect current bankroll but are excluded from poker P/L. Poker P/L is tournament net profit plus cash net profit.

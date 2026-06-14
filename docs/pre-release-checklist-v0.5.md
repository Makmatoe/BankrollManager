# BankrollManager 0.5 Pre-release Checklist

Use this checklist before tagging or publishing the 0.5 release.

## Scope freeze

- Confirm the release branch contains only intended 0.5 hardening, workflow, audit, filtering, monthly review, table loading, and bug-fix changes.
- Confirm no JSON, CSV, or user data schema change is present unless it is explicitly documented and migration-tested.
- Review `git status --short` and `git diff --stat` for unexpected files.

## Automated verification

- Run `.\scripts\verify.ps1`.
- Run `dotnet format BankrollManager.sln --verify-no-changes --verbosity minimal --include <changed files>`.
- Run the release-scale tests and confirm the fake dataset covers thousands of tournaments, cash sessions, ledger entries, and timeline rows.
- Confirm UI tests for grid loading, sorting, filtering, dialogs, and table autosizing pass.

## Data safety

- Create a backup from the toolbar and confirm a timestamped JSON file appears under the Backups folder.
- Restore from that backup and confirm the app creates a safety backup before replacing current data.
- Import and export JSON with a copy of real data, then reload the app and compare dashboard totals.
- Export CSV and ChatGPT/Markdown output and confirm files are readable.

## Manual app smoke

- Open Overview, MTTs, Cash, Ledger, Timeline, Wallets, Data Audit, Monthly Review, Day, Month, Year, Decide, EV Check, and Settings.
- On MTTs, Cash, Ledger, and Timeline, apply each date range, search text, and quick filter; confirm `Showing X of Y`, `Load more`, and `Show all` remain correct.
- Sort large filtered tables by date, name, platform, cost, and result; confirm sorting applies to the full filtered set, not only the visible rows.
- Use chart/deep-link navigation to select older MTT, Cash, Ledger, and Timeline rows that are outside the initial visible window.
- Verify compact and details table layouts at narrow and wide window sizes.

## Workflow regressions

- Quick Add several repeated tournaments from presets, including flip, Flip & Go, satellite, and ticket-used entries.
- Bulk finish flip, Flip & Go, and satellite entries with cash result, ticket result, realized ticket, and no-result outcomes.
- Confirm quick-added entries show correct cash risk, ticket buy-in source platform, finish date/time, prize/ticket fields, bankroll before/after, and audit timeline rows after finishing.
- Edit, delete, reorder, favorite, copy, and apply presets; confirm old preset data still loads.
- Use ticket won and use-ticket flows across different platforms; confirm ticket balance by platform reconciles.

## Audit and review

- Open Data Audit and confirm bankroll breakdown reconciles for known-good data.
- Review platform wallet reconciliation, accepted differences, and issue navigation for MTT, Cash, Ledger, and Wallet targets.
- Confirm suspicious entries are detected: missing finish fields, impossible ticket states, old active sessions, negative costs, zero-cost prize entries without promo/freebie/ticket marking, and validation errors.
- Open Monthly Review for at least three months: a winning month, losing month, and sparse/empty month.
- Confirm cash P/L, value P/L, ticket P/L, hours, hourly rates, format/category/platform breakdowns, biggest wins/losses, stop-loss breaches, risk breaches, flip/satellite/ticket performance, and notes exports.

## Release decision

- Block release for any failed verification command, data-loss risk, schema mismatch, broken restore, unreconciled known-good data, or crash in the manual smoke path.
- Document known non-blocking risks in `RELEASE.md` or release notes.
- After approval, bump version metadata, update publish commands, commit intended files only, tag `v0.5.0`, push, and confirm the GitHub Release workflow completes.

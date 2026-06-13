# Release Guide

Use this when the user asks to release the current changes to GitHub.

## Repo Context

- GitHub repo: `Makmatoe/BankrollManager`
- Default branch: `main`
- Release tags: `v*`, for example `v0.4.9`
- Release workflow: `.github/workflows/release.yml`
- A pushed `v*` tag builds the Windows package and creates the GitHub Release.

## Before Releasing

1. Check the working tree:

   ```powershell
   git status -sb
   git diff --stat
   ```

2. If unrelated files are changed, do not stage them. Only release the files that belong to the user's requested change.

3. Pick the next version by checking the latest tag:

   ```powershell
   git tag --sort=-v:refname | Select-Object -First 5
   ```

4. Bump the version in `BankrollManager.App/BankrollManager.App.csproj`:

   ```xml
   <Version>0.4.9</Version>
   <AssemblyVersion>0.4.9.0</AssemblyVersion>
   <FileVersion>0.4.9.0</FileVersion>
   <InformationalVersion>0.4.9</InformationalVersion>
   ```

5. Update the README publish command to the same tag:

   ```powershell
   .\scripts\publish.ps1 -Version v0.4.9
   ```

## Verify

Run the full local verification before committing:

```powershell
.\scripts\verify.ps1
```

For code changes, also run formatting verification on the changed files when practical:

```powershell
dotnet format BankrollManager.sln --verify-no-changes --verbosity minimal --include <changed files>
```

## Commit And Tag

Stage only the intended files:

```powershell
git add -- <changed files>
git diff --cached --stat
git commit -m "<short release summary>"
```

Create the release tag on the commit:

```powershell
git tag v0.4.9
```

## Push To GitHub

Push `main`, then push the tag:

```powershell
git push origin main
git push origin v0.4.9
```

The tag push starts the Release workflow.

## Watch The Release

Find the run:

```powershell
gh run list --repo Makmatoe/BankrollManager --limit 10 --json databaseId,name,headBranch,headSha,status,conclusion,url
```

Watch the Release run for the new tag:

```powershell
gh run watch <run id> --repo Makmatoe/BankrollManager --exit-status
```

After it passes, confirm the GitHub Release exists:

```powershell
gh release view v0.4.9 --repo Makmatoe/BankrollManager --json tagName,name,url,isDraft,isPrerelease,assets
```

Also check CI on `main` if it was triggered:

```powershell
gh run list --repo Makmatoe/BankrollManager --branch main --limit 5
```

## Final Response Checklist

Tell the user:

- the released version tag
- the commit hash
- the GitHub Release URL
- whether the Release workflow passed
- whether local verification passed

Example:

```text
Released v0.4.9.

Release: https://github.com/Makmatoe/BankrollManager/releases/tag/v0.4.9
Commit: abc1234
Verification: .\scripts\verify.ps1 passed; GitHub Release workflow passed.
```


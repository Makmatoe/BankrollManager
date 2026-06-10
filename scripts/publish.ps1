[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "",
    [string]$OutputRoot = ".\.verify\release"
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDirectory
$repoRootFull = [System.IO.Path]::GetFullPath($repoRoot)
$projectPath = Join-Path $repoRootFull "BankrollManager.App\BankrollManager.App.csproj"

function Resolve-FromRepo {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRootFull $Path))
}

function Assert-PathInsideRepo {
    param([string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $repoPrefix = $repoRootFull.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

    if (!$fullPath.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to write outside the repository: $fullPath"
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    if (![string]::IsNullOrWhiteSpace($env:GITHUB_REF_NAME) -and $env:GITHUB_REF_TYPE -eq "tag") {
        $Version = $env:GITHUB_REF_NAME
    }
    else {
        $Version = (& git -C $repoRootFull rev-parse --short HEAD).Trim()
        if ([string]::IsNullOrWhiteSpace($Version)) {
            $Version = "local"
        }
    }
}

$safeVersion = $Version -replace "[^0-9A-Za-z._-]", "-"
$packageVersion = $Version.Trim()
if ($packageVersion.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    $packageVersion = $packageVersion.Substring(1)
}

if (!($packageVersion -match '^\d+\.\d+\.\d+([\-+][0-9A-Za-z\.-]+)?$')) {
    throw "Velopack requires a semantic version such as v0.3.0 or 0.3.0. Received: $Version"
}

$artifactName = "BankrollManager-$safeVersion-$Runtime"
$outputRootFull = Resolve-FromRepo $OutputRoot
$publishDirectory = Join-Path $outputRootFull $artifactName
$zipPath = Join-Path $outputRootFull "$artifactName.zip"
$velopackDirectory = Join-Path $outputRootFull "velopack"
$releaseNotesPath = Join-Path $outputRootFull "release-notes-$safeVersion.md"

Assert-PathInsideRepo $outputRootFull
Assert-PathInsideRepo $publishDirectory
Assert-PathInsideRepo $zipPath
Assert-PathInsideRepo $velopackDirectory
Assert-PathInsideRepo $releaseNotesPath

New-Item -ItemType Directory -Force $outputRootFull | Out-Null

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (Test-Path -LiteralPath $velopackDirectory) {
    Remove-Item -LiteralPath $velopackDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $releaseNotesPath) {
    Remove-Item -LiteralPath $releaseNotesPath -Force
}

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDirectory `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:DebugType=none `
    /p:DebugSymbols=false

Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -Force

$releaseNotes = @"
Bankroll Manager $Version

See the GitHub release notes for the full changelog.
"@
$releaseNotes | Out-File -FilePath $releaseNotesPath -Encoding utf8

dotnet tool restore
dotnet tool run vpk -- pack `
    --packId BankrollManager `
    --packTitle "Bankroll Manager" `
    --packAuthors "Makmatoe" `
    --packVersion $packageVersion `
    --packDir $publishDirectory `
    --mainExe "BankrollManager.App.exe" `
    --runtime $Runtime `
    --outputDir $velopackDirectory `
    --releaseNotes $releaseNotesPath `
    --shortcuts StartMenuRoot

if (![string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
    "artifact_name=$artifactName" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "zip_path=$zipPath" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "velopack_dir=$velopackDirectory" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "velopack_assets=$velopackDirectory\*" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
}

Write-Host "Created release package: $zipPath"
Write-Host "Created Velopack release assets: $velopackDirectory"

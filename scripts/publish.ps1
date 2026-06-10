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
$artifactName = "BankrollManager-$safeVersion-$Runtime"
$outputRootFull = Resolve-FromRepo $OutputRoot
$publishDirectory = Join-Path $outputRootFull $artifactName
$zipPath = Join-Path $outputRootFull "$artifactName.zip"

Assert-PathInsideRepo $outputRootFull
Assert-PathInsideRepo $publishDirectory
Assert-PathInsideRepo $zipPath

New-Item -ItemType Directory -Force $outputRootFull | Out-Null

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
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

if (![string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
    "artifact_name=$artifactName" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "zip_path=$zipPath" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
}

Write-Host "Created release package: $zipPath"

[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$ArtifactsPath = ".\.verify\solution-test-current"
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDirectory
$solutionPath = Join-Path $repoRoot "BankrollManager.sln"

if ([System.IO.Path]::IsPathRooted($ArtifactsPath)) {
    $resolvedArtifactsPath = $ArtifactsPath
}
else {
    $resolvedArtifactsPath = Join-Path $repoRoot $ArtifactsPath
}

dotnet test $solutionPath --configuration $Configuration --artifacts-path $resolvedArtifactsPath

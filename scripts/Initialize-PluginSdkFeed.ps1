[CmdletBinding()]
param(
    [string]$FeedPath,
    [string]$HostRepositoryPath,
    [string]$SharedContractProjectPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($FeedPath)) {
    $FeedPath = Join-Path $repositoryRoot "packages"
}

if ([string]::IsNullOrWhiteSpace($HostRepositoryPath)) {
    $HostRepositoryPath = (Resolve-Path (Join-Path $repositoryRoot "..\LanMountainDesktop")).Path
}

if ([string]::IsNullOrWhiteSpace($SharedContractProjectPath)) {
    $SharedContractProjectPath = Join-Path $repositoryRoot "LanMountainDesktop.SharedContracts.SampleClock\LanMountainDesktop.SharedContracts.SampleClock.csproj"
}

$projects = @(
    (Join-Path $HostRepositoryPath "LanMountainDesktop.Shared.Contracts\LanMountainDesktop.Shared.Contracts.csproj"),
    (Join-Path $HostRepositoryPath "LanMountainDesktop.PluginIsolation.Contracts\LanMountainDesktop.PluginIsolation.Contracts.csproj"),
    (Join-Path $HostRepositoryPath "LanMountainDesktop.Shared.IPC\LanMountainDesktop.Shared.IPC.csproj"),
    (Join-Path $HostRepositoryPath "LanMountainDesktop.PluginSdk\LanMountainDesktop.PluginSdk.csproj"),
    $SharedContractProjectPath
)

foreach ($project in $projects) {
    if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
        throw "Required package project '$project' was not found."
    }
}

$sdkInfoPath = Join-Path $HostRepositoryPath "LanMountainDesktop.PluginSdk\PluginSdkInfo.cs"
$sdkInfo = Get-Content -Raw -Encoding utf8 -LiteralPath $sdkInfoPath
if ($sdkInfo -notmatch 'ApiVersion\s*=\s*"5\.0\.0"') {
    throw "The host repository does not expose Plugin API 5.0.0 at '$sdkInfoPath'."
}

New-Item -ItemType Directory -Force -Path $FeedPath | Out-Null
$resolvedFeedPath = (Resolve-Path -LiteralPath $FeedPath).Path

foreach ($project in $projects) {
    Write-Host "Packing $project"
    dotnet pack $project -c Release -o $resolvedFeedPath -p:ContinuousIntegrationBuild=true
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for '$project' with exit code $LASTEXITCODE."
    }
}

$requiredPackages = @(
    "LanMountainDesktop.Shared.Contracts.5.0.0.nupkg",
    "LanMountainDesktop.PluginIsolation.Contracts.5.0.0.nupkg",
    "LanMountainDesktop.Shared.IPC.5.0.0.nupkg",
    "LanMountainDesktop.PluginSdk.5.0.0.nupkg",
    "LanMountainDesktop.SharedContracts.SampleClock.2.0.0.nupkg"
)

foreach ($packageName in $requiredPackages) {
    $packagePath = Join-Path $resolvedFeedPath $packageName
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        throw "Expected package '$packagePath' was not produced."
    }
}

$localPackagesRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot ".nuget\packages"))
$localNuGetRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot ".nuget"))
if (-not $localPackagesRoot.StartsWith($localNuGetRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clear unexpected NuGet cache path '$localPackagesRoot'."
}
if (Test-Path -LiteralPath $localPackagesRoot) {
    Remove-Item -LiteralPath $localPackagesRoot -Recurse -Force
}

Write-Host "LanMountainDesktop PluginSdk 5.0.0 feed initialized at '$resolvedFeedPath'."

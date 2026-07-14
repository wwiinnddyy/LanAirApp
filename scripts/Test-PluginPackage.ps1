[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,
    [switch]$RequireCanonicalFileName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
if ([IO.Path]::GetExtension($resolvedPackagePath) -ne ".laapp") {
    throw "Plugin package '$resolvedPackagePath' must use the .laapp extension."
}

function Get-JsonPropertyValue([object]$Object, [string]$Name) {
    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $entries = @($archive.Entries | Where-Object { -not [string]::IsNullOrWhiteSpace($_.Name) })
    $entryNames = @($entries | ForEach-Object { $_.FullName.Replace('\', '/') })

    foreach ($entryName in $entryNames) {
        $segments = @($entryName.Split('/', [StringSplitOptions]::RemoveEmptyEntries))
        if ([IO.Path]::IsPathRooted($entryName) -or
            $entryName -eq ".." -or
            $entryName.StartsWith("../", [StringComparison]::Ordinal) -or
            $segments -contains "..") {
            throw "Plugin package contains unsafe entry '$entryName'."
        }
    }

    $manifestEntry = $entries | Where-Object { $_.FullName.Replace('\', '/') -eq "plugin.json" } | Select-Object -First 1
    if ($null -eq $manifestEntry) {
        throw "Plugin package does not contain plugin.json at its root."
    }

    $reader = [IO.StreamReader]::new($manifestEntry.Open())
    try {
        $manifest = $reader.ReadToEnd() | ConvertFrom-Json
    }
    finally {
        $reader.Dispose()
    }

    $apiVersion = [string](Get-JsonPropertyValue $manifest "apiVersion")
    if ($apiVersion -ne "5.0.0") {
        throw "Plugin package targets API '$apiVersion' instead of 5.0.0."
    }

    $runtimeMode = "in-proc"
    $runtime = Get-JsonPropertyValue $manifest "runtime"
    if ($null -ne $runtime) {
        $declaredRuntimeMode = [string](Get-JsonPropertyValue $runtime "mode")
        if (-not [string]::IsNullOrWhiteSpace($declaredRuntimeMode)) {
            $runtimeMode = $declaredRuntimeMode
        }
    }

    if ($runtimeMode -notin @("in-proc", "isolated-background", "isolated-window")) {
        throw "Plugin package declares unsupported runtime mode '$runtimeMode'."
    }

    $entranceAssembly = [string](Get-JsonPropertyValue $manifest "entranceAssembly")
    if ([string]::IsNullOrWhiteSpace($entranceAssembly) -or
        $entryNames -notcontains $entranceAssembly) {
        throw "Plugin package does not contain entrance assembly '$entranceAssembly'."
    }

    $forbiddenEntries = @($entryNames | Where-Object {
        $leafName = [IO.Path]::GetFileName($_)
        $leafName -eq "LanMountainDesktop.PluginSdk.dll" -or
        $leafName -like "Avalonia*.dll"
    })

    foreach ($contract in @((Get-JsonPropertyValue $manifest "sharedContracts"))) {
        if ($null -ne $contract -and
            -not [string]::IsNullOrWhiteSpace([string]$contract.assemblyName) -and
            $entryNames -contains [string]$contract.assemblyName) {
            $forbiddenEntries += [string]$contract.assemblyName
        }
    }

    if ($forbiddenEntries.Count -gt 0) {
        throw "Plugin package contains host-owned assemblies: $($forbiddenEntries -join ', ')."
    }

    if ($RequireCanonicalFileName) {
        $pluginId = [string](Get-JsonPropertyValue $manifest "id")
        $pluginVersion = [string](Get-JsonPropertyValue $manifest "version")
        if ([string]::IsNullOrWhiteSpace($pluginId) -or [string]::IsNullOrWhiteSpace($pluginVersion)) {
            throw "Plugin package must declare non-empty id and version values."
        }

        $expectedName = "$pluginId.$pluginVersion.laapp"
        if ([IO.Path]::GetFileName($resolvedPackagePath) -ne $expectedName) {
            throw "Plugin package file name must be '$expectedName'."
        }
    }

    Write-Host "Validated Plugin API 5 package '$resolvedPackagePath' ($($entries.Count) files)."
}
finally {
    $archive.Dispose()
}

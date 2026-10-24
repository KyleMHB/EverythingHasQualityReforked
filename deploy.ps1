param(
    [Parameter(Mandatory)][string]$Destination,
    [string]$PackagePath = (Join-Path $PSScriptRoot 'artifacts/package/ConfigurableQuality')
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $PackagePath
$source = (Resolve-Path -LiteralPath $PackagePath).Path
$target = [IO.Path]::GetFullPath($Destination).TrimEnd('\', '/')
# Restrict replacement to this mod's explicitly named installation, never the Mods root.
if ([IO.Path]::GetFileName($target) -notin @('ConfigurableQuality', 'EverythingHasQualityReforked') -or $target -eq $PSScriptRoot -or $target.StartsWith($PSScriptRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destination must be an external folder named ConfigurableQuality.'
}
if (Test-Path -LiteralPath (Join-Path $target 'About/About.xml')) {
    [xml]$existing = Get-Content -LiteralPath (Join-Path $target 'About/About.xml') -Raw
    if ($existing.ModMetaData.packageId -notin @('KyleMHB.ConfigurableQuality', 'Cozarkian.QualityFramework')) { throw 'Destination belongs to another mod.' }
} elseif (Test-Path -LiteralPath $target) {
    if (@(Get-ChildItem -LiteralPath $target -Force).Count -gt 0) { throw 'Destination is nonempty and has no matching mod metadata.' }
}
if ($source -eq $target -or $source.StartsWith($target + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Destination overlaps the prepared package.' }
if (Test-Path -LiteralPath $target) {
    foreach ($entry in Get-ChildItem -LiteralPath $target -Force) {
        if ($entry.Name -notin @('About', 'Common', '1.6', '1.7', 'Assemblies', 'LoadFolders.xml', 'LICENSE', 'build-manifest.json')) {
            throw "Destination contains an unowned entry: $($entry.Name). Use a clean installation folder."
        }
    }
}
New-Item -ItemType Directory -Path $target -Force | Out-Null
# Replace only owned package folders, including obsolete shared/older-version assemblies.
foreach ($owned in @('About', 'Common', '1.6', '1.7', 'Assemblies')) {
    $ownedPath = [IO.Path]::GetFullPath((Join-Path $target $owned))
    if (-not $ownedPath.StartsWith($target + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid owned destination.' }
    if (Test-Path -LiteralPath $ownedPath) { Remove-Item -LiteralPath $ownedPath -Recurse -Force }
}
foreach ($entry in @('About', 'Common', '1.6', '1.7', 'LoadFolders.xml', 'LICENSE', 'build-manifest.json')) {
    Copy-Item -LiteralPath (Join-Path $source $entry) -Destination $target -Recurse -Force
}
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $target
Write-Output "Deployed package to $target"

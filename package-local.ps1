param([Parameter(Mandatory)][string]$ManagedPath, [string]$DotNetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
# Build first. A failed build leaves the previous candidate intact.
foreach ($version in @('1.6', '1.7')) {
    & (Join-Path $PSScriptRoot 'build-local.ps1') -GameVersion $version -ManagedPath $ManagedPath -DotNetPath $DotNetPath -Configuration Release
}
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'artifacts'))
$stage = [IO.Path]::GetFullPath((Join-Path $artifactRoot 'package/EverythingHasQualityReforked'))
if (-not $stage.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Package stage must stay under artifacts.' }
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'About') -Destination $stage -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LoadFolders.xml') -Destination $stage
foreach ($folder in @('Defs', 'Languages', 'Textures', 'Sounds', 'Patches')) {
    $source = Join-Path $PSScriptRoot "Common/$folder"
    if (Test-Path -LiteralPath $source) {
        New-Item -ItemType Directory -Path (Join-Path $stage 'Common') -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination (Join-Path $stage 'Common') -Recurse
    }
}
foreach ($version in @('1.6', '1.7')) {
    $assemblyDirectory = Join-Path $stage "$version/Assemblies"
    New-Item -ItemType Directory -Path $assemblyDirectory -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "artifacts/$version/Release/QualityEverything.dll") -Destination $assemblyDirectory
}
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $stage
Write-Output "Package ready: $stage"

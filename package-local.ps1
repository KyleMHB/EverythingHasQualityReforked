param([Parameter(Mandatory)][string]$ManagedPath, [string]$DotNetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
function Get-SourceInputs {
    @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Source') -File -Recurse |
        Where-Object { $_.Extension -in @('.cs', '.csproj') -and $_.FullName -notmatch '[\\/](obj|bin)[\\/]' } |
        Sort-Object FullName | ForEach-Object {
            [ordered]@{ path = $_.FullName.Substring($PSScriptRoot.Length + 1).Replace('\', '/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
        })
}
$sourceInputs = Get-SourceInputs
# Build first. A failed build leaves the previous candidate intact.
foreach ($version in @('1.6', '1.7')) {
    & (Join-Path $PSScriptRoot 'build-local.ps1') -GameVersion $version -ManagedPath $ManagedPath -DotNetPath $DotNetPath -Configuration Release
}
if (($sourceInputs | ConvertTo-Json -Depth 4 -Compress) -ne ((Get-SourceInputs) | ConvertTo-Json -Depth 4 -Compress)) {
    throw 'Source changed during packaging. Rebuild both targets from the same source.'
}
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'artifacts'))
$stage = [IO.Path]::GetFullPath((Join-Path $artifactRoot 'package/ConfigurableQuality'))
if (-not $stage.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Package stage must stay under artifacts.' }
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'About') -Destination $stage -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LoadFolders.xml') -Destination $stage
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination $stage
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
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "artifacts/$version/Release/ConfigurableQuality.dll") -Destination $assemblyDirectory
}
$commit = & git -C $PSScriptRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify the source commit.' }
$status = @(& git -C $PSScriptRoot status --porcelain)
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify working-tree changes.' }
$sdk = & $DotNetPath --version
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify the build SDK.' }
[xml]$project = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Source/ConfigurableQuality.csproj') -Raw
$versionPath = Join-Path $ManagedPath 'Version.txt'
$manifest = [ordered]@{
    schemaVersion = 1
    source = [ordered]@{ commit = "$commit".Trim(); dirty = ($status.Count -gt 0); inputs = $sourceInputs }
    sdk = "$sdk".Trim()
    references = [ordered]@{
        rimWorld16 = $project.SelectSingleNode('//RimWorldReferenceVersion').InnerText
        harmony = $project.SelectSingleNode("//PackageReference[@Include='Lib.Harmony']").GetAttribute('Version')
        rimWorld17 = if (Test-Path -LiteralPath $versionPath) { (Get-Content -LiteralPath $versionPath -Raw).Trim() } else { 'Version.txt unavailable' }
        managed17 = @(foreach ($name in @('Assembly-CSharp.dll', 'UnityStubs.dll', 'Waylith.UnityCompat.dll')) {
            [ordered]@{ path = $name; sha256 = (Get-FileHash -LiteralPath (Join-Path $ManagedPath $name) -Algorithm SHA256).Hash }
        })
    }
    files = @(Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path = $_.FullName.Substring($stage.Length + 1).Replace('\', '/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $stage 'build-manifest.json') -Encoding UTF8
& (Join-Path $PSScriptRoot 'validate-package.ps1') -PackagePath $stage
Write-Output "Package ready: $stage"

param([Parameter(Mandatory)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PackagePath).Path
$allowedRoots = @('About', 'Common', '1.6', '1.7', 'LoadFolders.xml')
foreach ($entry in Get-ChildItem -LiteralPath $root -Force) {
    if ($entry.Name -notin $allowedRoots) { throw "Unexpected package entry: $($entry.Name)" }
}
[xml]$folders = Get-Content -LiteralPath (Join-Path $root 'LoadFolders.xml') -Raw
foreach ($version in @('1.6', '1.7')) {
    $entries = @($folders.loadFolders.SelectSingleNode("v$version").li)
    if ($entries.Count -ne 2 -or $entries[0] -ne $version -or $entries[1] -ne 'Common') { throw "Incorrect load folders for $version." }
    $assemblies = @(Get-ChildItem -LiteralPath (Join-Path $root "$version/Assemblies") -File -Recurse)
    if ($assemblies.Count -ne 1 -or $assemblies[0].Name -ne 'QualityEverything.dll') { throw "Expected exactly one mod assembly for $version." }
    $identity = [Reflection.AssemblyName]::GetAssemblyName($assemblies[0].FullName)
    if ($identity.Name -ne 'QualityEverything') { throw "Incorrect assembly identity for $version." }
    # Metadata remains readable without loading either game's runtime.
    $bytes = [IO.File]::ReadAllBytes($assemblies[0].FullName)
    $metadata = [Text.Encoding]::UTF8.GetString($bytes)
    $framework = if ($version -eq '1.6') { '.NETFramework,Version=v4.8' } else { '.NETCoreApp,Version=v10.0' }
    if (-not $metadata.Contains($framework)) { throw "Incorrect target framework for $version." }
}
foreach ($file in Get-ChildItem -LiteralPath $root -File -Recurse -Force) {
    $relative = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
    if ($file.Extension -eq '.dll' -and $relative -notin @('1.6/Assemblies/QualityEverything.dll', '1.7/Assemblies/QualityEverything.dll')) { throw "Unexpected assembly: $relative" }
    if ($file.Extension -in @('.pdb', '.exe', '.deps.json', '.runtimeconfig.json') -or $file.Name -match '\.(deps|runtimeconfig)\.json$') { throw "Runtime/debug artifact: $relative" }
    if ($file.Extension -eq '.xml') { [xml](Get-Content -LiteralPath $file.FullName -Raw) | Out-Null }
}
[xml]$about = Get-Content -LiteralPath (Join-Path $root 'About/About.xml') -Raw
if ('brrainz.harmony' -notin @($about.ModMetaData.modDependencies.li.packageId)) { throw 'External Harmony dependency is missing.' }
Write-Output 'Package layout, assembly targets, dependency metadata and XML are valid.'

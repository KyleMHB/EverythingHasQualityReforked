param(
    [ValidateSet('1.6', '1.7')][string]$GameVersion = '1.6',
    [string]$ManagedPath,
    [string]$DotNetPath = 'dotnet',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$arguments = @('build', (Join-Path $PSScriptRoot 'Source/QualityEverything.csproj'), '-c', $Configuration,
    "-p:RimWorldVersion=$GameVersion", '-p:NuGetAudit=false', '-p:UseSharedCompilation=false')
if ($GameVersion -eq '1.7') {
    foreach ($assembly in @('Assembly-CSharp.dll', 'UnityStubs.dll', 'Waylith.UnityCompat.dll')) {
        if (-not $ManagedPath -or -not (Test-Path -LiteralPath (Join-Path $ManagedPath $assembly) -PathType Leaf)) {
            throw "For 1.7, supply -ManagedPath containing $assembly. The game directory is read only."
        }
    }
    $arguments += "-p:RimWorldManagedPath=$((Resolve-Path -LiteralPath $ManagedPath).Path)"
}
& $DotNetPath @arguments
if ($LASTEXITCODE -ne 0) { throw "Local $GameVersion build failed with exit code $LASTEXITCODE." }

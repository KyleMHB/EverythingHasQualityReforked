param(
    [ValidateSet('1.6', '1.7')][string]$GameVersion = '1.6',
    [string]$ManagedPath,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'Source/QualityEverything.csproj'
$buildArguments = @('build', $projectPath, '-c', $Configuration, "-p:RimWorldVersion=$GameVersion")
if ($GameVersion -eq '1.7') {
    if (-not $ManagedPath -or -not (Test-Path -LiteralPath (Join-Path $ManagedPath 'Assembly-CSharp.dll'))) {
        throw 'For 1.7, supply -ManagedPath pointing to the game assemblies. It is read only; no deployment is performed.'
    }
    $buildArguments += "-p:RimWorldManagedPath=$((Resolve-Path -LiteralPath $ManagedPath).Path)"
}
& dotnet @buildArguments
if ($LASTEXITCODE -ne 0) { throw "Local $GameVersion build failed with exit code $LASTEXITCODE." }

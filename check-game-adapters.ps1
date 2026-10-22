param([Parameter(Mandatory)][string]$ManagedPath, [string]$DotNetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$modPath = Join-Path $PSScriptRoot 'artifacts/1.7/Release/QualityEverything.dll'
if (-not (Test-Path -LiteralPath $modPath -PathType Leaf)) { throw 'Build the 1.7 Release target first.' }
if (-not (Test-Path -LiteralPath (Join-Path $ManagedPath 'Assembly-CSharp.dll') -PathType Leaf)) { throw 'Supply a 1.7 managed assembly directory.' }
& $DotNetPath run --project (Join-Path $PSScriptRoot 'Tests/GameAdapterChecks.csproj') -p:NuGetAudit=false -p:UseSharedCompilation=false -- (Resolve-Path -LiteralPath $ManagedPath).Path $modPath
if ($LASTEXITCODE -ne 0) { throw "Game adapter checks failed with exit code $LASTEXITCODE." }

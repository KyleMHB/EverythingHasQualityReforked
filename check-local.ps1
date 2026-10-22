param([string]$DotNetPath = 'dotnet', [string]$PackagePath)
$ErrorActionPreference = 'Stop'
# Distinct completion messages catch accidental reuse of another project's apphost.
$checks = [ordered]@{
    FurnitureChecks = '8 linked furniture calculation checks passed.'
    SettingsChecks = 'Resolver, migration, roundtrip, validation and draft isolation checks passed.'
    EquipmentChecks = 'Equipment checks passed: 9 contribution calculations.'
    ProductionChecks = 'Production context checks passed.'
    ConsumableChecks = 'Consumable checks passed (mixed doses, cap, decay, disabled/untracked, invalid snapshots, surgical dose bookkeeping)'
    DurabilityChecks = 'Durability checks passed: 13 condition transitions.'
    EnergyChecks = '7 energy state checks passed.'
}
foreach ($check in $checks.GetEnumerator()) {
    Write-Output "Running $($check.Key)"
    $output = @(& $DotNetPath run --project (Join-Path $PSScriptRoot "Tests/$($check.Key).csproj") -p:NuGetAudit=false -p:UseSharedCompilation=false 2>&1)
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Output "$_" }
    if ($exitCode -ne 0) { throw "$($check.Key) failed with exit code $exitCode." }
    if (@($output | Where-Object { "$_" -eq $check.Value }).Count -ne 1) {
        throw "$($check.Key) did not report its own completion message. Check project build isolation."
    }
}
Write-Output "All $($checks.Count) check programs passed."
if ($PackagePath) { & (Join-Path $PSScriptRoot 'Tests/PackageChecks.ps1') -PackagePath $PackagePath }

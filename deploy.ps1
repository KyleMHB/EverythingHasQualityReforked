$ErrorActionPreference = "Stop"

$toolPath = Join-Path $PSScriptRoot "..\_Shared\RimWorldModTools.ps1"
. $toolPath

Invoke-RimWorldModDeploy `
    -ModName "EverythingHasQualityReforked" `
    -SourceRoot $PSScriptRoot `
    -BuildPath (Join-Path $PSScriptRoot "Source\QualityEverything.csproj") `
    -Configuration "Release" `
    -DotNetHome (Join-Path $PSScriptRoot ".dotnet") `
    -Folders @("About", "Common", "1.6") `
    -Files @("LoadFolders.xml") `
    -RemoveFilePatterns @("*.pdb")

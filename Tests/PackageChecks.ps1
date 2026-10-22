param([string]$PackagePath = (Join-Path $PSScriptRoot '../artifacts/package/EverythingHasQualityReforked'))
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$temporaryParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
$fixtureName = 'ehq-package-checks-' + [guid]::NewGuid().ToString('N')
$fixture = [IO.Path]::GetFullPath((Join-Path $temporaryParent $fixtureName))
# The fixture is outside the repository so it exercises the real deployment guards.
if (-not $fixture.StartsWith($temporaryParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid fixture path.' }
$candidateParent = Join-Path $fixture 'candidate'
$candidate = Join-Path $candidateParent 'EverythingHasQualityReforked'
$installed = Join-Path $fixture 'installed/EverythingHasQualityReforked'
function Expect-Rejection([string]$Expected) {
    try { & (Join-Path $repository 'validate-package.ps1') -PackagePath $candidate | Out-Null }
    catch {
        if ($_.Exception.Message.Contains($Expected)) { return }
        throw
    }
    throw "Invalid package was accepted: $Expected"
}
try {
    New-Item -ItemType Directory -Path $candidateParent -Force | Out-Null
    Copy-Item -LiteralPath $package -Destination $candidate -Recurse
    Remove-Item -LiteralPath (Join-Path $candidate 'LICENSE')
    Expect-Rejection 'complete inherited LICENSE'
    Copy-Item -LiteralPath (Join-Path $package 'LICENSE') -Destination $candidate
    $content = Join-Path $candidate 'Common/Defs/Inspirations.xml'
    Add-Content -LiteralPath $content -Value '<!-- changed after packaging -->'
    Expect-Rejection 'differs from its build manifest'
    Copy-Item -LiteralPath (Join-Path $package 'Common/Defs/Inspirations.xml') -Destination $content -Force
    & (Join-Path $repository 'deploy.ps1') -Destination $installed -PackagePath $candidate
    # A second deployment must accept the added root files and retire obsolete shared assemblies.
    $obsolete = Join-Path $installed 'Assemblies'
    New-Item -ItemType Directory -Path $obsolete -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $obsolete 'obsolete.dll') -Value 'fixture'
    & (Join-Path $repository 'deploy.ps1') -Destination $installed -PackagePath $candidate
    if (Test-Path -LiteralPath $obsolete) { throw 'Obsolete shared assembly directory survived deployment.' }
    foreach ($file in Get-ChildItem -LiteralPath $package -File -Recurse) {
        $relative = $file.FullName.Substring($package.Length + 1)
        if ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath (Join-Path $installed $relative) -Algorithm SHA256).Hash) {
            throw "Deployment changed package contents: $relative"
        }
    }
    Write-Output 'Package checks passed: missing license rejected, changed content rejected, repeat deployment preserves all files.'
}
finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if ($resolvedFixture -ne (Join-Path $temporaryParent $fixtureName) -or
        -not $resolvedFixture.StartsWith($temporaryParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}

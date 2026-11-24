# release-csharp.ps1 — build + package + publish the C#/WPF gadgets to GitHub Releases.
#
# Two packaging modes:
#   * default (framework-dependent): small exes, one zip per gadget with
#     exe + GadgetCore.dll + deps.json + runtimeconfig.json (NO .pdb). The target
#     machine must have the .NET 8 Desktop Runtime installed.
#   * -SelfContained: PublishSingleFile + SelfContained win-x64 → ONE big exe per
#     gadget with the whole .NET runtime bundled; runs anywhere, no install needed.
#
# Tag format: <gadget>/<yyyyMMdd_HHmm> (matches UpdateService expectations). Self-
# contained tags get a "-sc" suffix so they never collide with framework builds.
#
# Usage (from repo root):
#   .\release-csharp.ps1 [-Stamp yyyyMMdd_HHmm] [-SelfContained]
param(
    [string]$Stamp = (Get-Date -Format 'yyyyMMdd_HHmm'),
    [switch]$SelfContained,
    [string]$NotesFile = ''
)
$ErrorActionPreference = 'Stop'
$Repo = 'HectorBravo/gadgets'
$Root = $PSScriptRoot

# Optional release notes (markdown) shared by all gadget releases of this stamp.
$Notes = ''
if ($NotesFile -and (Test-Path $NotesFile)) { $Notes = Get-Content $NotesFile -Raw }

# gadget slug -> assembly name
$Gadgets = [ordered]@{
    'cpu-meter'     = 'CpuMeter'
    'drives-meter'  = 'DrivesMeter'
    'network-meter' = 'NetworkMeter'
    'digital-clock' = 'DigitalClock'
}

foreach ($slug in $Gadgets.Keys) {
    $asm = $Gadgets[$slug]
    $proj = Join-Path $Root "src\$asm\$asm.csproj"
    $pubDir = Join-Path $Root "src\$asm\bin\publish$(if ($SelfContained) { '-sc' })"

    if (Test-Path $pubDir) { Remove-Item -LiteralPath $pubDir -Recurse -Force }

    Write-Host "`n=== Publishing $asm ($slug) stamp=$Stamp $(if ($SelfContained) {'[self-contained single-file]'}) ===" -ForegroundColor Cyan
    if ($SelfContained) {
        dotnet publish $proj -c Release -o $pubDir -r win-x64 --self-contained true `
            -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --nologo
    } else {
        dotnet publish $proj -c Release -o $pubDir --nologo
    }
    if ($LASTEXITCODE -ne 0) { throw "publish $asm FAILED" }

    # Stamp the published binary(ies) with the release time (UpdateService compares its own LastWriteTime).
    foreach ($f in @("$pubDir\$asm.dll", "$pubDir\$asm.exe")) {
        if (Test-Path $f) { (Get-Item $f).LastWriteTime = Get-Date }
    }

    # Zip exe + dlls + json only (no .pdb). For single-file this is just the one exe.
    $files = Get-ChildItem $pubDir | Where-Object { $_.Extension.TrimStart('.') -in @('exe','dll','json') }
    $suffix = if ($SelfContained) { '-sc' } else { '' }
    $zip = Join-Path $env:TEMP "$slug-$Stamp$suffix.zip"
    if (Test-Path $zip) { Remove-Item -LiteralPath $zip }
    Compress-Archive -Path $files.FullName -DestinationPath $zip

    $tag = "$slug/$Stamp$(if ($SelfContained) { '-sc' })"
    # Idempotency: skip if this exact tag already exists.
    $prevEa = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    gh release view $tag --repo $Repo 2>$null | Out-Null
    $exists = $LASTEXITCODE -eq 0
    $ErrorActionPreference = $prevEa
    if ($exists) { Write-Host "[skip] $tag already exists" -ForegroundColor Gray; continue }

    if ($SelfContained) {
        $notes = "C#/WPF build of the $asm gadget (stamp $Stamp). Self-contained single-file: no .NET install required, runs on any 64-bit Windows. Contents: $asm.exe.`n`n" + $Notes
    } else {
        $notes = "C#/WPF build of the $asm gadget (stamp $Stamp). Framework-dependent: install the .NET 8 Desktop Runtime to run. Contents: $asm.exe + GadgetCore.dll + runtime manifests.`n`n" + $Notes
    }
    gh release create $tag $zip --repo $Repo --title "$asm ($slug) - $Stamp$(if ($SelfContained) { ' [self-contained]' })" --notes $notes
    if ($LASTEXITCODE -ne 0) { throw "release $tag FAILED" }

    Remove-Item -LiteralPath $zip -ErrorAction Ignore
    Write-Host "[ok] published $tag" -ForegroundColor Green
}

Write-Host "`nAll C# gadget releases published for stamp $Stamp." -ForegroundColor Green


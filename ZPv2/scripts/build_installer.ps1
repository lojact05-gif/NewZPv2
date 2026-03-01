param(
    [string]$InnoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $scriptDir "..")
$iss = Join-Path $root "installer/ZPv2.iss"

if (!(Test-Path $InnoPath)) {
    throw "ISCC not found at '$InnoPath'."
}

& $InnoPath $iss
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE"
}

$setup = Join-Path $root "out\installer\ZPv2Setup.exe"
if (!(Test-Path $setup)) {
    throw "Installer não encontrado em '$setup'."
}

Write-Host "Installer build completed: $setup"

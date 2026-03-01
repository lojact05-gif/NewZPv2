param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $scriptDir "..")
$src = Join-Path $root "src"
$out = Join-Path $root "out"

New-Item -ItemType Directory -Path $out -Force | Out-Null

$serviceProj = Join-Path $src "ZPv2.Service/ZPv2.Service.csproj"
$uiProj = Join-Path $src "ZPv2.Ui/ZPv2.Ui.csproj"

$serviceOut = Join-Path $out "_service"
$uiOut = Join-Path $out "_ui"
$publishOut = Join-Path $out "publish"
$stageOut = Join-Path $publishOut "_stage"

$sc = if ($SelfContained) { "true" } else { "false" }

if (Test-Path $serviceOut) { Remove-Item -Recurse -Force $serviceOut }
if (Test-Path $uiOut) { Remove-Item -Recurse -Force $uiOut }
if (Test-Path $publishOut) { Remove-Item -Recurse -Force $publishOut }

Write-Host "Publishing service..."
dotnet publish $serviceProj -c $Configuration -r $Runtime --self-contained $sc /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o $serviceOut

Write-Host "Publishing ui..."
dotnet publish $uiProj -c $Configuration -r $Runtime --self-contained $sc /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o $uiOut

New-Item -ItemType Directory -Path $publishOut -Force | Out-Null
New-Item -ItemType Directory -Path $stageOut -Force | Out-Null
Copy-Item (Join-Path $serviceOut "*") $stageOut -Recurse -Force
Copy-Item (Join-Path $uiOut "*") $stageOut -Recurse -Force

$required = @(
    (Join-Path $stageOut "ZPv2.Service.exe"),
    (Join-Path $stageOut "ZPv2.Ui.exe")
)
foreach ($req in $required) {
    if (!(Test-Path $req)) { throw "Build incompleto: $req" }
}

Write-Host "Build complete: $stageOut"

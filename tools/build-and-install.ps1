
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# Read local configuration
$propsFile = Join-Path $root "local.props"

if (-not (Test-Path $propsFile)) {
    throw "local.props not found."
}

[xml]$config = Get-Content -LiteralPath $propsFile -Raw

$props = $config.Project.PropertyGroup

$gameDir = [string]$props.Sts2Dir
$godotPath = [string]$props.GodotPath

if (-not $gameDir -or -not (Test-Path $gameDir)) {
    throw "Invalid Sts2Dir in local.props."
}

if (-not $godotPath -or -not (Test-Path $godotPath)) {
    throw "Invalid GodotPath in local.props."
}

# Build and package
Write-Host "[1/2] Building and packaging..."

& pwsh.exe -NoProfile -ExecutionPolicy Bypass `
    -File (Join-Path $root "tools\build.ps1") `
    -Pack `
    -SkipTests `
    -GodotPath $godotPath

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

# Install release files
Write-Host "[2/2] Installing..."

$source = Join-Path $root "artifacts\FrierenMod"
$target = Join-Path $gameDir "mods\FrierenMod"

foreach ($file in @("FrierenMod.dll", "FrierenMod.pck", "FrierenMod.json")) {
    if (-not (Test-Path (Join-Path $source $file))) {
        throw "Missing release file: $file"
    }
}

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item (Join-Path $source "*") $target -Recurse -Force

Write-Host "Installed to: $target" -ForegroundColor Green

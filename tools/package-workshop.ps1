param(
    [string]$WorkshopDirectory = '',
    [string]$ReferenceDirectory = '',
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path -Parent $PSScriptRoot
[xml]$packageConfig = Get-Content -LiteralPath (Join-Path $packageRoot 'local.props') -Raw
$packageGodot = [string]$packageConfig.Project.PropertyGroup.GodotPath
if (!$WorkshopDirectory) {
    $WorkshopDirectory = Join-Path (Split-Path -Parent $packageRoot) 'ModUploader-win-x64/Frieren-stable'
}
$WorkshopDirectory = [IO.Path]::GetFullPath($WorkshopDirectory)
if (!(Test-Path -LiteralPath (Join-Path $WorkshopDirectory 'workshop.json'))) {
    throw "未找到现有工坊配置，请用 -WorkshopDirectory 指定上传工作区：$WorkshopDirectory"
}
if (!$ReferenceDirectory -and (Test-Path -LiteralPath (Join-Path $packageRoot 'artifacts/build-references/reference-files.json'))) {
    $ReferenceDirectory = Join-Path $packageRoot 'artifacts/build-references'
}
$packageOptions = @{ Pack = $true; GodotPath = $packageGodot; SkipTests = $SkipTests }
if ($ReferenceDirectory) { $packageOptions.ReferenceDirectory = $ReferenceDirectory }
& (Join-Path $PSScriptRoot 'build.ps1') @packageOptions

$packageSource = Join-Path $packageRoot 'artifacts/FrierenMod'
$packageContent = Join-Path $WorkshopDirectory 'content'
# 只整理本地 content，不调用上传器，不写 workshop.json、封面或预览图。
New-Item -ItemType Directory -Force -Path $packageContent | Out-Null
foreach ($packageFile in @('FrierenMod.dll', 'FrierenMod.pck', 'FrierenMod.json', 'README.md', 'LICENSE.STS1', 'LICENSE.WATCHER')) {
    $packageFrom = Join-Path $packageSource $packageFile
    $packageTo = Join-Path $packageContent $packageFile
    Copy-Item -LiteralPath $packageFrom -Destination $packageTo -Force
    if ((Get-FileHash -LiteralPath $packageFrom).Hash -ne (Get-FileHash -LiteralPath $packageTo).Hash) {
        throw "本地打包校验失败：$packageFile"
    }
}
Write-Output "LOCAL_PACKAGE_OK: $packageContent"
Write-Output '仅完成本地打包，未上传创意工坊。'

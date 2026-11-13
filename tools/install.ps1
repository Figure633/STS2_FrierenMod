param()
$ErrorActionPreference = 'Stop'
$installRoot = Split-Path -Parent $PSScriptRoot
if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw '游戏正在运行，请先退出到桌面后安装。' }
[xml]$installConfig = Get-Content -LiteralPath (Join-Path $installRoot 'local.props') -Raw
[xml]$installProject = Get-Content -LiteralPath (Join-Path $installRoot 'FrierenMod.csproj') -Raw
$installSource = Join-Path $installRoot 'artifacts/FrierenMod'
$installTarget = Join-Path ([string]$installConfig.Project.PropertyGroup.Sts2Dir) 'mods/FrierenMod'
$installVersion = [string]$installProject.Project.PropertyGroup.Version
$installAssembly = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $installSource 'FrierenMod.dll')).Version.ToString(3)
$installManifest = (Get-Content -LiteralPath (Join-Path $installSource 'FrierenMod.json') -Raw | ConvertFrom-Json).version
if ($installVersion -ne $installAssembly -or $installVersion -ne $installManifest) { throw '工程、DLL 和清单版本不一致，请重新构建。' }
$installFiles = @('FrierenMod.dll', 'FrierenMod.pck', 'FrierenMod.json', 'README.md', 'LICENSE.STS1', 'LICENSE.WATCHER')
$installRecordPath = Join-Path $installRoot 'artifacts/build-target.json'
if (!(Test-Path -LiteralPath $installRecordPath)) { throw '缺少构建目标记录，请先运行 tools/build.ps1 -Pack 重新构建。' }
$installRecord = Get-Content -LiteralPath $installRecordPath -Raw | ConvertFrom-Json
$installGameDir = [string]$installConfig.Project.PropertyGroup.Sts2Dir
$installBaseDir = [string]$installConfig.Project.PropertyGroup.BaseLibDir
$installGameInfo = Get-Content -LiteralPath (Join-Path $installGameDir 'release_info.json') -Raw | ConvertFrom-Json
$installBaseInfo = Get-Content -LiteralPath (Join-Path $installBaseDir 'BaseLib.json') -Raw | ConvertFrom-Json
if ($installRecord.game_version -ne $installGameInfo.version -or
    $installRecord.game_assembly_sha256 -ne (Get-FileHash -LiteralPath (Join-Path $installGameDir 'data_sts2_windows_x86_64/sts2.dll')).Hash -or
    $installRecord.baselib_version -ne $installBaseInfo.version -or
    $installRecord.baselib_assembly_sha256 -ne (Get-FileHash -LiteralPath (Join-Path $installBaseDir 'BaseLib.dll')).Hash) {
    throw '构建包对应的游戏或 BaseLib 与当前安装不一致；切换分支或更新依赖后请重新构建。'
}
foreach ($installFile in $installFiles) {
    if (!(Test-Path -LiteralPath (Join-Path $installSource $installFile))) { throw "缺少构建文件：$installFile" }
    if ($installRecord.files.$installFile -ne (Get-FileHash -LiteralPath (Join-Path $installSource $installFile)).Hash) {
        throw "构建文件已变化，请重新构建：$installFile"
    }
}
# 当前包直接覆盖安装；历史源码由 Git 保存，不再逐版本复制完整安装备份。
New-Item -ItemType Directory -Force -Path $installTarget | Out-Null
foreach ($installFile in $installFiles) {
    $installFrom = Join-Path $installSource $installFile
    $installTo = Join-Path $installTarget $installFile
    Copy-Item -LiteralPath $installFrom -Destination $installTo -Force
    if ((Get-FileHash -LiteralPath $installFrom).Hash -ne (Get-FileHash -LiteralPath $installTo).Hash) { throw "安装校验失败：$installFile" }
}
Write-Output "INSTALL_OK version=$installVersion verified=$($installFiles.Count)"

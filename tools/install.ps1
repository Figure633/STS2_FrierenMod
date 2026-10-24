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
foreach ($installFile in $installFiles) {
    if (!(Test-Path -LiteralPath (Join-Path $installSource $installFile))) { throw "缺少构建文件：$installFile" }
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

param(
    [string]$GodotPath = "",
    [switch]$Pack,
    [switch]$EngineTests,
    [string]$GodotMonoPath = ""
)
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
function Invoke-Checked([string]$program, [string[]]$arguments) {
    & $program @arguments
    if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): $program" }
}
# 本次适配以实际安装版本为准，更新游戏后必须重新验证 API。
$gameDir = & dotnet msbuild "$projectRoot/FrierenMod.csproj" -nologo -getProperty:Sts2Dir
if ($LASTEXITCODE -ne 0) { throw "无法读取 Sts2Dir。" }
$baseLibDir = & dotnet msbuild "$projectRoot/FrierenMod.csproj" -nologo -getProperty:BaseLibDir
if ($LASTEXITCODE -ne 0) { throw "无法读取 BaseLibDir。" }
$releaseInfo = Get-Content -Raw -LiteralPath "$gameDir/release_info.json" | ConvertFrom-Json
$baseManifest = Get-Content -Raw -LiteralPath "$baseLibDir/BaseLib.json" | ConvertFrom-Json
if ($releaseInfo.version -ne "v0.107.1" -or $baseManifest.version -ne "v3.4.7") {
    throw "此实验版验证基线为 StS2 v0.107.1 + BaseLib v3.4.7，请先处理版本升级兼容性。"
}
Invoke-Checked "dotnet" @("build", "$projectRoot/FrierenMod.csproj", "-c", "Release", "--nologo")
Invoke-Checked "dotnet" @("run", "--project", "$projectRoot/Tests/Pure/PureTests.csproj", "-c", "Release")
Invoke-Checked "dotnet" @("run", "--project", "$projectRoot/Tests/Integration/IntegrationTests.csproj", "-c", "Release")
Invoke-Checked "python" @("$projectRoot/tools/validate_content.py")
if ($Pack) {
    if (!(Test-Path -LiteralPath $GodotPath)) { throw "请用 -GodotPath 提供 Godot 4.5.1 便携版可执行文件。" }
    $godotVersion = & $GodotPath --version
    if ($LASTEXITCODE -ne 0 -or $godotVersion -notmatch '^4\.5\.1\.') { throw "打包需要 Godot 4.5.1。" }
    Invoke-Checked $GodotPath @("--headless", "--editor", "--path", $projectRoot, "--import", "--log-file", "$projectRoot/artifacts/import.log")
    Invoke-Checked $GodotPath @("--headless", "--path", $projectRoot, "--script", "res://tools/export_pack.gd", "--log-file", "$projectRoot/artifacts/export.log")
    # 在空目录中挂载 PCK，防止本地导入缓存掩盖打包遗漏。
    $checkDir = "$projectRoot/artifacts/pack-check"
    New-Item -ItemType Directory -Force -Path $checkDir | Out-Null
    Copy-Item -LiteralPath "$projectRoot/tools/check_pack.gd" -Destination "$checkDir/check_pack.gd"
    Invoke-Checked $GodotPath @("--headless", "--path", $checkDir, "--script", "$checkDir/check_pack.gd", "--log-file", "$projectRoot/artifacts/pack-check.log", "--", "$projectRoot/artifacts/FrierenMod/FrierenMod.pck")
}
if ($EngineTests) {
    if (!(Test-Path -LiteralPath $GodotMonoPath)) { throw "请用 -GodotMonoPath 提供 Godot 4.5.1 .NET 版可执行文件。" }
    if (!(Test-Path -LiteralPath "$projectRoot/artifacts/FrierenMod/FrierenMod.pck")) { throw "引擎测试前需要 -Pack。" }
    Copy-Item -LiteralPath "$gameDir/release_info.json" -Destination "$projectRoot/Tests/Engine/release_info.json"
    $monoVersion = & $GodotMonoPath --version
    if ($LASTEXITCODE -ne 0 -or $monoVersion -notmatch '^4\.5\.1\.stable\.mono') { throw "引擎测试需要 Godot 4.5.1 .NET 版。" }
    $nupkgs = Join-Path (Split-Path -Parent (Resolve-Path -LiteralPath $GodotMonoPath).Path) "GodotSharp/Tools/nupkgs"
    if (!(Test-Path -LiteralPath $nupkgs)) { throw "未找到 Godot .NET 随附的 NuGet 包：$nupkgs" }
    # 只为当前进程及子进程设置路径，退出后恢复，不修改系统环境变量。
    $testEnvironment = @{
        FRIEREN_STS2_DIR = [string]$gameDir
        FRIEREN_BASELIB_DIR = [string]$baseLibDir
        FRIEREN_GODOT_NUPKGS = [string]$nupkgs
    }
    $previousEnvironment = @{}
    try {
        foreach ($name in $testEnvironment.Keys) {
            $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
            [Environment]::SetEnvironmentVariable($name, $testEnvironment[$name], "Process")
        }
        Invoke-Checked "dotnet" @("build", "$projectRoot/Tests/Engine/EngineTests.csproj", "--nologo")
        Invoke-Checked $GodotMonoPath @("--headless", "--path", "$projectRoot/Tests/Engine", "--log-file", "$projectRoot/artifacts/engine-tests.log")
    } finally {
        foreach ($name in $previousEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], "Process")
        }
    }
    $engineLog = Get-Content -LiteralPath "$projectRoot/artifacts/engine-tests.log" -Raw
    if ($engineLog -notmatch 'ENGINE_TESTS_OK:' -or $engineLog -match '\[ERROR\]|ERROR: System\.|Fatal error|System\.Exception:') {
        throw "Godot 引擎测试出现异步异常或缺少完成标记，请检查 engine-tests.log。"
    }
}

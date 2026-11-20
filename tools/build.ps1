param(
    [string]$GodotPath = "",
    [switch]$Pack,
    [switch]$EngineTests,
    [string]$GodotMonoPath = "",
    [switch]$SkipTests,
    [string]$ReferenceDirectory = ""
)
$ErrorActionPreference = "Stop"
if ($SkipTests -and $EngineTests) {
    throw "-SkipTests 和 -EngineTests 不能同时使用。"
}
if ($EngineTests -and -not $Pack) {
    throw "执行引擎测试必须同时指定 -Pack，以确保测试使用最新的 PCK。"
}
$projectRoot = Split-Path -Parent $PSScriptRoot
$buildTargetPath = Join-Path $projectRoot 'artifacts/build-target.json'
# 构建失败后不能继续使用上一次的安装凭据。
if (Test-Path -LiteralPath $buildTargetPath) { Remove-Item -LiteralPath $buildTargetPath }
function Invoke-Checked([string]$program, [string[]]$arguments) {
    & $program @arguments
    if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): $program" }
}
function Invoke-PackGodot([string[]]$arguments) {
    $previousAppData = [Environment]::GetEnvironmentVariable('APPDATA', 'Process')
    $previousLocalAppData = [Environment]::GetEnvironmentVariable('LOCALAPPDATA', 'Process')
    $packProfile = Join-Path $projectRoot 'artifacts/build-appdata'
    $packCache = Join-Path $projectRoot 'artifacts/build-localappdata'
    New-Item -ItemType Directory -Force -Path $packProfile | Out-Null
    New-Item -ItemType Directory -Force -Path $packCache | Out-Null
    try {
        [Environment]::SetEnvironmentVariable('APPDATA', $packProfile, 'Process')
        [Environment]::SetEnvironmentVariable('LOCALAPPDATA', $packCache, 'Process')
        Invoke-Checked $GodotPath $arguments
    } finally {
        [Environment]::SetEnvironmentVariable('APPDATA', $previousAppData, 'Process')
        [Environment]::SetEnvironmentVariable('LOCALAPPDATA', $previousLocalAppData, 'Process')
    }
}
# 本次适配以实际安装版本为准，更新游戏后必须重新验证 API。
$gameDir = & dotnet msbuild "$projectRoot/FrierenMod.csproj" -nologo -getProperty:Sts2Dir
if ($LASTEXITCODE -ne 0) { throw "无法读取 Sts2Dir。" }
$baseLibDir = & dotnet msbuild "$projectRoot/FrierenMod.csproj" -nologo -getProperty:BaseLibDir
if ($LASTEXITCODE -ne 0) { throw "无法读取 BaseLibDir。" }
$baseLibResourceDir = $baseLibDir
$dataDir = Join-Path $gameDir 'data_sts2_windows_x86_64'
$releaseInfoPath = Join-Path $gameDir 'release_info.json'
if ($ReferenceDirectory) {
    $ReferenceDirectory = (Resolve-Path -LiteralPath $ReferenceDirectory).Path
    $referenceRecord = Get-Content -Raw -LiteralPath (Join-Path $ReferenceDirectory 'reference-files.json') | ConvertFrom-Json
    foreach ($file in $referenceRecord.PSObject.Properties) {
        if ($file.Name -ne [IO.Path]::GetFileName($file.Name) -or
            $file.Value -ne (Get-FileHash -LiteralPath (Join-Path $ReferenceDirectory $file.Name)).Hash) {
            throw "缓存依赖已变化，请使用对应游戏分支重新构建：$($file.Name)"
        }
    }
    foreach ($required in @('sts2.dll', 'GodotSharp.dll', '0Harmony.dll', 'BaseLib.dll', 'BaseLib.json', 'release_info.json')) {
        if (!$referenceRecord.PSObject.Properties[$required]) { throw "缓存依赖清单缺少 $required" }
    }
    $dataDir = $ReferenceDirectory
    $baseLibDir = $ReferenceDirectory
    $releaseInfoPath = Join-Path $ReferenceDirectory 'release_info.json'
}
$referenceArgs = @("-p:Sts2DataDir=$dataDir", "-p:BaseLibDir=$baseLibDir")
$releaseInfo = Get-Content -Raw -LiteralPath $releaseInfoPath | ConvertFrom-Json
$baseManifest = Get-Content -Raw -LiteralPath "$baseLibDir/BaseLib.json" | ConvertFrom-Json
if ($releaseInfo.version -notin @("v0.113.0", "v0.113.1") -or $baseManifest.version -ne "v3.4.8") {
    throw "验证基线为 StS2 beta v0.113.0 / v0.113.1 + BaseLib v3.4.8；其他版本需重新核查兼容性。"
}
if ($baseManifest.version -eq 'v3.4.8' -and !(Test-Path -LiteralPath (Join-Path $baseLibDir 'BaseLibCompatibilityHelper.dll'))) {
    throw 'BaseLib 3.4.8 缺少随附的兼容助手，请更新完整的 BaseLib。'
}
$buildGameHash = (Get-FileHash -LiteralPath (Join-Path $dataDir 'sts2.dll')).Hash
$buildBaseHash = (Get-FileHash -LiteralPath (Join-Path $baseLibDir 'BaseLib.dll')).Hash

Invoke-Checked "dotnet" @(
    "build",
    "$projectRoot/FrierenMod.csproj",
    "-c",
    "Release",
    "--nologo"
    $referenceArgs
)

if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Cyan

    Invoke-Checked "dotnet" @(
        "run", "--project",
        "$projectRoot/Tests/Pure/PureTests.csproj",
        "-c", "Release"
    )

    Invoke-Checked "dotnet" @(
        "run", "--project",
        "$projectRoot/Tests/Integration/IntegrationTests.csproj",
        "-c", "Release"
        $referenceArgs
    )

    Invoke-Checked "dotnet" @(
        "run", "--project", "$projectRoot/Tests/Compatibility/CompatibilityTests.csproj", "--",
        "$projectRoot/artifacts/FrierenMod/FrierenMod.dll", [string]$dataDir, [string]$baseLibDir
    )

    Invoke-Checked "python" @(
        "$projectRoot/tools/validate_content.py"
    )
}
else {
    Write-Host "Skipping tests and content validation." -ForegroundColor Yellow
}
if ($Pack) {
    if (!(Test-Path -LiteralPath $GodotPath)) { throw "请用 -GodotPath 提供 Godot 4.5.1 便携版可执行文件。" }
    $godotVersion = & $GodotPath --version
    if ($LASTEXITCODE -ne 0 -or $godotVersion -notmatch '^4\.5\.1\.') { throw "打包需要 Godot 4.5.1。" }
    Invoke-PackGodot @("--headless", "--editor", "--path", $projectRoot, "--import", "--log-file", "$projectRoot/artifacts/import.log")
    Invoke-PackGodot @("--headless", "--path", $projectRoot, "--script", "res://tools/export_pack.gd", "--log-file", "$projectRoot/artifacts/export.log")
    # 在空目录中挂载 PCK，防止本地导入缓存掩盖打包遗漏。
    $checkDir = "$projectRoot/artifacts/pack-check"
    New-Item -ItemType Directory -Force -Path $checkDir | Out-Null
    Copy-Item -LiteralPath "$projectRoot/tools/check_pack.gd" -Destination "$checkDir/check_pack.gd"
    Invoke-PackGodot @("--headless", "--path", $checkDir, "--script", "$checkDir/check_pack.gd", "--log-file", "$projectRoot/artifacts/pack-check.log", "--", "$projectRoot/artifacts/FrierenMod/FrierenMod.pck", "$gameDir/SlayTheSpire2.pck")
    Invoke-Checked "python" @("$projectRoot/tools/verify_pack_payload.py")
}
if ($EngineTests) {
    if (!(Test-Path -LiteralPath $GodotMonoPath)) { throw "请用 -GodotMonoPath 提供 Godot 4.5.1 .NET 版可执行文件。" }
    if (!(Test-Path -LiteralPath "$projectRoot/artifacts/FrierenMod/FrierenMod.pck")) { throw "引擎测试前需要 -Pack。" }
    Copy-Item -LiteralPath $releaseInfoPath -Destination "$projectRoot/Tests/Engine/release_info.json"
    $monoVersion = & $GodotMonoPath --version
    if ($LASTEXITCODE -ne 0 -or $monoVersion -notmatch '^4\.5\.1\.stable\.mono') { throw "引擎测试需要 Godot 4.5.1 .NET 版。" }
    $nupkgs = Join-Path (Split-Path -Parent (Resolve-Path -LiteralPath $GodotMonoPath).Path) "GodotSharp/Tools/nupkgs"
    if (!(Test-Path -LiteralPath $nupkgs)) { throw "未找到 Godot .NET 随附的 NuGet 包：$nupkgs" }
    # 只为当前进程及子进程设置路径，退出后恢复，不修改系统环境变量。
    $testEnvironment = @{
        FRIEREN_STS2_DIR = [string]$gameDir
        FRIEREN_BASELIB_DIR = [string]$baseLibResourceDir
        FRIEREN_RELEASE_INFO = [string]$releaseInfoPath
        FRIEREN_GODOT_NUPKGS = [string]$nupkgs
    }
    $previousEnvironment = @{}
    try {
        foreach ($name in $testEnvironment.Keys) {
            $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
            [Environment]::SetEnvironmentVariable($name, $testEnvironment[$name], "Process")
        }
        $engineBuildArgs = @("build", "$projectRoot/Tests/Engine/EngineTests.csproj", "--nologo") + $referenceArgs
        if ($ReferenceDirectory) { $engineBuildArgs += "-p:TestRuntimeDataDir=$ReferenceDirectory" }
        Invoke-Checked "dotnet" $engineBuildArgs
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
if ($Pack) {
    # 全部构建与检查成功后才记录目标；安装时核对，避免切换分支后误装旧包。
    if ($buildGameHash -ne (Get-FileHash -LiteralPath (Join-Path $dataDir 'sts2.dll')).Hash -or
        $buildBaseHash -ne (Get-FileHash -LiteralPath (Join-Path $baseLibDir 'BaseLib.dll')).Hash) {
        throw '构建期间游戏或依赖发生更新，请重新构建。'
    }
    $packageFiles = @{}
    foreach ($packageFile in @('FrierenMod.dll', 'FrierenMod.pck', 'FrierenMod.json')) {
        $packageFiles[$packageFile] = (Get-FileHash -LiteralPath (Join-Path $projectRoot "artifacts/FrierenMod/$packageFile")).Hash
    }
    $targetRecord = [ordered]@{
        game_version = $releaseInfo.version
        game_assembly_sha256 = $buildGameHash
        baselib_version = $baseManifest.version
        baselib_assembly_sha256 = $buildBaseHash
        files = $packageFiles
    }
    $targetRecord | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $buildTargetPath -Encoding utf8
    # 每个工作树只保留一套可复用的编译依赖；以后打包无需切换 Steam 分支。
    $referenceCache = Join-Path $projectRoot 'artifacts/build-references'
    if ([IO.Path]::GetFullPath($dataDir) -ne [IO.Path]::GetFullPath($referenceCache)) {
        New-Item -ItemType Directory -Force -Path $referenceCache | Out-Null
        # 只缓存游戏的托管依赖，不复制其整套 .NET 运行时或原生 DLL。
        $cacheDlls = @(Get-ChildItem -LiteralPath $dataDir -Filter '*.dll' -File | Where-Object {
            $_.Name -match '^(sts2|GodotSharp|0Harmony|JetBrains\.Annotations|Steamworks\.NET|System\.IO\.Hashing)\.dll$' -or
            $_.Name -match '^(MonoMod\.|Sentry|SharpGen\.|SmartFormat|Vortice\.).*\.dll$'
        })
        $cacheDllNames = @($cacheDlls.Name) + @('BaseLib.dll', 'BaseLibCompatibilityHelper.dll')
        Get-ChildItem -LiteralPath $referenceCache -Filter '*.dll' -File | Where-Object Name -NotIn $cacheDllNames | ForEach-Object {
            Remove-Item -LiteralPath $_.FullName
        }
        $cacheDlls | Copy-Item -Destination $referenceCache -Force
        foreach ($dependency in @('BaseLib.dll', 'BaseLib.json', 'BaseLibCompatibilityHelper.dll')) {
            $sourceDependency = Join-Path $baseLibDir $dependency
            if (Test-Path -LiteralPath $sourceDependency) { Copy-Item -LiteralPath $sourceDependency -Destination $referenceCache -Force }
        }
        Copy-Item -LiteralPath $releaseInfoPath -Destination (Join-Path $referenceCache 'release_info.json') -Force
        $referenceFiles = [ordered]@{}
        Get-ChildItem -LiteralPath $referenceCache -File | Where-Object Name -ne 'reference-files.json' | Sort-Object Name | ForEach-Object {
            $referenceFiles[$_.Name] = (Get-FileHash -LiteralPath $_.FullName).Hash
        }
        $referenceFiles | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $referenceCache 'reference-files.json') -Encoding utf8
    }
    Write-Host "BUILD_TARGET_OK: $($releaseInfo.version) / BaseLib $($baseManifest.version)"
}

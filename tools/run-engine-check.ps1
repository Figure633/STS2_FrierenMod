param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Mode, [switch]$Snapshot,
    [string]$RuntimeDataDir = '', [string]$ReleaseInfoPath = '', [string]$GodotMonoPath = '')
$ErrorActionPreference = 'Stop'
$checkRoot = Split-Path -Parent $PSScriptRoot
[xml]$checkConfig = Get-Content -LiteralPath (Join-Path $checkRoot 'local.props') -Raw
$checkProps = $checkConfig.Project.PropertyGroup
$env:FRIEREN_STS2_DIR = [string]$checkProps.Sts2Dir
$env:FRIEREN_BASELIB_DIR = [string]$checkProps.BaseLibDir
$env:APPDATA = Join-Path $checkRoot 'artifacts/test-appdata'
$env:LOCALAPPDATA = Join-Path $checkRoot 'artifacts/test-localappdata'
$env:FRIEREN_RELEASE_INFO = if($ReleaseInfoPath) { (Resolve-Path -LiteralPath $ReleaseInfoPath).Path } else { Join-Path $env:FRIEREN_STS2_DIR 'release_info.json' }
if(!$RuntimeDataDir) { $RuntimeDataDir=Join-Path $env:FRIEREN_STS2_DIR 'data_sts2_windows_x86_64' }
if($RuntimeDataDir) {
    if(!(Test-Path -LiteralPath (Join-Path $RuntimeDataDir 'sts2.dll'))) { throw '运行时目录缺少 sts2.dll。' }
    $checkBin=Join-Path $checkRoot 'Tests/Engine/.godot/mono/temp/bin/Debug'
    Get-ChildItem -LiteralPath $RuntimeDataDir -Filter '*.dll' | Where-Object { $_.Name -notin @('FrierenMod.dll','EngineTests.dll') } | Copy-Item -Destination $checkBin -Force
}
$checkLog = Join-Path $checkRoot 'artifacts/engine-current.log'
$checkOut = Join-Path $checkRoot 'artifacts/engine-current-output.log'
$checkErrors = Join-Path $checkRoot 'artifacts/engine-current-errors.log'
$checkArgs = @('--path', (Join-Path $checkRoot 'Tests/Engine'), '--log-file', $checkLog)
if ($Snapshot) { $checkArgs += @('--position','-4000,-4000') } else { $checkArgs += '--headless' }
$checkArgs += @('--', "--$Mode")
if ($Snapshot) { $checkArgs += '--ui-snapshot' }
if(!$GodotMonoPath) { $GodotMonoPath=[string]$checkProps.GodotMonoPath }
$checkProcess = Start-Process -FilePath $GodotMonoPath -ArgumentList $checkArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput $checkOut -RedirectStandardError $checkErrors
if(!$checkProcess.WaitForExit(90000)) {
    $checkProcess.Kill($true)
    throw '引擎检查超过90秒，已停止本次测试进程。'
}
Get-Content -LiteralPath $checkOut -Tail 18
Get-Content -LiteralPath $checkErrors -Tail 12
$checkText = Get-Content -LiteralPath $checkLog -Raw
$checkProblems = $checkText + (Get-Content -LiteralPath $checkErrors -Raw)
if ($checkProcess.ExitCode -ne 0 -or $checkProblems -match '\[ERROR\]|ERROR:|\b\w+Exception:|Fatal error' -or $checkText -notmatch '_OK:') { throw '引擎检查失败，请查看 artifacts/engine-current.log。' }

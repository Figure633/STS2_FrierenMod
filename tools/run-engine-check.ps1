param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Mode, [switch]$Snapshot)
$ErrorActionPreference = 'Stop'
$checkRoot = Split-Path -Parent $PSScriptRoot
[xml]$checkConfig = Get-Content -LiteralPath (Join-Path $checkRoot 'local.props') -Raw
$checkProps = $checkConfig.Project.PropertyGroup
$env:FRIEREN_STS2_DIR = [string]$checkProps.Sts2Dir
$env:FRIEREN_BASELIB_DIR = [string]$checkProps.BaseLibDir
$env:APPDATA = Join-Path $checkRoot 'artifacts/test-appdata'
$env:LOCALAPPDATA = Join-Path $checkRoot 'artifacts/test-localappdata'
$checkLog = Join-Path $checkRoot 'artifacts/engine-current.log'
$checkOut = Join-Path $checkRoot 'artifacts/engine-current-output.log'
$checkErrors = Join-Path $checkRoot 'artifacts/engine-current-errors.log'
$checkArgs = @('--path', (Join-Path $checkRoot 'Tests/Engine'), '--log-file', $checkLog)
if ($Snapshot) { $checkArgs += @('--position','-4000,-4000') } else { $checkArgs += '--headless' }
$checkArgs += @('--', "--$Mode")
if ($Snapshot) { $checkArgs += '--ui-snapshot' }
$checkProcess = Start-Process -FilePath ([string]$checkProps.GodotMonoPath) -ArgumentList $checkArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput $checkOut -RedirectStandardError $checkErrors
$checkProcess.WaitForExit()
Get-Content -LiteralPath $checkOut -Tail 18
Get-Content -LiteralPath $checkErrors -Tail 12
$checkText = Get-Content -LiteralPath $checkLog -Raw
$checkProblems = $checkText + (Get-Content -LiteralPath $checkErrors -Raw)
if ($checkProcess.ExitCode -ne 0 -or $checkProblems -match '\[ERROR\]|ERROR:|\b\w+Exception:|Fatal error' -or $checkText -notmatch '_OK:') { throw '引擎检查失败，请查看 artifacts/engine-current.log。' }

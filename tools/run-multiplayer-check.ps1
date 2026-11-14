param([switch]$Mixed, [switch]$Dragon, [switch]$TurnGuard, [string]$GodotMonoPath = '', [string]$ReleaseInfoPath = '')
$ErrorActionPreference='Stop'
$mpRoot=Split-Path -Parent $PSScriptRoot
[xml]$mpConfig=Get-Content -LiteralPath (Join-Path $mpRoot 'local.props') -Raw
$mpProps=$mpConfig.Project.PropertyGroup
if(!$GodotMonoPath){$GodotMonoPath=[string]$mpProps.GodotMonoPath}
$mpDir=Join-Path $mpRoot ('artifacts/multiplayer/live-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $mpDir | Out-Null
$env:FRIEREN_STS2_DIR=[string]$mpProps.Sts2Dir
$env:FRIEREN_BASELIB_DIR=[string]$mpProps.BaseLibDir
$env:FRIEREN_RELEASE_INFO=if($ReleaseInfoPath){(Resolve-Path -LiteralPath $ReleaseInfoPath).Path}else{Join-Path $env:FRIEREN_STS2_DIR 'release_info.json'}
$env:FRIEREN_MP_TEST_DIR=$mpDir
# 由系统选择一个临时端口，避免碰到作者正在用的服务。
$mpSocket=New-Object System.Net.Sockets.UdpClient(0)
$env:FRIEREN_MP_TEST_PORT=[string]$mpSocket.Client.LocalEndPoint.Port
$mpSocket.Dispose()
$mpProcesses=@()
try {
 foreach($mpRole in @('host','client')) {
  $env:APPDATA=Join-Path $mpDir ($mpRole+'-appdata')
  $env:LOCALAPPDATA=Join-Path $mpDir ($mpRole+'-localappdata')
  $mpArgs=@('--headless','--path',(Join-Path $mpRoot 'Tests/Engine'),'--log-file',(Join-Path $mpDir ($mpRole+'.log')),'--',('--mp-'+$mpRole))
  if($Mixed){$mpArgs+='--mp-mixed'}
  if($Dragon){$mpArgs+='--mp-dragon'}
  if($TurnGuard){$mpArgs+='--mp-turn-guard'}
  $mpProcesses+=Start-Process -FilePath $GodotMonoPath -ArgumentList $mpArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $mpDir ($mpRole+'-stdout.log')) -RedirectStandardError (Join-Path $mpDir ($mpRole+'-stderr.log'))
 }
 $mpDeadline=[DateTime]::UtcNow.AddSeconds(100)
 while(@($mpProcesses | Where-Object {!$_.HasExited}).Count -gt 0) {
  if([DateTime]::UtcNow -gt $mpDeadline){throw "双端验证超时：$mpDir"}
  Start-Sleep -Milliseconds 200
 }
 foreach($mpRole in @('host','client')) {
  $mpLog=Get-Content -LiteralPath (Join-Path $mpDir ($mpRole+'.log')) -Raw
  $mpErrors=Get-Content -LiteralPath (Join-Path $mpDir ($mpRole+'-stderr.log')) -Raw
  if($mpLog -notmatch 'MULTIPLAYER_OK:' -or ($mpLog+$mpErrors) -match '\[ERROR\]|ERROR:|\w+Exception:|Fatal error'){throw "双端验证失败（$mpRole）：$mpDir"}
  Get-Content -LiteralPath (Join-Path $mpDir ($mpRole+'-result.json'))
 }
 if(@($mpProcesses | Where-Object {$_.ExitCode -ne 0}).Count -gt 0){throw '验证进程退出异常'}
 Write-Output "MULTIPLAYER_PAIR_OK mixed=$Mixed logs=$mpDir"
} finally {
 foreach($mpProcess in $mpProcesses){if(!$mpProcess.HasExited){$mpProcess.Kill($true)}}
}

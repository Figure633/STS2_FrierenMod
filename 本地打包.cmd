@echo off
chcp 65001 >nul
pwsh.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\package-workshop.ps1" %*
if errorlevel 1 (
  echo 打包失败，请查看上方错误。
  pause
  exit /b 1
)
echo 本地打包完成。未上传创意工坊。
pause

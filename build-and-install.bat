
@echo off
setlocal
cd /d "%~dp0"

pwsh.exe -NoProfile -ExecutionPolicy Bypass -File ".\tools\build-and-install.ps1"

set "RESULT=%ERRORLEVEL%"
echo.
if not "%RESULT%"=="0" (
    echo Build or installation failed.
) else (
    echo Build and installation completed.
)
pause
exit /b %RESULT%

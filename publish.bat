@echo off
chcp 65001 >nul
echo ==============================
echo   AudioGuard Build
echo ==============================
echo.

dotnet publish -c Release -r win-x64 --self-contained true -o .\dist

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed.
    echo Install .NET 8 SDK first:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo.
echo ==============================
echo   Build OK!
echo   Output: .\dist\AudioGuard.exe
echo ==============================
echo.
pause

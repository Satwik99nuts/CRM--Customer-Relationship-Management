@echo off
setlocal

cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK was not found. Install the .NET 10 SDK, then try again.
    pause
    exit /b 1
)

echo Starting AcxiomCRM. Keep this window open while using the app.
dotnet run --project "%~dp0AcxiomCRM.csproj"
set "appExitCode=%ERRORLEVEL%"

if not "%appExitCode%"=="0" (
    echo AcxiomCRM stopped with exit code %appExitCode%.
    pause
)

endlocal & exit /b %appExitCode%

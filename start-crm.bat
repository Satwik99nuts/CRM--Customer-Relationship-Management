@echo off
setlocal

set "APP_URL=http://localhost:5218"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK was not found. Install the .NET 10 SDK, then try again.
    pause
    exit /b 1
)

rem Reuse the app if it is already responding on the configured port.
powershell -NoProfile -Command "try { $r = Invoke-WebRequest -Uri '%APP_URL%' -TimeoutSec 2 -UseBasicParsing; if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { exit 0 } } catch {}; exit 1" >nul 2>nul
if errorlevel 1 (
    echo Starting AcxiomCRM...
    start "AcxiomCRM server" /min cmd /k "cd /d ""%~dp0"" && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --no-launch-profile"

    echo Waiting for AcxiomCRM to become ready...
    powershell -NoProfile -Command "$deadline = (Get-Date).AddSeconds(60); do { try { $r = Invoke-WebRequest -Uri '%APP_URL%' -TimeoutSec 2 -UseBasicParsing; if ($r.StatusCode -ge 200 -and $r.StatusCode -lt 500) { exit 0 } } catch {}; Start-Sleep -Seconds 1 } while ((Get-Date) -lt $deadline); exit 1"
    if errorlevel 1 (
        echo AcxiomCRM did not respond within 60 seconds. Check the minimized server window for an error.
        pause
        exit /b 1
    )
) else (
    echo AcxiomCRM is already running.
)

set "CHROME=%ProgramFiles%\Google\Chrome\Application\chrome.exe"
if not exist "%CHROME%" set "CHROME=%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"
if exist "%CHROME%" (
    start "" "%CHROME%" "%APP_URL%"
) else (
    start "" "%APP_URL%"
)

endlocal

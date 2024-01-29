@echo off
setlocal enabledelayedexpansion

:: Check if the script is running with administrative privileges
NET SESSION >NUL 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Please run this script as administrator!
    pause
    exit /b 1
)

set "targetDir=%APPDATA%\SpaceEngineers\Mods"

for /d %%i in ("%targetDir%\*") do (
    if exist "%%i\metadata.mod" (
        set "symlinkPath=%%i"

        rmdir /s /q "!symlinkPath!"
        echo Removed symlink in "%%i"
    )
)

pause

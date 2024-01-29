@echo off
setlocal enabledelayedexpansion

:: Check if the script is running with administrative privileges
NET SESSION >NUL 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Please run this script as administrator!
    pause
    exit /b 1
)

set "sourceDir=%~dp0"
set "targetDir=%APPDATA%\SpaceEngineers\Mods"

:: Get the name of the current folder (excluding the path)
for %%F in ("%sourceDir%.") do set "folderName=%%~nxF"

:: Create a symlink for the current folder
set "symlinkPath=!targetDir!\!folderName!"

mklink /d "!symlinkPath!" "!sourceDir!"
echo Created symlink for "!sourceDir!" in "!symlinkPath!"

pause

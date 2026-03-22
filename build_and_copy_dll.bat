@echo off
setlocal EnableExtensions EnableDelayedExpansion

if not defined SCHEDULE_I_DIR set "SCHEDULE_I_DIR=C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
set "MODS_DIR=%SCHEDULE_I_DIR%\Mods"
set "IL2CPP_DIR=%SCHEDULE_I_DIR%\MelonLoader\Il2CppAssemblies"
set "PROJECT_FILE=PaxDrops.IL2CPP.csproj"

if "%~1"=="" (
    call :all Debug
    exit /b %errorlevel%
)

if /I "%~1"=="-h" goto usage
if /I "%~1"=="--help" goto usage
if /I "%~1"=="-c" goto clean
if /I "%~1"=="--clean" goto clean
if /I "%~1"=="-a" (
    call :all Debug
    exit /b %errorlevel%
)
if /I "%~1"=="--all" (
    call :all Debug
    exit /b %errorlevel%
)
if /I "%~1"=="-b" goto buildarg
if /I "%~1"=="--build" goto buildarg
if /I "%~1"=="-d" goto copyarg
if /I "%~1"=="--dll" goto copyarg
if /I "%~1"=="-config" goto buildarg
if /I "%~1"=="--config" goto buildarg

echo [ERROR] Invalid option: %~1
goto usage_error

:buildarg
if "%~2"=="" (
    echo [ERROR] Please specify a build configuration.
    goto usage_error
)
call :build "%~2"
exit /b %errorlevel%

:copyarg
if "%~2"=="" (
    echo [ERROR] Please specify a build configuration.
    goto usage_error
)
call :copy "%~2"
exit /b %errorlevel%

:clean
echo [INFO] Cleaning build directory...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
echo [SUCCESS] Build directory cleaned.
exit /b 0

:all
set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Debug"
call :build "%CONFIG%"
if errorlevel 1 exit /b 1
call :copy "%CONFIG%"
exit /b %errorlevel%

:check_env
if not exist "%SCHEDULE_I_DIR%" (
    echo [ERROR] Schedule I install not found at !SCHEDULE_I_DIR!.
    echo [ERROR] Set SCHEDULE_I_DIR to your Schedule I install root and try again.
    exit /b 1
)
if not exist "%SCHEDULE_I_DIR%\MelonLoader\net6\MelonLoader.dll" (
    echo [ERROR] MelonLoader was not found under !SCHEDULE_I_DIR!.
    echo [ERROR] Install MelonLoader for Schedule I before building PaxDrops.
    exit /b 1
)
if not exist "%IL2CPP_DIR%" (
    echo [ERROR] Missing generated IL2CPP assemblies at !IL2CPP_DIR!.
    echo [ERROR] Launch Schedule I once with MelonLoader to generate Il2CppAssemblies, then rerun this script.
    exit /b 1
)
if not exist "%IL2CPP_DIR%\Assembly-CSharp.dll" (
    echo [ERROR] Missing !IL2CPP_DIR!\Assembly-CSharp.dll.
    echo [ERROR] Launch Schedule I once with MelonLoader to regenerate the IL2CPP assemblies, then rerun this script.
    exit /b 1
)
if not exist "%IL2CPP_DIR%\Il2CppSystem.dll" (
    echo [ERROR] Missing !IL2CPP_DIR!\Il2CppSystem.dll.
    echo [ERROR] Launch Schedule I once with MelonLoader to regenerate the IL2CPP assemblies, then rerun this script.
    exit /b 1
)
if not exist "%IL2CPP_DIR%\UnityEngine.dll" (
    echo [ERROR] Missing !IL2CPP_DIR!\UnityEngine.dll.
    echo [ERROR] Launch Schedule I once with MelonLoader to regenerate the IL2CPP assemblies, then rerun this script.
exit /b 0

:build
set "CONFIG=%~1"
call :check_env
if errorlevel 1 exit /b 1
call :clean
echo [INFO] Building %PROJECT_FILE% (%CONFIG%)...
dotnet clean "%PROJECT_FILE%" -c "%CONFIG%"
if errorlevel 1 (
    echo [ERROR] dotnet clean failed.
    exit /b 1
)
dotnet build "%PROJECT_FILE%" -c "%CONFIG%"
if errorlevel 1 (
    echo [ERROR] Build failed for configuration %CONFIG%.
    exit /b 1
)
echo [SUCCESS] Build completed. MSBuild deployed PaxDrops runtime files to %MODS_DIR%.
exit /b 0

:copy_file
set "SRC=%~1"
set "DST=%~2"
if not exist "%SRC%" exit /b 0
copy /Y "%SRC%" "%DST%" >nul
if errorlevel 1 exit /b 1
exit /b 0

:copy
set "CONFIG=%~1"
set "OUTPUT_DIR=bin\%CONFIG%\net6.0"
set "OUTPUT_DLL=%OUTPUT_DIR%\PaxDrops.dll"
if not exist "%SCHEDULE_I_DIR%" (
    echo [ERROR] Schedule I install not found at !SCHEDULE_I_DIR!.
    echo [ERROR] Set SCHEDULE_I_DIR to your Schedule I install root and try again.
    exit /b 1
)
if not exist "%OUTPUT_DLL%" (
    echo [ERROR] !OUTPUT_DLL! was not found. Build the project first.
    exit /b 1
)
if not exist "%MODS_DIR%" mkdir "%MODS_DIR%"
call :copy_file "%OUTPUT_DIR%\PaxDrops.dll" "%MODS_DIR%\PaxDrops.dll"
if errorlevel 1 (
    echo [ERROR] Failed to copy PaxDrops.dll to !MODS_DIR!.
    exit /b 1
)
call :copy_file "%OUTPUT_DIR%\PaxDrops.deps.json" "%MODS_DIR%\PaxDrops.deps.json"
if errorlevel 1 (
    echo [ERROR] Failed to copy PaxDrops.deps.json to !MODS_DIR!.
    exit /b 1
)
call :copy_file "%OUTPUT_DIR%\Microsoft.Data.Sqlite.dll" "%MODS_DIR%\Microsoft.Data.Sqlite.dll"
if errorlevel 1 (
    echo [ERROR] Failed to copy Microsoft.Data.Sqlite.dll to !MODS_DIR!.
    exit /b 1
)
call :copy_file "%OUTPUT_DIR%\SQLitePCLRaw.batteries_v2.dll" "%MODS_DIR%\SQLitePCLRaw.batteries_v2.dll"
if errorlevel 1 (
    echo [ERROR] Failed to copy SQLitePCLRaw.batteries_v2.dll to !MODS_DIR!.
    exit /b 1
)
call :copy_file "%OUTPUT_DIR%\SQLitePCLRaw.core.dll" "%MODS_DIR%\SQLitePCLRaw.core.dll"
if errorlevel 1 (
    echo [ERROR] Failed to copy SQLitePCLRaw.core.dll to !MODS_DIR!.
    exit /b 1
)
call :copy_file "%OUTPUT_DIR%\SQLitePCLRaw.provider.e_sqlite3.dll" "%MODS_DIR%\SQLitePCLRaw.provider.e_sqlite3.dll"
if errorlevel 1 (
    echo [ERROR] Failed to copy SQLitePCLRaw.provider.e_sqlite3.dll to !MODS_DIR!.
    exit /b 1
)
if exist "%OUTPUT_DIR%\runtimes" (
    robocopy "%OUTPUT_DIR%\runtimes" "%MODS_DIR%\runtimes" /E /XO /XN /XC /NFL /NDL /NJH /NJS >nul
    if errorlevel 8 (
        echo [ERROR] Failed to copy SQLite runtime assets to !MODS_DIR!\runtimes.
        exit /b 1
    )
)
echo [SUCCESS] Copied PaxDrops.dll and SQLite runtime files to %MODS_DIR%.
exit /b 0

:usage
echo Usage: %~nx0 [options]
echo.
echo Options:
echo   -h, --help           Show this help message
echo   -c, --clean          Remove local bin and obj folders
echo   -b, --build CONFIG   Clean and build, then let MSBuild deploy to Mods
echo   -d, --dll CONFIG     Copy PaxDrops.dll and SQLite runtime files to Mods
echo   -a, --all            Clean + build + copy using Debug
echo   -config CONFIG       Alias for --build CONFIG
echo.
echo Environment:
echo   SCHEDULE_I_DIR       Overrides the Schedule I install root.
echo                        Current Mods target: %MODS_DIR%
echo                        Builds require MelonLoader-generated Il2CppAssemblies.
exit /b 0

:usage_error
exit /b 1

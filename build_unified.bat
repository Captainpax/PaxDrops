@echo off
REM Build script for both IL2CPP and Mono versions of PaxDrops

echo 🔨 Building PaxDrops for IL2CPP and Mono runtimes...

REM Clean previous builds
echo 🧹 Cleaning previous builds...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

REM Build IL2CPP version (default)
echo 🔧 Building IL2CPP version...
dotnet build --configuration Release
if %ERRORLEVEL% equ 0 (
    echo ✅ IL2CPP build completed successfully
    REM Copy to IL2CPP output
    if not exist dist\IL2CPP mkdir dist\IL2CPP
    copy bin\Release\net6.0\PaxDrops.dll dist\IL2CPP\
) else (
    echo ❌ IL2CPP build failed
    exit /b 1
)

REM Build Mono version
echo 🔧 Building Mono version...
dotnet build --configuration Release -p:RuntimeTarget=Mono
if %ERRORLEVEL% equ 0 (
    echo ✅ Mono build completed successfully
    REM Copy to Mono output
    if not exist dist\Mono mkdir dist\Mono
    copy bin\Release\net6.0\PaxDrops.Mono.dll dist\Mono\
) else (
    echo ❌ Mono build failed
    exit /b 1
)

echo.
echo 🎉 Build complete!
echo 📁 IL2CPP version: dist\IL2CPP\PaxDrops.dll
echo 📁 Mono version: dist\Mono\PaxDrops.Mono.dll
echo.
echo 🚀 To use:
echo    IL2CPP games: Copy PaxDrops.dll to your game's Mods folder
echo    Mono games: Copy PaxDrops.Mono.dll to your game's Mods folder

pause

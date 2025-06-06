@echo off

:: Build the project based on the configuration

:: Colors for output logging
:: Note: Windows does not support ANSI escape codes for color output directly.
:: This script will not have colored output as it is not natively supported in Windows.

:: Define paths
set MODS_DIR="C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods"

:: Function to log messages
echo_info() {
    echo [INFO] %1
}

echo_success() {
    echo [SUCCESS] %1
}

echo_warning() {
    echo [WARNING] %1
}

echo_error() {
    echo [ERROR] %1
}

:: Function to clean the build directory
clean_build() {
    echo_info "Cleaning build directory..."
    if exist "bin" (
        rmdir /s /q bin
    )
    if exist "obj" (
        rmdir /s /q obj
    )
    echo_success "Build directory cleaned"
}

:: help function
help() {
    echo Usage: %0 [options]
    echo Options:
    echo   -h, --help    Show this help message
    echo   -c, --clean   Clean the build directory
    echo   -b, --build   Build the project with a specified configuration
    echo   -d, --dll    Copy the DLL to the mods directory
    echo   -a, --all    Perform a full build and copy process
    echo   -config     Specify the build configuration (Debug, Staging, Release)
}

:: Function to build the project
build_project() {
    set config=%1
    set output_dir="bin\%config\net6.0"
    
    :: Clean the build directory
    clean_build

    :: Build the project
    dotnet clean && dotnet build --configuration %config
    if %errorlevel% neq 0 (
        echo_error "Failed to build project in %config configuration"
        exit /b 1
    )
    echo_success "Project built in %config configuration"
    
    :: Copy the DLL to the mods directory    
    copy_dll %config
}

:: Function to copy the DLL to the mods directory
copy_dll() {
    set config=%1
    set output_dir="bin\%config\net6.0"
    
    :: Copy the DLL to the mods directory
    copy "%output_dir%\PaxDrops.dll" "%MODS_DIR%"
    echo_success "DLL copied to %MODS_DIR%"
}

:: Parse arguments
set "options=-hcdba"
set "long_options=--help --clean --build --dll --all --config"
set "arguments=%*"
set "arguments=%arguments:~0,-1%" & rem remove the last character which is the script name

for %%O in (%options%) do (
    if "%%O" == "-h" (
        help
        exit /b 0
    )
    if "%%O" == "-c" (
        clean_build
        exit /b 0
    )
    if "%%O" == "-b" (
        shift
        if "%1" == "" (
            echo Please specify a build configuration
            exit /b 1
        )
        build_project %1
        exit /b 0
    )
    if "%%O" == "-d" (
        shift
        if "%1" == "" (
            echo Please specify a build configuration
            exit /b 1
        )
        copy_dll %1
        exit /b 0
    )
    if "%%O" == "-a" (
        build_project Debug
        exit /b 0
    )
    if "%%O" == "?" (
        echo Invalid option: -%%OPTARG
        help
        exit /b 1
    )
)

for %%O in (%long_options%) do (
    if "%%O" == "--help" (
        help
        exit /b 0
    )
    if "%%O" == "--clean" (
        clean_build
        exit /b 0
    )
    if "%%O" == "--build" (
        shift
        if "%1" == "" (
            echo Please specify a build configuration
            exit /b 1
        )
        build_project %1
        exit /b 0
    )
    if "%%O" == "--dll" (
        shift
        if "%1" == "" (
            echo Please specify a build configuration
            exit /b 1
        )
        copy_dll %1
        exit /b 0
    )
    if "%%O" == "--all" (
        build_project Debug
        exit /b 0
    )
    if "%%O" == "--config" (
        shift
        if "%1" == "" (
            echo Please specify a build configuration
            exit /b 1
        )
        build_project %1
        exit /b 0
    )
    if "%%O" == "" (
        echo Invalid option: %1
        help
        exit /b 1
    )
)

:: Example usage
:: build_and_copy_dll.bat -c
:: build_and_copy_dll.bat -b Debug
:: build_and_copy_dll.bat -d Debug
:: build_and_copy_dll.bat -a Debug
:: build_and_copy_dll.bat --config Staging
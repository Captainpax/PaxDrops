#!/bin/bash

# Build the project based on the configuration

# Colors for output logging
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

# Define paths
MODS_DIR="/Users/shreyas/Library/Application Support/CrossOver/Bottles/Schedule I/drive_c/Program Files (x86)/Steam/steamapps/common/Schedule I/Mods"

# Function to log messages with colors
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to clean the build directory
clean_build() {
    print_info "Cleaning build directory..."
    if [ -d "bin" ]; then
        rm -rf bin
    fi
    if [ -d "obj" ]; then
        rm -rf obj
    fi
    print_success " ✅ Build directory cleaned"
}

# help function
help() {
    echo "Usage: $0 [options]"
    echo "Options:"
    echo "  -h, --help    Show this help message"
    echo "  -c, --clean   Clean the build directory"
    echo "  -b, --build   Build the project with a specified configuration"
    echo "  -d, --dll    Copy the DLL to the mods directory"
    echo "  -a, --all    Perform a full build and copy process"
    echo "  -config     Specify the build configuration (Debug, Staging, Release)"
}

# Function to build the project
build_project() {
    local config=$1
    local output_dir="bin/$config/net6.0"
    
    # Clean the build directory
    clean_build

    # Build the project
    dotnet clean && dotnet build --configuration $config
    if [ $? -ne 0 ]; then
        print_error " ❌ Failed to build project in $config configuration"
        exit 1
    fi
    print_success " ✅ Project built in $config configuration"
    
    # Copy the DLL to the mods directory    
    copy_dll $config
}

# Function to copy the DLL to the mods directory
copy_dll() {
    local config=$1
    local output_dir="bin/$config/net6.0"
    
    # Copy the DLL to the mods directory
    cp "$output_dir/PaxDrops.dll" "$MODS_DIR/"
    print_success " ✅ DLL copied to $MODS_DIR"
}

# Parse arguments
while getopts ":hcdba" opt; do
  case $opt in
    h)
      help
      exit 0
      ;;
    c)
      clean_build
      exit 0
      ;;
    b)
      shift
      if [ -z "$1" ]; then
        echo "Please specify a build configuration"
        exit 1
      fi
      build_project "$1"
      exit 0
      ;;
    d)
      shift
      if [ -z "$1" ]; then
        echo "Please specify a build configuration"
        exit 1
      fi
      copy_dll "$1"
      exit 0
      ;;
    a)
      build_project "Debug"
      exit 0
      ;;
    \?)
      echo "Invalid option: -$OPTARG" >&2
      help
      exit 1
      ;;
  esac
done

if [ $# -gt 0 ]; then
  case $1 in
    --help)
      help
      exit 0
      ;;
    --clean)
      clean_build
      exit 0
      ;;
    --build)
      shift
      if [ -z "$1" ]; then
        echo "Please specify a build configuration"
        exit 1
      fi
      build_project "$1"
      exit 0
      ;;
    --dll)
      shift
      if [ -z "$1" ]; then
        echo "Please specify a build configuration"
        exit 1
      fi
      copy_dll "$1"
      exit 0
      ;;
    --all)
      build_project "Debug"
      exit 0
      ;;
    --config)
      shift
      if [ -z "$1" ]; then
        echo "Please specify a build configuration"
        exit 1
      fi
      build_project "$1"
      exit 0
      ;;
    *)
      echo "Invalid option: $1" >&2
      help
      exit 1
      ;;
  esac
fi

# Example usage
# ./build_and_copy_dll.sh -c
# ./build_and_copy_dll.sh -b Debug
# ./build_and_copy_dll.sh -d Debug
# ./build_and_copy_dll.sh -a Debug
# ./build_and_copy_dll.sh --config Staging
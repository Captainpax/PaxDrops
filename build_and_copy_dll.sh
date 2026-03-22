#!/bin/bash

set -euo pipefail

SCHEDULE_I_DIR="${SCHEDULE_I_DIR:-/Users/shreyas/Library/Application Support/CrossOver/Bottles/Schedule I/drive_c/Program Files (x86)/Steam/steamapps/common/Schedule I}"
MODS_DIR="${SCHEDULE_I_DIR}/Mods"
IL2CPP_DIR="${SCHEDULE_I_DIR}/MelonLoader/Il2CppAssemblies"
PROJECT_FILE="PaxDrops.IL2CPP.csproj"

print_info() {
    echo "[INFO] $1"
}

print_success() {
    echo "[SUCCESS] $1"
}

print_error() {
    echo "[ERROR] $1" >&2
}

require_schedule_i_build_env() {
    if [[ ! -d "${SCHEDULE_I_DIR}" ]]; then
        print_error "Schedule I install not found at ${SCHEDULE_I_DIR}."
        print_error "Set SCHEDULE_I_DIR to your Schedule I install root and try again."
        exit 1
    fi

    if [[ ! -f "${SCHEDULE_I_DIR}/MelonLoader/net6/MelonLoader.dll" ]]; then
        print_error "MelonLoader was not found under ${SCHEDULE_I_DIR}."
        print_error "Install MelonLoader for Schedule I before building PaxDrops."
        exit 1
    fi

    if [[ ! -d "${IL2CPP_DIR}" ]]; then
        print_error "Missing generated IL2CPP assemblies at ${IL2CPP_DIR}."
        print_error "Launch Schedule I once with MelonLoader to generate Il2CppAssemblies, then rerun this script."
        exit 1
    fi

    local required_assemblies=(
        "${IL2CPP_DIR}/Assembly-CSharp.dll"
        "${IL2CPP_DIR}/Il2CppSystem.dll"
        "${IL2CPP_DIR}/UnityEngine.dll"
    )

    local required_assembly
    for required_assembly in "${required_assemblies[@]}"; do
        if [[ ! -f "${required_assembly}" ]]; then
            print_error "Missing ${required_assembly}."
            print_error "Launch Schedule I once with MelonLoader to regenerate the IL2CPP assemblies, then rerun this script."
            exit 1
        fi
    done
}

clean_build() {
    print_info "Cleaning build directory..."
    rm -rf bin obj
    print_success "Build directory cleaned."
}

build_project() {
    local config="$1"

    require_schedule_i_build_env
    clean_build
    print_info "Building ${PROJECT_FILE} (${config})..."
    dotnet clean "${PROJECT_FILE}" -c "${config}"
    dotnet build "${PROJECT_FILE}" -c "${config}"
    print_success "Build completed. MSBuild deployed PaxDrops.dll to ${MODS_DIR}."
}

copy_dll() {
    local config="$1"
    local output_dll="bin/${config}/net6.0/PaxDrops.dll"

    if [[ ! -d "${SCHEDULE_I_DIR}" ]]; then
        print_error "Schedule I install not found at ${SCHEDULE_I_DIR}."
        print_error "Set SCHEDULE_I_DIR to your Schedule I install root and try again."
        exit 1
    fi

    if [[ ! -f "${output_dll}" ]]; then
        print_error "${output_dll} was not found. Build the project first."
        exit 1
    fi

    mkdir -p "${MODS_DIR}"
    cp "${output_dll}" "${MODS_DIR}/PaxDrops.dll"
    print_success "Copied PaxDrops.dll to ${MODS_DIR}."
}

usage() {
    cat <<EOF
Usage: $0 [options]

Options:
  -h, --help           Show this help message
  -c, --clean          Remove local bin and obj folders
  -b, --build CONFIG   Clean and build, then let MSBuild deploy to Mods
  -d, --dll CONFIG     Copy bin/CONFIG/net6.0/PaxDrops.dll to Mods
  -a, --all            Same as --build Debug
  -config CONFIG       Alias for --build CONFIG

Environment:
  SCHEDULE_I_DIR       Overrides the Schedule I install root.
                       Current Mods target: ${MODS_DIR}
                       Builds require MelonLoader-generated Il2CppAssemblies.
EOF
}

if [[ $# -eq 0 ]]; then
    usage
    exit 0
fi

case "$1" in
    -h|--help)
        usage
        ;;
    -c|--clean)
        clean_build
        ;;
    -a|--all)
        build_project "Debug"
        ;;
    -b|--build|-config|--config)
        if [[ $# -lt 2 ]]; then
            print_error "Please specify a build configuration."
            exit 1
        fi
        build_project "$2"
        ;;
    -d|--dll)
        if [[ $# -lt 2 ]]; then
            print_error "Please specify a build configuration."
            exit 1
        fi
        copy_dll "$2"
        ;;
    *)
        print_error "Invalid option: $1"
        usage
        exit 1
        ;;
esac

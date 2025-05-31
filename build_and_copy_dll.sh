#!/bin/bash

# Build the project
dotnet clean && dotnet build
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

# Define paths
OUTPUT_DIR="bin/Debug/net6.0"
MODS_DIR="/Users/shreyas/Library/Application Support/CrossOver/Bottles/Schedule I/drive_c/Program Files (x86)/Steam/steamapps/common/Schedule I/Mods"

# Copy main DLL
cp "$OUTPUT_DIR/PaxDrops.dll" "$MODS_DIR/"

echo "✅ Copied PaxDrops.dll to mods folder"
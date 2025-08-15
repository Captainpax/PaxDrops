#!/bin/bash
# Build script for both IL2CPP and Mono versions of PaxDrops

echo "🔨 Building PaxDrops for IL2CPP and Mono runtimes..."

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf bin/
rm -rf obj/

# Build IL2CPP version (default)
echo "🔧 Building IL2CPP version..."
dotnet build --configuration Release
if [ $? -eq 0 ]; then
    echo "✅ IL2CPP build completed successfully"
    # Copy to IL2CPP output
    mkdir -p dist/IL2CPP
    cp bin/Release/net6.0/PaxDrops.dll dist/IL2CPP/
else
    echo "❌ IL2CPP build failed"
    exit 1
fi

# Build Mono version
echo "🔧 Building Mono version..."
dotnet build --configuration Release -p:RuntimeTarget=Mono
if [ $? -eq 0 ]; then
    echo "✅ Mono build completed successfully"
    # Copy to Mono output
    mkdir -p dist/Mono
    cp bin/Release/net6.0/PaxDrops.Mono.dll dist/Mono/
else
    echo "❌ Mono build failed"
    exit 1
fi

echo ""
echo "🎉 Build complete!"
echo "📁 IL2CPP version: dist/IL2CPP/PaxDrops.dll"
echo "📁 Mono version: dist/Mono/PaxDrops.Mono.dll"
echo ""
echo "🚀 To use:"
echo "   IL2CPP games: Copy PaxDrops.dll to your game's Mods folder"
echo "   Mono games: Copy PaxDrops.Mono.dll to your game's Mods folder"

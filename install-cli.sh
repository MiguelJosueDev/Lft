#!/bin/bash
set -e

echo "[LFT] Packing CLI..."
dotnet pack src/Lft.Cli/Lft.Cli.csproj -c Release

echo "[LFT] Uninstalling existing tool..."
dotnet tool uninstall -g lft || true

echo "[LFT] Installing new version..."
dotnet tool install -g lft --add-source src/Lft.Cli/bin/Release/ --version 1.0.0

echo "[SUCCESS] LFT CLI installed successfully!"

#!/usr/bin/env bash
# Exports the release build to build/TheLedger.exe (same path every time, so a taskbar pin always opens the newest).
# Needs the Godot 4.7.2 .NET Windows export templates in %APPDATA%/Godot/export_templates/4.7.2.stable.mono.
# Usage (Git Bash, from the repo root): tools/build_local.sh
set -euo pipefail
cd "$(dirname "$0")/.."
GODOT="/c/Users/matth/Desktop/Apps/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"

if tasklist //FI "IMAGENAME eq TheLedger.exe" 2>/dev/null | grep -qi "TheLedger.exe"; then
    echo "The Ledger is running: close it first so the .exe can be replaced." >&2
    exit 1
fi

mkdir -p build
dotnet build game/TheLedger.csproj -c ExportRelease -nologo -v q -warnaserror
"$GODOT" --headless --path game --export-release "Windows Desktop" ../build/TheLedger.exe
version=$(sed -n 's|^config/version="\(.*\)"|\1|p' game/project.godot)
echo "Built The Ledger v$version -> $(cd build && pwd -W)/TheLedger.exe"

#!/bin/bash
# SpellBinder Community Server — Setup Script (Linux/WSL)
# Run from the community_server/ directory
# Prerequisites: mono-devel, mysql-server, nuget (or wget)

set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"
SKIP_MYSQL=false
HEADLESS=false
MYSQL_USER="root"
MYSQL_PASSWORD=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-mysql) SKIP_MYSQL=true; shift ;;
        --headless) HEADLESS=true; shift ;;
        --mysql-user) MYSQL_USER="$2"; shift 2 ;;
        --mysql-password) MYSQL_PASSWORD="$2"; shift 2 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

echo "=== SpellBinder Server Setup ==="

# 1. Check prerequisites
echo -e "\n[1/7] Checking prerequisites..."

if command -v msbuild &>/dev/null; then
    BUILD_CMD="msbuild"
elif command -v xbuild &>/dev/null; then
    BUILD_CMD="xbuild"
else
    echo "ERROR: Neither msbuild nor xbuild found."
    echo "Install mono-devel: sudo apt install mono-devel"
    echo "Or on Windows: use setup.ps1 instead"
    exit 1
fi
echo "  Build tool: $BUILD_CMD"

if ! command -v mono &>/dev/null; then
    echo "ERROR: mono not found. Install: sudo apt install mono-runtime"
    exit 1
fi
echo "  Mono: $(mono --version | head -1)"

# 2. Download NuGet if needed
echo -e "\n[2/7] Checking NuGet..."
NUGET="$ROOT/nuget.exe"
if [ ! -f "$NUGET" ]; then
    echo "  Downloading nuget.exe..."
    wget -q -O "$NUGET" "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
fi
echo "  NuGet: OK"

# 3. Restore NuGet packages
echo -e "\n[3/7] Restoring NuGet packages..."
mono "$NUGET" restore "$ROOT/Spellbinder.sln" -Verbosity quiet
echo "  Packages restored"

# 4. Build
echo -e "\n[4/7] Building SpellServer..."
$BUILD_CMD "$ROOT/SpellServer/SpellServer.csproj" \
    /p:Configuration=Debug /p:Platform=x86 \
    /verbosity:minimal /nologo
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed"
    exit 1
fi
echo "  Build: OK"

# 5. Copy content files
echo -e "\n[5/7] Copying content files..."
if [ -f "$ROOT/copy_content.py" ]; then
    python3 "$ROOT/copy_content.py"
elif [ -d "$ROOT/Content/Grids" ]; then
    for grid_dir in "$ROOT/Content/Grids"/*/; do
        grid_name=$(basename "$grid_dir")
        dst="$ROOT/Build/Debug/Grids/$grid_name"
        mkdir -p "$dst"
        cp -r "$grid_dir"* "$dst/"
        # Fix case: GEOMETRY.DAT -> Geometry.dat
        if [ -f "$dst/GEOMETRY.DAT" ] && [ ! -f "$dst/Geometry.dat" ]; then
            mv "$dst/GEOMETRY.DAT" "$dst/Geometry.dat"
        fi
    done
    echo "  Content copied"
else
    echo "  WARNING: Content/Grids not found - grids won't load"
fi

# 6. MySQL setup
if [ "$SKIP_MYSQL" = false ]; then
    echo -e "\n[6/7] Setting up MySQL database..."
    if command -v mysql &>/dev/null; then
        SQL_FILE="$ROOT/Content/spellbinder-server.sql"
        if [ -f "$SQL_FILE" ]; then
            PASS_ARG=""
            [ -n "$MYSQL_PASSWORD" ] && PASS_ARG="-p$MYSQL_PASSWORD"
            mysql -u "$MYSQL_USER" $PASS_ARG -e "CREATE DATABASE IF NOT EXISTS spellbinder;"
            mysql -u "$MYSQL_USER" $PASS_ARG spellbinder < "$SQL_FILE"
            mysql -u "$MYSQL_USER" $PASS_ARG -e "CREATE USER IF NOT EXISTS 'localweb'@'localhost' IDENTIFIED WITH mysql_native_password BY ''; GRANT ALL PRIVILEGES ON spellbinder.* TO 'localweb'@'localhost'; FLUSH PRIVILEGES;"
            echo "  MySQL: OK"
        else
            echo "  WARNING: spellbinder-server.sql not found"
        fi
    else
        echo "  WARNING: mysql not found - skipping DB setup"
    fi
else
    echo -e "\n[6/7] Skipping MySQL setup (--skip-mysql)"
fi

# 7. Update config
echo -e "\n[7/7] Updating configuration..."
cp "$ROOT/SpellServer/app.config" "$ROOT/Build/Debug/SpellServer.exe.config" 2>/dev/null || true
echo "  Config copied"

# Done
echo -e "\n=== Setup Complete ==="
EXE="$ROOT/Build/Debug/SpellServer.exe"

if [ "$HEADLESS" = true ]; then
    echo "Starting server in headless mode..."
    mono "$EXE" --headless
else
    echo ""
    echo "To start the server:"
    echo "  GUI mode:      mono $EXE"
    echo "  Headless mode: mono $EXE --headless"
    echo ""
    echo "To run tests:"
    echo "  mono packages/NUnit.ConsoleRunner.3.16.3/tools/nunit3-console.exe SpellServer.Tests/bin/Debug/SpellServer.Tests.dll"
fi

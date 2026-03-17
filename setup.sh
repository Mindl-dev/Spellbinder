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

# Check for sudo early — needed for package installs and MySQL setup
check_sudo() {
    if [ "$(id -u)" -eq 0 ]; then
        return 0  # running as root
    fi
    if command -v sudo &>/dev/null && sudo -n true 2>/dev/null; then
        return 0  # sudo available and cached
    fi
    if command -v sudo &>/dev/null; then
        # sudo exists but needs password — test it
        echo "  This script needs sudo for package installation and MySQL setup."
        if sudo true; then
            return 0
        fi
    fi
    echo "ERROR: This script requires root or sudo privileges."
    echo "  Run as root:  sudo ./setup.sh"
    echo "  Or skip:      ./setup.sh --skip-mysql"
    exit 1
}

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

MONO_MIN_MAJOR=6
MONO_MIN_MINOR=12

if ! command -v mono &>/dev/null; then
    echo "ERROR: mono not found."
    echo ""
    echo "The system mono package is too old. Install from the Mono project repo:"
    echo ""
    echo "  sudo apt install -y ca-certificates gnupg"
    echo "  sudo gpg --homedir /tmp --no-default-keyring --keyring /usr/share/keyrings/mono-official-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
    echo "  echo \"deb [signed-by=/usr/share/keyrings/mono-official-archive-keyring.gpg] https://download.mono-project.com/repo/ubuntu stable-focal main\" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list"
    echo "  sudo apt update && sudo apt install -y mono-devel msbuild"
    exit 1
fi

# Check mono version (need >= 6.12 for C# 7.1+ and .NET 4.8 target)
MONO_VER=$(mono --version | head -1 | grep -oP '\d+\.\d+' | head -1)
MONO_MAJOR=$(echo "$MONO_VER" | cut -d. -f1)
MONO_MINOR=$(echo "$MONO_VER" | cut -d. -f2)

if [ "$MONO_MAJOR" -lt "$MONO_MIN_MAJOR" ] || { [ "$MONO_MAJOR" -eq "$MONO_MIN_MAJOR" ] && [ "$MONO_MINOR" -lt "$MONO_MIN_MINOR" ]; }; then
    echo "ERROR: Mono $MONO_VER is too old (need >= $MONO_MIN_MAJOR.$MONO_MIN_MINOR)."
    echo "  Your version: $MONO_VER"
    echo ""
    echo "The system mono package on Ubuntu is outdated. Install from the Mono project repo:"
    echo ""
    echo "  sudo gpg --homedir /tmp --no-default-keyring --keyring /usr/share/keyrings/mono-official-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
    echo "  echo \"deb [signed-by=/usr/share/keyrings/mono-official-archive-keyring.gpg] https://download.mono-project.com/repo/ubuntu stable-focal main\" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list"
    echo "  sudo apt update && sudo apt install -y mono-devel msbuild"
    exit 1
fi
echo "  Mono: $MONO_VER (>= $MONO_MIN_MAJOR.$MONO_MIN_MINOR required)"

if command -v msbuild &>/dev/null; then
    BUILD_CMD="msbuild"
elif command -v xbuild &>/dev/null; then
    echo "WARNING: xbuild is deprecated. Install msbuild from the Mono project repo."
    BUILD_CMD="xbuild"
else
    echo "ERROR: msbuild not found."
    echo "Install: sudo apt install -y msbuild"
    exit 1
fi
echo "  Build tool: $BUILD_CMD"
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

    # Install MySQL if not present
    if ! command -v mysql &>/dev/null; then
        check_sudo
        echo "  MySQL not found. Installing..."
        if command -v apt &>/dev/null; then
            # Debian/Ubuntu — noninteractive install avoids the root password prompt
            sudo DEBIAN_FRONTEND=noninteractive apt install -y mysql-server 2>/dev/null
            sudo systemctl start mysql 2>/dev/null || sudo service mysql start 2>/dev/null
        elif command -v yum &>/dev/null; then
            sudo yum install -y mysql-server
            sudo systemctl start mysqld
        elif command -v pacman &>/dev/null; then
            sudo pacman -S --noconfirm mariadb
            sudo mysql_install_db --user=mysql --basedir=/usr --datadir=/var/lib/mysql
            sudo systemctl start mariadb
        else
            echo "  ERROR: Could not detect package manager. Install MySQL manually."
            echo "  Then re-run: ./setup.sh"
            exit 1
        fi
        echo "  MySQL installed"
    fi

    if command -v mysql &>/dev/null; then
        # Check if root can connect without a password (fresh install)
        if [ -z "$MYSQL_PASSWORD" ] && mysql -u "$MYSQL_USER" -e "SELECT 1" &>/dev/null; then
            echo "  MySQL root has no password — generating one..."

            # Generate a secure root password using the EFF wordlist
            if command -v python3 &>/dev/null && [ -f "$ROOT/hash_passwords.py" ]; then
                MYSQL_ROOT_PW=$(python3 -c "
import sys; sys.path.insert(0, '$ROOT')
from hash_passwords import generate_password
print(generate_password(5))
")
            else
                MYSQL_ROOT_PW=$(tr -dc 'a-zA-Z0-9' < /dev/urandom | head -c 20)
            fi

            mysql -u "$MYSQL_USER" -e "ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY '$MYSQL_ROOT_PW'; FLUSH PRIVILEGES;" 2>/dev/null
            MYSQL_PASSWORD="$MYSQL_ROOT_PW"

            CREDS_FILE="$ROOT/credentials.txt"
            echo "" >> "$CREDS_FILE"
            echo "# MySQL root password (generated by setup.sh)" >> "$CREDS_FILE"
            echo "mysql root / $MYSQL_ROOT_PW" >> "$CREDS_FILE"

            echo "  MySQL root password set and saved to: $CREDS_FILE"
            echo "  Password: $MYSQL_ROOT_PW"
            echo "  DELETE credentials.txt after noting the password!"
        fi

        PASS_ARG=""
        [ -n "$MYSQL_PASSWORD" ] && PASS_ARG="-p$MYSQL_PASSWORD"

        SQL_FILE="$ROOT/Content/spellbinder-server.sql"
        if [ -f "$SQL_FILE" ]; then
            mysql -u "$MYSQL_USER" $PASS_ARG -e "CREATE DATABASE IF NOT EXISTS magestorm;"
            mysql -u "$MYSQL_USER" $PASS_ARG magestorm < "$SQL_FILE"
            mysql -u "$MYSQL_USER" $PASS_ARG -e "CREATE USER IF NOT EXISTS 'localweb'@'localhost' IDENTIFIED WITH mysql_native_password BY ''; GRANT ALL PRIVILEGES ON magestorm.* TO 'localweb'@'localhost'; FLUSH PRIVILEGES;"
            # Widen password column and hash existing plaintext passwords
            if command -v python3 &>/dev/null && [ -f "$ROOT/hash_passwords.py" ]; then
                pip3 install -q pymysql 2>/dev/null || true
                python3 "$ROOT/hash_passwords.py" --create-defaults --mysql-user "$MYSQL_USER" ${MYSQL_PASSWORD:+--mysql-password "$MYSQL_PASSWORD"}
            fi
            echo "  MySQL: OK"
        else
            echo "  WARNING: spellbinder-server.sql not found in Content/"
            echo "  You need the game data files. See README.md for details."
        fi
    else
        echo "  ERROR: MySQL installation failed"
        exit 1
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

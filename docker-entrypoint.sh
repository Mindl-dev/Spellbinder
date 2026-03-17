#!/bin/bash
set -e

echo "=== SpellBinder Server ==="

# Parse flags
DEV_MODE=false
for arg in "$@"; do
    case "$arg" in
        --dev) DEV_MODE=true; shift ;;
    esac
done

CREDENTIALS_FILE="/app/credentials.txt"

# Copy game content from mounted volume (not baked into image — copyrighted data)
if [ -d /app/Content ]; then
    echo "Copying game content from /app/Content..."
    cp -f Content/Spells.dat Content/Arenas.dat . 2>/dev/null || true
    if [ -d Content/Grids ] && [ ! -d Grids ]; then
        cp -r Content/Grids/ Grids/
        # Fix case: server expects Geometry.dat, installer has GEOMETRY.DAT
        find Grids/ -iname "geometry.dat" ! -name "Geometry.dat" \
            -exec sh -c 'mv "$1" "$(dirname "$1")/Geometry.dat"' _ {} \; 2>/dev/null || true
    fi
else
    echo "WARNING: No /app/Content mount found. Mount game content via docker-compose volume."
    echo "  docker run -v /path/to/Content:/app/Content ..."
fi

# Start MySQL if not already running
if command -v mysqld &>/dev/null; then
    if ! pgrep -x mysqld &>/dev/null; then
        echo "Starting MySQL..."
        mkdir -p /var/run/mysqld
        chown mysql:mysql /var/run/mysqld
        mysqld --user=mysql --datadir=/var/lib/mysql &
        # Wait for MySQL to be ready
        for i in $(seq 1 30); do
            if mysqladmin ping -u root --silent 2>/dev/null; then
                break
            fi
            sleep 1
        done
    fi

    # Import schema if database doesn't exist
    if ! mysql -u root -e "USE spellbinder" 2>/dev/null; then
        echo "Initializing database..."
        mysql -u root -e "CREATE DATABASE spellbinder;"
        if [ -f Content/spellbinder-server.sql ]; then
            mysql -u root spellbinder < Content/spellbinder-server.sql
        fi
        mysql -u root -e "CREATE USER IF NOT EXISTS 'localweb'@'localhost' IDENTIFIED WITH mysql_native_password BY ''; GRANT ALL PRIVILEGES ON spellbinder.* TO 'localweb'@'localhost'; FLUSH PRIVILEGES;"

        # Create player accounts
        if [ -f hash_passwords.py ]; then
            if [ "$DEV_MODE" = true ]; then
                echo "Dev mode: creating accounts with simple passwords (test1/test1, test2/test2, ...)"
                python3 hash_passwords.py --create-defaults --dev --mysql-user root
                echo "Dev accounts created (password = username)" | tee "$CREDENTIALS_FILE"
            else
                echo "Creating accounts with generated passwords..."
                python3 hash_passwords.py --create-defaults --mysql-user root | tee "$CREDENTIALS_FILE"
                echo ""
                echo "Credentials saved to $CREDENTIALS_FILE"
                echo "Back this up — passwords are hashed in the database and cannot be recovered."
            fi
        fi
    else
        echo "Database 'spellbinder' already exists"
    fi
fi

echo "Starting SpellBinder server..."
exec mono SpellServer.exe --headless "$@"

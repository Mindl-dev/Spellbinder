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
        # Fix case: C# server expects Grid00/Misc.dat etc, installer has grid00/MISC.DAT
        for dir in Grids/grid[0-9][0-9]; do
            [ -d "$dir" ] || continue
            target="Grids/Grid$(basename "$dir" | sed 's/grid//')"
            [ "$dir" != "$target" ] && mv "$dir" "$target"
        done
        # Fix file case: installer has WORLD.DAT, server expects World.dat
        # Map uppercase filenames to the exact casing the C# code uses
        for dir in Grids/Grid*/; do
            [ -d "$dir" ] || continue
            cd "$dir"
            for f in *.DAT *.dat; do
                [ -f "$f" ] || continue
                case "$(echo "$f" | tr '[:upper:]' '[:lower:]')" in
                    world.dat)       target="World.dat" ;;
                    grid.dat)        target="Grid.dat" ;;
                    misc.dat)        target="Misc.dat" ;;
                    geometry.dat)    target="Geometry.dat" ;;
                    trigger.dat)     target="Trigger.dat" ;;
                    objects.dat)     target="Objects.dat" ;;
                    subpixel.dat)    target="SubPixel.dat" ;;
                    *)               target="$f" ;;
                esac
                [ "$f" != "$target" ] && mv "$f" "$target" 2>/dev/null || true
            done
            cd /app
        done
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

    # Allow root login via TCP (needed for pymysql in hash_passwords.py)
    mysql -u root -e "ALTER USER 'root'@'localhost' IDENTIFIED BY ''; FLUSH PRIVILEGES;" 2>/dev/null || true
    mysql -u root -e "GRANT ALL PRIVILEGES ON *.* TO 'root'@'localhost' WITH GRANT OPTION; FLUSH PRIVILEGES;" 2>/dev/null || true

    # Import schema if database doesn't exist
    if ! mysql -u root -e "USE magestorm" 2>/dev/null; then
        echo "Initializing database..."
        mysql -u root -e "CREATE DATABASE magestorm;"
        if [ -f Content/spellbinder-server.sql ]; then
            # Fix MySQL 8.0 collation for MariaDB compatibility
            sed 's/utf8mb4_0900_ai_ci/utf8mb4_general_ci/g' Content/spellbinder-server.sql \
                | mysql -u root magestorm
        fi
        mysql -u root -e "CREATE USER IF NOT EXISTS 'localweb'@'localhost' IDENTIFIED BY ''; GRANT ALL PRIVILEGES ON magestorm.* TO 'localweb'@'localhost'; FLUSH PRIVILEGES;"

        # Create player accounts
        if [ -f hash_passwords.py ]; then
            printf "# SpellBinder Server — Generated Credentials\n# DELETE THIS FILE after noting the passwords!\n" > "$CREDENTIALS_FILE"
            if [ "$DEV_MODE" = true ]; then
                echo "Dev mode: creating accounts with simple passwords (password = username)"
                python3 hash_passwords.py --create-defaults --dev --mysql-user root --database magestorm | tee -a "$CREDENTIALS_FILE"
            else
                echo "Creating accounts with generated passwords..."
                python3 hash_passwords.py --create-defaults --mysql-user root --database magestorm | tee -a "$CREDENTIALS_FILE"
                echo ""
                echo "Credentials saved to $CREDENTIALS_FILE"
                echo "Back this up — passwords are hashed in the database and cannot be recovered."
            fi
        fi
    else
        echo "Database 'magestorm' already exists"
    fi
fi

mkdir -p Logs/Main Logs/Cheat

echo "Starting SpellBinder server..."
export MONO_IOMAP=all
exec mono SpellServer.exe --headless "$@"

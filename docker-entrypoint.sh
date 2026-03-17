#!/bin/bash
set -e

echo "=== SpellBinder Server ==="

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

        # Create player accounts with generated passwords
        if [ -f hash_passwords.py ]; then
            python3 hash_passwords.py --create-defaults --mysql-user root
        fi
    else
        echo "Database 'spellbinder' already exists"
    fi
fi

echo "Starting SpellBinder server..."
exec mono SpellServer.exe --headless "$@"

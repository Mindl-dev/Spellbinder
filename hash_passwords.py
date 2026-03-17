#!/usr/bin/env python3
"""Migrate plaintext passwords to PBKDF2 hashes in the SpellBinder MySQL database.
Also creates default accounts if they don't exist.

Usage:
    python3 hash_passwords.py [--create-defaults] [--mysql-user root] [--mysql-password pw]
"""

import argparse
import hashlib
import os
import base64
import sys

ITERATIONS = 100000
SALT_SIZE = 16
HASH_SIZE = 32
PREFIX = "$PBKDF2$"

def load_wordlist() -> list:
    """Load the EFF short wordlist from file. Format: 'dice_number\\tword' per line."""
    wordlist_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "eff_short_wordlist.txt")
    if not os.path.exists(wordlist_path):
        print(f"ERROR: Wordlist not found at {wordlist_path}")
        print("Download from: https://www.eff.org/files/2016/09/08/eff_short_wordlist_1.txt")
        sys.exit(1)
    words = []
    with open(wordlist_path) as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            parts = line.split()
            if len(parts) >= 2:
                words.append(parts[-1])  # word is the last column
    return words

WORDLIST = None  # loaded lazily


def generate_password(num_words: int = 3) -> str:
    """Generate a diceware-style password (EFF short wordlist) that fits in 20 chars."""
    import random
    global WORDLIST
    if WORDLIST is None:
        WORDLIST = load_wordlist()
    rng = random.SystemRandom()
    while True:
        words = [rng.choice(WORDLIST) for _ in range(num_words)]
        pw = ".".join(words)
        if len(pw) <= 20:
            return pw


def pbkdf2_hash(password: str) -> str:
    salt = os.urandom(SALT_SIZE)
    dk = hashlib.pbkdf2_hmac('sha1', password.encode('utf-8'), salt, ITERATIONS, dklen=HASH_SIZE)
    return f"{PREFIX}{ITERATIONS}${base64.b64encode(salt).decode()}${base64.b64encode(dk).decode()}"


def main():
    parser = argparse.ArgumentParser(description="Migrate passwords to PBKDF2")
    parser.add_argument("--create-defaults", action="store_true", help="Create default test accounts")
    parser.add_argument("--mysql-user", default="root", help="MySQL username")
    parser.add_argument("--mysql-password", default="", help="MySQL password")
    parser.add_argument("--database", default="spellbinder", help="Database name")
    parser.add_argument("--dry-run", action="store_true", help="Print changes without applying")
    parser.add_argument("--dev", action="store_true", help="Use username as password (dev/testing only)")
    args = parser.parse_args()

    try:
        import MySQLdb
    except ImportError:
        try:
            import pymysql
            pymysql.install_as_MySQLdb()
            import MySQLdb
        except ImportError:
            print("ERROR: Need mysqlclient or pymysql. Install: pip3 install pymysql")
            sys.exit(1)

    conn = MySQLdb.connect(
        host="localhost",
        user=args.mysql_user,
        passwd=args.mysql_password,
        db=args.database
    )
    cursor = conn.cursor()

    # Widen password column if needed
    cursor.execute("ALTER TABLE accounts MODIFY password VARCHAR(128) NOT NULL")
    conn.commit()
    print("Ensured password column is VARCHAR(128)")

    # Create default accounts if requested
    if args.create_defaults:
        # 30 SpellBinder-flavored player accounts + 1 admin
        # Names are thematic — mages, classes, and lore from the game
        accounts = [
            ("admin", 5),        # Server admin/sysop
            ("Ashenveil", 0),
            ("Brimstone", 0),
            ("Cinderspell", 0),
            ("Doomweaver", 0),
            ("Emberclaw", 0),
            ("Frostbane", 0),
            ("Grimthorn", 0),
            ("Hexblade", 0),
            ("Ironshroud", 0),
            ("Jadestorm", 0),
            ("Khaelrune", 0),
            ("Lichward", 0),
            ("Moonshard", 0),
            ("Nexusworn", 0),
            ("Obsidianmaw", 0),
            ("Pyrelight", 0),
            ("Quartzfang", 0),
            ("Runekeeper", 0),
            ("Shadowpact", 0),
            ("Thornweald", 0),
            ("Umbralcast", 0),
            ("Voidtender", 0),
            ("Wyrmscribe", 0),
            ("Xenolith", 0),
            ("Yewshade", 0),
            ("Zephyrkin", 0),
            ("Spellslinger", 0),
            ("Arcanist", 0),
            ("Dragonsworn", 0),
            ("Gryphonheart", 0),
            ("Phoenixborn", 0),
        ]
        creds_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), "credentials.txt")
        created = []
        for username, admin_level in accounts:
            # Check if account already exists
            cursor.execute("SELECT COUNT(*) FROM accounts WHERE username = %s", (username,))
            if cursor.fetchone()[0] > 0:
                print(f"  Account '{username}' already exists — skipping")
                continue
            password = username.lower() if args.dev else generate_password()
            hashed = pbkdf2_hash(password)
            if args.dry_run:
                print(f"  Would create: {username} / {password} (admin={admin_level})")
            else:
                cursor.execute(
                    "INSERT INTO accounts (username, password, Admin) VALUES (%s, %s, %s)",
                    (username, hashed, admin_level)
                )
                created.append((username, password, admin_level))
        conn.commit()

        if created:
            print(f"\n  === Default Accounts Created ===")
            print(f"  Credentials saved to: {creds_file}")
            print(f"  {'Username':<12} {'Password':<22} {'Role'}")
            print(f"  {'-'*12} {'-'*22} {'-'*8}")
            with open(creds_file, "w") as f:
                f.write("# SpellBinder Server — Generated Credentials\n")
                f.write("# DELETE THIS FILE after noting the passwords!\n\n")
                for username, password, admin_level in created:
                    role = "admin" if admin_level > 0 else "player"
                    print(f"  {username:<12} {password:<22} {role}")
                    f.write(f"{username} / {password} ({role})\n")
            print()
        else:
            print("  All default accounts already exist")

    # Migrate existing plaintext passwords
    cursor.execute("SELECT AccountID, username, password FROM accounts")
    rows = cursor.fetchall()

    migrated = 0
    skipped = 0
    for account_id, username, stored_pw in rows:
        if stored_pw.startswith(PREFIX):
            skipped += 1
            continue

        hashed = pbkdf2_hash(stored_pw)
        if args.dry_run:
            print(f"  Would migrate: {username} (ID={account_id})")
        else:
            cursor.execute(
                "UPDATE accounts SET password = %s WHERE AccountID = %s",
                (hashed, account_id)
            )
        migrated += 1

    conn.commit()
    conn.close()

    print(f"Done. Migrated: {migrated}, Already hashed: {skipped}")
    if args.dry_run:
        print("(Dry run — no changes applied)")


if __name__ == "__main__":
    main()

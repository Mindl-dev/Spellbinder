# SpellBinder Community Server

Fork of [Magestorm/Magestorm](https://github.com/Magestorm/Magestorm), adapted for SpellBinder: The Nexus Conflict (1999).

## Quick Start

### Windows (PowerShell)
```powershell
.\setup.ps1
```

The setup script handles NuGet restore, build, content copy, MySQL database creation, and config.

### Docker
```bash
docker build -t spellbinder .
docker run -d -p 10601:10601/udp -p 10602:10602/tcp \
  -v ./Content:/app/Content spellbinder
```

The `Content/` volume mount is required — game data files (Spells.dat, Arenas.dat, Grids/) are copyrighted and not baked into the image. The entrypoint handles MariaDB setup, schema import, file case normalization, and account creation automatically.

On first run, diceware passwords are generated and saved to `/app/credentials.txt`:
```bash
docker exec spellbinder cat /app/credentials.txt
```

> **`--dev` flag**: `docker run ... spellbinder --dev` creates accounts with simple passwords (password = lowercase username). **Do not use in production** — these credentials are trivially guessable.

### Options
| Flag | PowerShell | Bash | Description |
|------|-----------|------|-------------|
| Skip MySQL | `-SkipMySQL` | `--skip-mysql` | Skip database setup (already configured) |
| Headless | `-Headless` | `--headless` | Start server after setup (no GUI) |
| MySQL user | `-MySQLUser root` | `--mysql-user root` | MySQL admin username |
| MySQL pass | `-MySQLPassword pw` | `--mysql-password pw` | MySQL admin password |

## Prerequisites

1. **Windows**: [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) + [VS Build Tools 2022](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)
2. **Linux**: `mono-devel` + `mono-runtime`
3. **MySQL Server 8.0**: https://dev.mysql.com/downloads/installer/

## Manual Setup

If you prefer to set up manually instead of using the scripts:

### 1. NuGet Restore
```bash
nuget.exe restore Spellbinder.sln
```

### 2. Build
```bash
# Windows
"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" SpellServer/SpellServer.csproj -p:Configuration=Debug -p:Platform=x86

# Linux
msbuild SpellServer/SpellServer.csproj /p:Configuration=Debug /p:Platform=x86
```

### 3. MySQL Setup
```sql
CREATE DATABASE spellbinder;
USE spellbinder;
SOURCE Content/spellbinder-server.sql;

CREATE USER 'localweb'@'localhost' IDENTIFIED WITH mysql_native_password BY '';
GRANT ALL PRIVILEGES ON spellbinder.* TO 'localweb'@'localhost';
FLUSH PRIVILEGES;
```

### 4. Content Files
Grid data must be in the `Build/Debug/Grids/` directory. Run `copy_content.py` or copy manually from `Content/Grids/`. Note the case fix: `GEOMETRY.DAT` must be renamed to `Geometry.dat`.

### 5. Configuration
Update `SpellServer/app.config`:

| Setting | Value | Notes |
|---------|-------|-------|
| DatabaseName | spellbinder | Must match SQL database name |
| ServerVersion | 2.0.2 | Must match client version |
| ListenPort | 10602 | TCP game port |
| UDPPort | 10601 | UDP game port |

## Running

```bash
# Windows GUI
Build\Debug\SpellServer.exe

# Windows headless (no GUI, console logging)
Build\Debug\SpellServer.exe --headless

# Linux
mono Build/Debug/SpellServer.exe --headless
```

Expected startup log:
```
400 Spells loaded.
Server listening on UDP port 10601.
Server listening on TCP port 10602.
4 out of 4 Arenas loaded.
```

## Tests

```bash
# Windows
.\packages\NUnit.ConsoleRunner.3.16.3\tools\nunit3-console.exe SpellServer.Tests\bin\Debug\SpellServer.Tests.dll

# Linux
mono packages/NUnit.ConsoleRunner.3.16.3/tools/nunit3-console.exe SpellServer.Tests/bin/Debug/SpellServer.Tests.dll
```

## Client Setup

The server targets the **full game** (2001 release, `game.exe`). Set `main.dat`:
```
address=127.0.0.1
```

Use the **unpatched** client binary. The discord-patched DLL has UDP changes that are incompatible.

## Changes from Upstream

- **INI cache**: Spell loading from 15s to instant (cached `GetPrivateProfileString`)
- **Headless mode**: `--headless` flag for VPS deployment, console + file logging
- **Log routing**: `Program.Log()` with category routing (Main, Chat, Cheat, Admin, Whisper, Report, Misc)
- **Packet source_id**: Fixed `Packet` constructor to pass `ArenaPlayerId` so clients can see other players
- **Overflow fix**: `unchecked` block in `GetChecksum` instead of global `CheckForOverflowUnderflow=false`
- **LogBox FIFO**: Fixed reverse-order message drain in GUI log
- **Unit tests**: 35 tests covering packets, INI cache, checksums, data casting, logging

# SpellBinder Community Server — Setup Script
# Run from the community_server/ directory
# Prerequisites: .NET Framework 4.8 Dev Pack, VS Build Tools 2022, MySQL Server 8.0

param(
    [switch]$SkipMySQL,
    [switch]$Headless,
    [string]$MySQLUser = "root",
    [string]$MySQLPassword = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "=== SpellBinder Server Setup ===" -ForegroundColor Cyan

# 1. Check prerequisites
Write-Host "`n[1/7] Checking prerequisites..." -ForegroundColor Yellow

$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    Write-Host "ERROR: MSBuild not found at $msbuild" -ForegroundColor Red
    Write-Host "Install Visual Studio Build Tools 2022: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022"
    exit 1
}
Write-Host "  MSBuild: OK" -ForegroundColor Green

# 2. Download NuGet if needed
Write-Host "`n[2/7] Checking NuGet..." -ForegroundColor Yellow
$nuget = Join-Path $root "nuget.exe"
if (-not (Test-Path $nuget)) {
    Write-Host "  Downloading nuget.exe..."
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nuget
}
Write-Host "  NuGet: OK" -ForegroundColor Green

# 3. Restore NuGet packages
Write-Host "`n[3/7] Restoring NuGet packages..." -ForegroundColor Yellow
& $nuget restore (Join-Path $root "Spellbinder.sln") -Verbosity quiet
Write-Host "  Packages restored" -ForegroundColor Green

# 4. Build
Write-Host "`n[4/7] Building SpellServer..." -ForegroundColor Yellow
& $msbuild (Join-Path $root "SpellServer\SpellServer.csproj") `
    -p:Configuration=Debug -p:Platform=x86 `
    -verbosity:minimal `
    -nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "  Build: OK" -ForegroundColor Green

# 5. Copy content files (grid data)
Write-Host "`n[5/7] Copying content files..." -ForegroundColor Yellow
$contentScript = Join-Path $root "copy_content.py"
if (Test-Path $contentScript) {
    python $contentScript
} else {
    # Manual copy: Content/Grids -> Build/Debug/Grids with case fix
    $src = Join-Path $root "Content\Grids"
    $dst = Join-Path $root "Build\Debug\Grids"
    if (Test-Path $src) {
        Get-ChildItem $src -Directory | ForEach-Object {
            $gridDst = Join-Path $dst $_.Name
            New-Item -ItemType Directory -Force -Path $gridDst | Out-Null
            Copy-Item "$($_.FullName)\*" $gridDst -Recurse -Force
            # Fix case: GEOMETRY.DAT -> Geometry.dat
            $geomUpper = Join-Path $gridDst "GEOMETRY.DAT"
            $geomLower = Join-Path $gridDst "Geometry.dat"
            if ((Test-Path $geomUpper) -and -not (Test-Path $geomLower)) {
                Rename-Item $geomUpper $geomLower
            }
        }
        Write-Host "  Content copied" -ForegroundColor Green
    } else {
        Write-Host "  WARNING: Content/Grids not found - grids won't load" -ForegroundColor Yellow
    }
}

# 6. MySQL setup
if (-not $SkipMySQL) {
    Write-Host "`n[6/7] Setting up MySQL database..." -ForegroundColor Yellow
    $mysql = Get-Command mysql -ErrorAction SilentlyContinue
    if ($mysql) {
        $sqlFile = Join-Path $root "Content\spellbinder-server.sql"
        if (Test-Path $sqlFile) {
            $passArg = if ($MySQLPassword) { "-p$MySQLPassword" } else { "" }
            & mysql -u $MySQLUser $passArg -e "CREATE DATABASE IF NOT EXISTS spellbinder;"
            & mysql -u $MySQLUser $passArg spellbinder < $sqlFile
            & mysql -u $MySQLUser $passArg -e "CREATE USER IF NOT EXISTS 'localweb'@'localhost' IDENTIFIED WITH mysql_native_password BY ''; GRANT ALL PRIVILEGES ON spellbinder.* TO 'localweb'@'localhost'; FLUSH PRIVILEGES;"
            Write-Host "  MySQL: OK" -ForegroundColor Green
        } else {
            Write-Host "  WARNING: spellbinder-server.sql not found" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  WARNING: mysql not in PATH - skipping DB setup" -ForegroundColor Yellow
        Write-Host "  Run with -SkipMySQL if database is already configured"
    }
} else {
    Write-Host "`n[6/7] Skipping MySQL setup (-SkipMySQL)" -ForegroundColor Yellow
}

# 7. Update config
Write-Host "`n[7/7] Updating configuration..." -ForegroundColor Yellow
$configSrc = Join-Path $root "SpellServer\app.config"
$configDst = Join-Path $root "Build\Debug\SpellServer.exe.config"
if (Test-Path $configSrc) {
    Copy-Item $configSrc $configDst -Force
    Write-Host "  Config copied" -ForegroundColor Green
}

# Done
Write-Host "`n=== Setup Complete ===" -ForegroundColor Cyan
Write-Host ""
$exe = Join-Path $root "Build\Debug\SpellServer.exe"
if ($Headless) {
    Write-Host "Starting server in headless mode..."
    & $exe --headless
} else {
    Write-Host "To start the server:"
    Write-Host "  GUI mode:      $exe"
    Write-Host "  Headless mode: $exe --headless"
    Write-Host ""
    Write-Host "To run tests:"
    Write-Host "  .\packages\NUnit.ConsoleRunner.3.16.3\tools\nunit3-console.exe SpellServer.Tests\bin\Debug\SpellServer.Tests.dll"
}

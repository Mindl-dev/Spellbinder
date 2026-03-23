# Kill, build, test, and restart the C# server
param(
    [switch]$SkipTests,
    [string]$Debug
)

$ErrorActionPreference = "Stop"
$ServerDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $ServerDir

try {
    Write-Host "=== Stopping server ==="
    Stop-Process -Name SpellServer -Force -ErrorAction SilentlyContinue

    Write-Host "=== Building ==="
    & "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" `
        SpellServer\SpellServer.csproj -p:Configuration=Debug -p:Platform=x86 -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "BUILD FAILED" }

    & "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" `
        SpellServer.Tests\SpellServer.Tests.csproj -p:Configuration=Debug -p:Platform=AnyCPU -v:minimal
    if ($LASTEXITCODE -ne 0) { throw "BUILD FAILED" }

    if (-not $SkipTests) {
        Write-Host "=== Running tests ==="
        & packages\NUnit.ConsoleRunner.3.16.3\tools\nunit3-console.exe `
            SpellServer.Tests\bin\Debug\SpellServer.Tests.dll --noresult
        if ($LASTEXITCODE -ne 0) { throw "TESTS FAILED" }
    }

    Write-Host "=== Starting server ==="
    Set-Location "$ServerDir\Build\Debug"
    $serverArgs = @("--headless")
    if ($Debug -and $Debug.Trim().Length -gt 0) {
        $normalizedDebug = $Debug.Trim()
        if ($normalizedDebug.StartsWith("--debug=")) {
            $normalizedDebug = $normalizedDebug.Substring("--debug=".Length)
        }
        elseif ($normalizedDebug.StartsWith("--debug")) {
            $normalizedDebug = $normalizedDebug.Substring("--debug".Length).TrimStart("=")
        }

        $serverArgs += "--debug=$normalizedDebug"
        Write-Host "=== Debug flags: $normalizedDebug ==="
    }
    & .\SpellServer.exe @serverArgs
}
finally {
    Pop-Location
}

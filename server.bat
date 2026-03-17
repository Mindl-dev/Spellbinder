@echo off
REM Kill, build, test, and restart the C# server
cd /d "%~dp0"

echo === Stopping server ===
taskkill /IM SpellServer.exe /F 2>nul

echo === Building ===
"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" SpellServer\SpellServer.csproj -p:Configuration=Debug -p:Platform=x86 -v:minimal
if errorlevel 1 (
    echo BUILD FAILED
    pause
    exit /b 1
)
"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" SpellServer.Tests\SpellServer.Tests.csproj -p:Configuration=Debug -p:Platform=AnyCPU -v:minimal
if errorlevel 1 (
    echo BUILD FAILED
    pause
    exit /b 1
)

echo === Running tests ===
packages\NUnit.ConsoleRunner.3.16.3\tools\nunit3-console.exe SpellServer.Tests\bin\Debug\SpellServer.Tests.dll --noresult
if errorlevel 1 (
    echo TESTS FAILED
    pause
    exit /b 1
)

echo === Starting server ===
cd Build\Debug
SpellServer.exe --headless

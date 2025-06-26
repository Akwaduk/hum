@echo off
setlocal

echo 🔨 Installing hum CLI tool...

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET SDK is not installed or not in PATH. Please install .NET 9.0 SDK first.
    exit /b 1
)

echo ✅ .NET SDK found

REM Clean, build, and pack
echo 🧹 Cleaning previous builds...
dotnet clean --configuration Release

echo 🔧 Building project...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo ❌ Build failed
    exit /b 1
)

echo 📦 Creating NuGet package...
if not exist "nupkg" mkdir nupkg
dotnet pack --configuration Release --output ./nupkg
if %errorlevel% neq 0 (
    echo ❌ Pack failed
    exit /b 1
)

REM Uninstall existing tool if present
echo 🔄 Checking for existing installation...
dotnet tool uninstall --global hum >nul 2>&1

echo 🚀 Installing hum as global tool...
dotnet tool install --global --add-source ./nupkg hum --version 1.0.0
if %errorlevel% neq 0 (
    echo ❌ Installation failed
    exit /b 1
)

echo.
echo ✅ hum CLI tool installed successfully!
echo.
echo 🎉 You can now use 'hum' from anywhere in your terminal!
echo.
echo Try these commands:
echo   hum --help
echo   hum config --show
echo   hum create my-app --template dotnet-webapi --description "My awesome API"

endlocal

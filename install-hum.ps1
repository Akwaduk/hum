#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs the hum CLI tool globally
.DESCRIPTION
    This script builds the hum project, packs it as a NuGet package, and installs it as a global .NET tool.
    After running this script, you can use 'hum' from anywhere in your terminal.
.EXAMPLE
    .\install-hum.ps1
#>

[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$Verbose
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get the script directory (where hum.csproj should be)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ScriptDir "hum.csproj"

Write-Host "🔨 Installing hum CLI tool..." -ForegroundColor Cyan

# Check if .NET SDK is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Error "❌ .NET SDK is not installed or not in PATH. Please install .NET 9.0 SDK first."
    exit 1
}

# Check if project file exists
if (-not (Test-Path $ProjectFile)) {
    Write-Error "❌ hum.csproj not found at: $ProjectFile"
    exit 1
}

try {
    # Change to project directory
    Push-Location $ScriptDir
    
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean --configuration Release
    
    Write-Host "🔧 Building project..." -ForegroundColor Yellow
    dotnet build --configuration Release
    
    Write-Host "📦 Creating NuGet package..." -ForegroundColor Yellow
    dotnet pack --configuration Release --output ./nupkg
    
    # Find the generated package
    $PackageFile = Get-ChildItem -Path "./nupkg" -Name "hum.*.nupkg" | Sort-Object -Descending | Select-Object -First 1
    
    if (-not $PackageFile) {
        Write-Error "❌ Could not find generated NuGet package in ./nupkg/"
        exit 1
    }    
    $PackagePath = Join-Path "./nupkg" $PackageFile
    Write-Host "📦 Package created: $PackagePath" -ForegroundColor Green
    
    # Check if hum is already installed
    $existingTool = dotnet tool list --global | Where-Object { $_ -match "^hum\s" }
    
    if ($existingTool) {
        Write-Host "🔄 Uninstalling existing hum tool..." -ForegroundColor Yellow
        dotnet tool uninstall --global hum
    }
    
    Write-Host "🚀 Installing hum as global tool..." -ForegroundColor Yellow
    if ($Force) {
        dotnet tool install --global --add-source ./nupkg hum --version 1.0.0
    } else {
        dotnet tool install --global --add-source ./nupkg hum --version 1.0.0
    }
    
    Write-Host "✅ hum CLI tool installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎉 You can now use 'hum' from anywhere in your terminal!" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Try these commands:" -ForegroundColor White
    Write-Host "  hum --help" -ForegroundColor Gray
    Write-Host "  hum config --show" -ForegroundColor Gray
    Write-Host "  hum create my-app --template dotnet-webapi --description 'My awesome API'" -ForegroundColor Gray
    
} catch {
    Write-Error "❌ Installation failed: $($_.Exception.Message)"
    exit 1
} finally {
    Pop-Location
}

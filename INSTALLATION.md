# Installation Guide

This guide will help you install and set up the `hum` CLI tool on your system.

## Quick Installation

### Option 1: Using the Installation Script (Recommended)

Run the provided installation script from the project directory:

**PowerShell:**
```powershell
.\install-hum.ps1
```

**Command Prompt:**
```cmd
.\install-hum.bat
```

### Option 2: Manual Installation

1. Build and pack the project:
   ```bash
   dotnet clean --configuration Release
   dotnet build --configuration Release
   dotnet pack --configuration Release --output ./nupkg
   ```

2. Install as a global tool:
   ```bash
   dotnet tool install --global --add-source ./nupkg hum --version 1.0.0
   ```

## Verification

After installation, verify that `hum` is working:

```bash
# Check if hum is installed
hum --help

# Run diagnostics
hum doctor

# View available commands
hum --version
```

## Configuration

Before using hum, you need to configure your GitHub credentials:

```bash
# Set your GitHub username and token
hum config --github-username your-username --github-token your-pat-token

# Or set environment variable
$env:HUM_GITHUB_TOKEN = "your-pat-token"

# Verify configuration
hum config --show
```

## Uninstalling

To uninstall the hum CLI tool:

```bash
dotnet tool uninstall --global hum
```

## Usage Examples

Once installed and configured, you can use hum from anywhere:

```bash
# Create a new API service
hum create my-orders-api --template dotnet-webapi --env production --host app-srv-03 --description "Orders processing API"

# List available templates
hum template list

# Check environment status
hum doctor
```

## Troubleshooting

### "Command not found" Error

If you get a "command not found" error:

1. Make sure the .NET SDK is installed and in your PATH
2. Verify the global tools directory is in your PATH:
   - Windows: `%USERPROFILE%\.dotnet\tools`
   - macOS/Linux: `~/.dotnet/tools`

### Reinstalling

If you need to reinstall:

```bash
# Uninstall first
dotnet tool uninstall --global hum

# Then run the install script again
.\install-hum.bat
```

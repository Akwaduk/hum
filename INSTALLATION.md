# Installation Guide

This guide will help you install and set up the `hum` CLI tool on your system.

## Quick Installation

### Option 1: Using the Installation Script (Recommended)

Run the provided installation script from the project directory:

**PowerShell (with dependency management):**
```powershell
.\install-hum.ps1
```
This script checks for required dependencies (.NET SDK, GitHub CLI, Ansible) and offers to install them using winget.

**Command Prompt (basic installation):**
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

## Dependencies

The hum CLI tool requires:

### 1. .NET SDK 9.0 or later
- **Windows:** Install via [.NET SDK download](https://dotnet.microsoft.com/download) or using winget:
  ```powershell
  winget install Microsoft.DotNet.SDK.9
  ```

### 2. GitHub CLI (gh)
- **Windows:** Install using winget:
  ```powershell
  winget install GitHub.cli
  ```
- Configure with:
  ```powershell
  gh auth login
  ```

### 3. Ansible 2.15+ (Optional)
Ansible is only required if you plan to:
- Use the deployment features of generated projects
- Set up remote Ansible orchestration
- Run end-to-end tests during development

For general use of hum, **Ansible is not required**.

When needed, install Ansible using these options:

- **Windows Options:**
  
  **Option A: WSL2 (Recommended)**
  ```powershell
  # Install WSL2 with Ubuntu
  wsl --install -d Ubuntu
  
  # In WSL2 Ubuntu terminal:
  sudo apt update
  sudo apt install -y ansible
  ```
  
  **Option B: Windows Native (via pip)**
  ```powershell
  # Ensure Python is installed
  python --version
  
  # Install Ansible via pip
  pip install ansible
  ```

The PowerShell installation script (`install-hum.ps1`) can automatically check for these dependencies and help install them when available through winget. For Ansible, the script will provide installation instructions but continue the installation without it.

### Remote Ansible Configuration

If you want to use hum with a remote Ansible server (rather than installing Ansible locally), you can configure it after installation:

```powershell
# Configure remote Ansible server connection
hum ansible-config
```

This interactive command will prompt you for:
- Remote host information
- SSH username
- SSH key path (with an option to generate and deploy a new key)

Once configured, hum will communicate with your remote Ansible server for deployment operations.

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
.\install-hum.ps1  # PowerShell with dependency management
# or
.\install-hum.bat  # Command Prompt basic installation
```

### Missing Dependencies

If you see errors related to missing dependencies:

1. For **.NET SDK** issues:
   ```powershell
   winget install Microsoft.DotNet.SDK.9
   ```

2. For **GitHub CLI** issues:
   ```powershell
   winget install GitHub.cli
   # Then authenticate:
   gh auth login
   ```

3. For **Ansible** issues:
   ```powershell
   winget install Ansible.Ansible
   ```

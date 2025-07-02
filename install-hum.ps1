# install-hum.ps1
# Installation script for hum CLI with dependency checks

# Display header
Write-Host "🔨 hum CLI Installer" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "⚠️ Note: Some dependency installations may require administrator privileges." -ForegroundColor Yellow
    Write-Host
}

# Check dependencies
$dependencies = @(
    @{
        Name = ".NET SDK 9.0";
        CheckCommand = "dotnet --list-sdks";
        CheckPattern = "9\.0";
        InstallCommand = "winget install Microsoft.DotNet.SDK.9";
        Required = $true
    },
    @{
        Name = "GitHub CLI";
        CheckCommand = "gh --version";
        CheckPattern = "";
        InstallCommand = "winget install GitHub.cli";
        Required = $true
    },
    @{
        Name = "Ansible";
        CheckCommand = "ansible --version";
        CheckPattern = "";
        InstallCommand = "python.exe -m pip install --upgrade pip;pip install ansible";
        Required = $false  # Changed to false since it's only required for specific features
    }
)

# Function to check if a command exists
function Test-CommandExists {
    param (
        [string]$Command
    )
    
    try {
        if (Get-Command $Command -ErrorAction SilentlyContinue) {
            return $true
        }
    }
    catch {
        return $false
    }
    return $false
}

# Function to check and provide guidance for Ansible installation
function Test-AnsibleInstallation {
    param (
        [switch]$Quiet
    )
    
    if (-not $Quiet) {
        Write-Host "Checking Ansible installation..." -ForegroundColor Cyan
    }
    
    # Check for native Ansible
    if (Test-CommandExists -Command "ansible") {
        $ansibleVersion = ansible --version | Select-Object -First 1
        if (-not $Quiet) {
            Write-Host "✅ Ansible is installed: $ansibleVersion" -ForegroundColor Green
        }
        return $true
    }
    
    # Check for WSL Ansible
    $wslAvailable = $false
    try {
        $wslCheck = wsl --list 2>&1
        if ($LASTEXITCODE -eq 0) {
            $wslAvailable = $true
            $wslAnsible = wsl ansible --version 2>&1
            if ($LASTEXITCODE -eq 0) {
                if (-not $Quiet) {
                    Write-Host "✅ Ansible is installed in WSL: $($wslAnsible | Select-Object -First 1)" -ForegroundColor Green
                    Write-Host "   To use Ansible, prefix commands with 'wsl', e.g., 'wsl ansible --version'" -ForegroundColor Cyan
                }
                return $true
            }
        }
    } catch {}
    
    if (-not $Quiet) {
        Write-Host "❌ Ansible is not installed or not in PATH" -ForegroundColor Yellow
        Write-Host "  Options to install Ansible:" -ForegroundColor Cyan
        if ($wslAvailable) {
            Write-Host "  1. In WSL (recommended): Run 'wsl' then 'sudo apt update; sudo apt install -y ansible'" -ForegroundColor Cyan
        } else {
            Write-Host "  1. Install WSL: 'wsl --install -d Ubuntu' then 'sudo apt update; sudo apt install -y ansible'" -ForegroundColor Cyan
        }
        Write-Host "  2. Windows native: Install Python, then 'pip install ansible'" -ForegroundColor Cyan
        Write-Host "  3. Configure remote Ansible: Run 'hum ansible-config' and point to a remote Ansible server" -ForegroundColor Cyan
        Write-Host "  For detailed instructions, see INSTALLATION.md" -ForegroundColor Cyan
    }
    
    return $false
}

# Ask if user wants to install dependencies
$installDependencies = $false
$missingDependencies = @()

Write-Host "📋 Checking dependencies..." -ForegroundColor Cyan
foreach ($dep in $dependencies) {
    Write-Host "  - $($dep.Name): " -NoNewline
    
    $commandName = ($dep.CheckCommand -split ' ')[0]
    if (Test-CommandExists -Command $commandName) {
        if ($dep.CheckPattern) {
            $output = Invoke-Expression $dep.CheckCommand 2>&1
            if ($output -match $dep.CheckPattern) {
                Write-Host "✅ Found" -ForegroundColor Green
            }
            else {
                Write-Host "⚠️ Found but may need update" -ForegroundColor Yellow
                $missingDependencies += $dep
            }
        }
        else {
            Write-Host "✅ Found" -ForegroundColor Green
        }
    }
    else {
        Write-Host "❌ Not found" -ForegroundColor Red
        $missingDependencies += $dep
    }
}

# If there are missing dependencies, offer to install them
if ($missingDependencies.Count -gt 0) {
    Write-Host
    Write-Host "Missing or outdated dependencies:" -ForegroundColor Yellow
    foreach ($dep in $missingDependencies) {
        Write-Host "  - $($dep.Name)" -ForegroundColor Yellow
    }
    
    Write-Host
    $response = Read-Host "Would you like to install these dependencies using winget? (Y/n)"
    if ($response -eq "" -or $response -eq "y" -or $response -eq "Y") {
        $installDependencies = $true
    }
    
    if ($installDependencies) {
        Write-Host
        Write-Host "📥 Installing dependencies..." -ForegroundColor Cyan
        foreach ($dep in $missingDependencies) {
            Write-Host "Installing $($dep.Name)..." -ForegroundColor Cyan
            Invoke-Expression $dep.InstallCommand
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "⚠️ There was a problem installing $($dep.Name)" -ForegroundColor Yellow
                if ($dep.Required) {
                    Write-Host "❌ $($dep.Name) is required to use hum CLI. Please install it manually." -ForegroundColor Red
                    exit 1
                }
            }
        }
    }
    elseif ($missingDependencies | Where-Object { $_.Required }) {
        $missingRequired = $missingDependencies | Where-Object { $_.Required } | ForEach-Object { $_.Name }
        Write-Host "❌ Required dependencies are missing: $($missingRequired -join ', ')" -ForegroundColor Red
        Write-Host "Please install them manually before continuing." -ForegroundColor Red
        $continue = Read-Host "Do you want to continue anyway? (y/N)"
        if ($continue -ne "y" -and $continue -ne "Y") {
            exit 1
        }
    }
    
    # Display note about missing optional dependencies
    $missingOptional = $missingDependencies | Where-Object { -not $_.Required }
    if ($missingOptional) {
        Write-Host
        Write-Host "ℹ️ Note about optional dependencies:" -ForegroundColor Yellow
        foreach ($dep in $missingOptional) {
            if ($dep.Name -eq "Ansible") {
                Write-Host "  - Ansible is only required for deployment features." -ForegroundColor Yellow
                Write-Host "    You can install it later when needed using the instructions in INSTALLATION.md." -ForegroundColor Yellow
                Write-Host "    When you run 'hum ansible-config' or other Ansible-related commands," -ForegroundColor Yellow
                Write-Host "    you will see 'not recognized' errors until Ansible is installed." -ForegroundColor Yellow
                
                # Check if WSL is available as an option
                $wslAvailable = $false
                try {
                    $wslCheck = wsl --list 2>&1
                    if ($LASTEXITCODE -eq 0) {
                        $wslAvailable = $true
                    }
                } catch {}
                
                if ($wslAvailable) {
                    Write-Host "    Quick setup with WSL: Run 'wsl' then 'sudo apt update; sudo apt install -y ansible'" -ForegroundColor Cyan
                }
            } else {
                Write-Host "  - $($dep.Name) is optional but recommended." -ForegroundColor Yellow
            }
        }
        Write-Host
    }
}

# Clean, build and pack
Write-Host
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Cyan
dotnet clean --configuration Release

Write-Host "🔧 Building project..." -ForegroundColor Cyan
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "📦 Creating NuGet package..." -ForegroundColor Cyan
if (-not (Test-Path "nupkg")) {
    New-Item -ItemType Directory -Path "nupkg" | Out-Null
}
dotnet pack --configuration Release --output ./nupkg
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Pack failed" -ForegroundColor Red
    exit 1
}

# Uninstall existing tool if present
Write-Host "🔄 Checking for existing installation..." -ForegroundColor Cyan
dotnet tool uninstall --global hum 2>&1 | Out-Null

Write-Host "🚀 Installing hum as global tool..." -ForegroundColor Cyan
dotnet tool install --global --add-source ./nupkg hum --version 1.0.0
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Installation failed" -ForegroundColor Red
    exit 1
}

# Success message
Write-Host
Write-Host "✅ hum CLI tool installed successfully!" -ForegroundColor Green
Write-Host
Write-Host "🎉 You can now use 'hum' from anywhere in your terminal!" -ForegroundColor Cyan
Write-Host
Write-Host "Try these commands:" -ForegroundColor Cyan
Write-Host "  hum --help"
Write-Host "  hum config --show"
Write-Host "  hum create my-app --template dotnet-webapi --description 'My awesome API'"

# Check and provide guidance for Ansible if it will be needed
$ansibleNeeded = $false
if (-not (Test-AnsibleInstallation -Quiet)) {
    Write-Host
    Write-Host "ℹ️ Note about Ansible:" -ForegroundColor Yellow
    Write-Host "  If you plan to use the deployment features of hum, you'll need Ansible." -ForegroundColor Yellow
    Write-Host "  When you run 'hum ansible' commands, you'll see 'not recognized' errors until Ansible is installed." -ForegroundColor Yellow
    Write-Host "  You can set up Ansible anytime later by following the instructions in INSTALLATION.md." -ForegroundColor Yellow
    
    # Check if we can create a WSL wrapper for Ansible
    $wslAvailable = $false
    try {
        $wslCheck = wsl --list 2>&1
        if ($LASTEXITCODE -eq 0) {
            $wslAvailable = $true
            # Check if WSL has Ansible installed
            $wslAnsible = wsl ansible --version 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host
                Write-Host "ℹ️ Good news! Ansible is installed in WSL." -ForegroundColor Green
                Write-Host "  Would you like to create a wrapper script so 'ansible' commands work automatically through WSL?" -ForegroundColor Cyan
                $createWrapper = Read-Host "  Create WSL-Ansible wrapper? (Y/n)"
                if ($createWrapper -eq "" -or $createWrapper -eq "y" -or $createWrapper -eq "Y") {
                    # Create a simple PowerShell script to wrap ansible commands
                    $wrapperPath = "$env:USERPROFILE\ansible.ps1"
                    $wrapperContent = @"
# Ansible WSL Wrapper
# Automatically forwards ansible commands to WSL
param(
    [Parameter(ValueFromRemainingArguments=`$true)]
    [string[]]`$arguments
)

`$argString = `$arguments -join ' '
`$command = "wsl ansible `$argString"
Invoke-Expression `$command
"@
                    Set-Content -Path $wrapperPath -Value $wrapperContent
                    
                    # Create a simple batch file to call the PS script
                    $batchPath = "$env:USERPROFILE\ansible.bat"
                    $batchContent = "@echo off`r`npowershell -ExecutionPolicy Bypass -File `"%USERPROFILE%\ansible.ps1`" %*"
                    Set-Content -Path $batchPath -Value $batchContent
                    
                    # Add the directory to PATH if not already there
                    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
                    if (-not $currentPath.Contains($env:USERPROFILE)) {
                        [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$env:USERPROFILE", "User")
                        Write-Host "  ✅ Added wrapper script to PATH. You'll need to restart your terminal to use it." -ForegroundColor Green
                    }
                    
                    Write-Host "  ✅ Created Ansible wrapper at $wrapperPath" -ForegroundColor Green
                    Write-Host "  ✅ Created Ansible batch file at $batchPath" -ForegroundColor Green
                    Write-Host "  Restart your terminal, then you can use 'ansible' commands directly!" -ForegroundColor Green
                }
            }
        }
    } catch {}
    
    # Check for pip-installed Ansible that might not be in PATH
    $pipAnsible = $false
    try {
        $pythonPath = (Get-Command python -ErrorAction SilentlyContinue).Path
        if ($pythonPath) {
            $pythonDir = Split-Path -Parent $pythonPath
            $possibleAnsiblePaths = @(
                "$pythonDir\Scripts\ansible.exe",
                "$pythonDir\ansible.exe",
                "$env:APPDATA\Python\Scripts\ansible.exe",
                "$env:LOCALAPPDATA\Programs\Python\Python*\Scripts\ansible.exe"
            )
            
            foreach ($path in $possibleAnsiblePaths) {
                if (Test-Path -Path $path -PathType Leaf) {
                    Write-Host
                    Write-Host "ℹ️ Ansible is installed via pip but might not be in your PATH!" -ForegroundColor Yellow
                    Write-Host "   Found Ansible at: $path" -ForegroundColor Cyan
                    Write-Host "   To use Ansible with hum, ensure this location is in your PATH environment variable." -ForegroundColor Cyan
                    
                    # Offer to add to PATH
                    $addToPath = Read-Host "   Would you like to add this directory to your PATH? (Y/n)"
                    if ($addToPath -eq "" -or $addToPath -eq "y" -or $addToPath -eq "Y") {
                        $ansibleDir = Split-Path -Parent $path
                        $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
                        if (-not $currentPath.Contains($ansibleDir)) {
                            [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$ansibleDir", "User")
                            Write-Host "   ✅ Added Ansible directory to PATH. You'll need to restart your terminal." -ForegroundColor Green
                        } else {
                            Write-Host "   ✅ Directory is already in PATH." -ForegroundColor Green
                        }
                    }
                    $pipAnsible = $true
                    break
                }
            }
            
            # If we didn't find Ansible but Python is installed, offer to install
            if (-not $pipAnsible) {
                Write-Host
                Write-Host "ℹ️ Python is installed, but Ansible wasn't found." -ForegroundColor Yellow
                Write-Host "   If you just installed Ansible with pip, you might need to restart your terminal." -ForegroundColor Yellow
                $installWithPip = Read-Host "   Would you like to (re)install Ansible with pip now? (y/N)"
                if ($installWithPip -eq "y" -or $installWithPip -eq "Y") {
                    Write-Host "   Installing Ansible with pip..." -ForegroundColor Cyan
                    python -m pip install ansible
                    Write-Host "   ✅ Ansible installation completed. Please restart your terminal." -ForegroundColor Green
                }
            }
        }
    } catch {}
    
    # Provide option to check Ansible installation in detail if not found via pip
    if (-not $pipAnsible -and (-not $wslAvailable -or -not ($LASTEXITCODE -eq 0))) {
        $checkAnsible = Read-Host "Would you like to see Ansible installation options? (y/N)"
        if ($checkAnsible -eq "y" -or $checkAnsible -eq "Y") {
            Test-AnsibleInstallation
        }
    }
}
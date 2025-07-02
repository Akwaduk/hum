# check-ansible.ps1
# Verifies and troubleshoots Ansible installation for hum

Write-Host "🔍 Ansible Installation Check for hum" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host

# Check if Ansible is directly in PATH
$ansibleFound = $false
try {
    $ansibleVersion = ansible --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Ansible is correctly installed and in PATH!" -ForegroundColor Green
        Write-Host "   $($ansibleVersion[0])" -ForegroundColor Green
        $ansibleFound = $true
    }
} catch {}

# Check for pip-installed Ansible
if (-not $ansibleFound) {
    Write-Host "Checking for pip-installed Ansible..." -ForegroundColor Cyan
    $pythonFound = $false
    $pipAnsible = $false

    # Check if Python is available
    try {
        $pythonVersion = python --version 2>&1
        $pythonPath = (Get-Command python -ErrorAction SilentlyContinue).Path
        if ($pythonPath) {
            $pythonFound = $true
            $pythonDir = Split-Path -Parent $pythonPath
            Write-Host "✅ Python found: $pythonVersion" -ForegroundColor Green
            Write-Host "   Python path: $pythonPath" -ForegroundColor Cyan
            
            # Look for Ansible in potential locations
            $possibleAnsiblePaths = @(
                "$pythonDir\Scripts\ansible.exe",
                "$pythonDir\ansible.exe",
                "$env:APPDATA\Python\Scripts\ansible.exe",
                "$env:LOCALAPPDATA\Programs\Python\Python*\Scripts\ansible.exe"
            )
            
            foreach ($path in $possibleAnsiblePaths) {
                foreach ($expandedPath in Resolve-Path -Path $path -ErrorAction SilentlyContinue) {
                    if (Test-Path -Path $expandedPath -PathType Leaf) {
                        Write-Host "✅ Found Ansible at: $expandedPath" -ForegroundColor Green
                        $pipAnsible = $true
                        
                        # Check if this path is in PATH
                        $ansibleDir = Split-Path -Parent $expandedPath
                        $currentPath = $env:PATH
                        if (-not $currentPath.Contains($ansibleDir)) {
                            Write-Host "⚠️ Ansible directory is NOT in your PATH!" -ForegroundColor Yellow
                            Write-Host "   This is why 'ansible' commands aren't working directly." -ForegroundColor Yellow
                            
                            # Offer to add to PATH
                            $addToPath = Read-Host "   Would you like to add this directory to your PATH? (Y/n)"
                            if ($addToPath -eq "" -or $addToPath -eq "y" -or $addToPath -eq "Y") {
                                [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$ansibleDir", "User")
                                Write-Host "   ✅ Added Ansible directory to PATH. You'll need to restart your terminal." -ForegroundColor Green
                            } else {
                                # Provide information on running ansible with full path
                                Write-Host "   To use Ansible, you can:" -ForegroundColor Cyan
                                Write-Host "   1. Use the full path: $expandedPath --version" -ForegroundColor Cyan
                                Write-Host "   2. Add the directory to PATH manually later" -ForegroundColor Cyan
                            }
                        } else {
                            Write-Host "⚠️ Ansible directory IS in your PATH but the command isn't working." -ForegroundColor Yellow
                            Write-Host "   This might be because you need to restart your terminal." -ForegroundColor Yellow
                        }
                        break
                    }
                }
                if ($pipAnsible) { break }
            }
            
            # If we couldn't find Ansible but Python exists
            if (-not $pipAnsible) {
                Write-Host "❌ Ansible not found in standard locations." -ForegroundColor Red
                Write-Host "   Checking if it's installed in pip packages..." -ForegroundColor Cyan
                
                $pipList = python -m pip list 2>&1
                $ansibleInPip = $pipList | Select-String -Pattern "ansible"
                
                if ($ansibleInPip) {
                    Write-Host "⚠️ Ansible is installed via pip but the executable wasn't found in expected locations." -ForegroundColor Yellow
                    $pipShow = python -m pip show ansible 2>&1
                    $location = ($pipShow | Select-String -Pattern "Location:").ToString()
                    if ($location) {
                        Write-Host "   Package location: $($location.Split(':')[1].Trim())" -ForegroundColor Cyan
                    }
                    Write-Host "   Try reinstalling: python -m pip install --force-reinstall ansible" -ForegroundColor Cyan
                } else {
                    Write-Host "❌ Ansible is not installed via pip." -ForegroundColor Red
                    $installAnsible = Read-Host "   Would you like to install Ansible with pip now? (Y/n)"
                    if ($installAnsible -eq "" -or $installAnsible -eq "y" -or $installAnsible -eq "Y") {
                        python -m pip install ansible
                        Write-Host "   Please restart your terminal and run this script again." -ForegroundColor Yellow
                    }
                }
            }
        }
    } catch {
        Write-Host "❌ Python not found or error occurred." -ForegroundColor Red
    }
}

# Check for WSL Ansible if not found natively
if (-not $ansibleFound -and -not $pipAnsible) {
    Write-Host "Checking for WSL-installed Ansible..." -ForegroundColor Cyan
    $wslAvailable = $false
    try {
        $wslCheck = wsl --list 2>&1
        if ($LASTEXITCODE -eq 0) {
            $wslAvailable = $true
            $wslAnsible = wsl ansible --version 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Ansible is installed in WSL: $($wslAnsible | Select-Object -First 1)" -ForegroundColor Green
                
                # Offer to create wrapper
                Write-Host "   You can use Ansible by prefixing commands with 'wsl', e.g., 'wsl ansible --version'" -ForegroundColor Cyan
                $createWrapper = Read-Host "   Would you like to create a wrapper script so 'ansible' commands work automatically through WSL? (Y/n)"
                if ($createWrapper -eq "" -or $createWrapper -eq "y" -or $createWrapper -eq "Y") {
                    # Create wrapper scripts
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
                    
                    # Create batch file
                    $batchPath = "$env:USERPROFILE\ansible.bat"
                    $batchContent = "@echo off`r`npowershell -ExecutionPolicy Bypass -File `"%USERPROFILE%\ansible.ps1`" %*"
                    Set-Content -Path $batchPath -Value $batchContent
                    
                    # Add to PATH if needed
                    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
                    $userProfile = $env:USERPROFILE.Replace("\", "\\")
                    if (-not ($currentPath -match $userProfile)) {
                        [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$env:USERPROFILE", "User")
                        Write-Host "   ✅ Added wrapper script to PATH. You'll need to restart your terminal." -ForegroundColor Green
                    } else {
                        Write-Host "   ✅ Created wrapper scripts. Your PATH already includes the location." -ForegroundColor Green
                    }
                    
                    Write-Host "   ✅ Created Ansible wrapper at $wrapperPath" -ForegroundColor Green
                    Write-Host "   ✅ Created Ansible batch file at $batchPath" -ForegroundColor Green
                    Write-Host "   Restart your terminal, then you can use 'ansible' commands directly!" -ForegroundColor Green
                }
            } else {
                Write-Host "❌ WSL is available but Ansible is not installed in it." -ForegroundColor Yellow
                $installWslAnsible = Read-Host "   Would you like to install Ansible in WSL now? (Y/n)"
                if ($installWslAnsible -eq "" -or $installWslAnsible -eq "y" -or $installWslAnsible -eq "Y") {
                    Write-Host "   Installing Ansible in WSL..." -ForegroundColor Cyan
                    wsl sudo apt update
                    wsl sudo apt install -y ansible
                    $wslAnsible = wsl ansible --version 2>&1
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "   ✅ Ansible installed in WSL successfully!" -ForegroundColor Green
                    } else {
                        Write-Host "   ❌ There was an error installing Ansible in WSL." -ForegroundColor Red
                    }
                }
            }
        } else {
            Write-Host "❌ WSL is not available on this system." -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error checking WSL: $_" -ForegroundColor Red
    }
}

if (-not $ansibleFound -and -not $pipAnsible -and -not $wslAvailable) {
    Write-Host "`nℹ️ Summary: Ansible is not installed on your system." -ForegroundColor Yellow
    Write-Host "   You have these options to install Ansible:" -ForegroundColor Cyan
    Write-Host "   1. Install via pip: python -m pip install ansible" -ForegroundColor Cyan
    Write-Host "   2. Install WSL and use Ansible there: wsl --install -d Ubuntu" -ForegroundColor Cyan
    Write-Host "   3. Configure remote Ansible with 'hum ansible-config'" -ForegroundColor Cyan
    Write-Host "`nRemember: Ansible is only needed for deployment features of hum." -ForegroundColor Cyan
} else {
    Write-Host "`nℹ️ For more information about using Ansible with hum, see INSTALLATION.md" -ForegroundColor Cyan
}

# Test if hum can find ansible
Write-Host "`nWould you like to test if 'hum' can find your Ansible installation? (Y/n)" -ForegroundColor Cyan
$testHum = Read-Host
if ($testHum -eq "" -or $testHum -eq "y" -or $testHum -eq "Y") {
    $humPath = Get-Command hum -ErrorAction SilentlyContinue
    if ($humPath) {
        Write-Host "Running 'hum doctor'..." -ForegroundColor Cyan
        hum doctor
    } else {
        Write-Host "❌ 'hum' command not found. Make sure it's installed correctly." -ForegroundColor Red
    }
}

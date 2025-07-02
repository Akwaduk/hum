# setup-ansible-wrapper.ps1
# Creates an Ansible wrapper script to automatically forward Ansible commands to WSL

Write-Host "🔨 hum Ansible WSL Wrapper Setup" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host

# Check if WSL is available
$wslAvailable = $false
try {
    $wslCheck = wsl --list 2>&1
    if ($LASTEXITCODE -eq 0) {
        $wslAvailable = $true
    }
} catch {}

if (-not $wslAvailable) {
    Write-Host "❌ WSL is not available on this system." -ForegroundColor Red
    Write-Host "Please install WSL first with 'wsl --install -d Ubuntu'" -ForegroundColor Yellow
    exit 1
}

# Check if Ansible is available in WSL
Write-Host "Checking for Ansible in WSL..." -ForegroundColor Cyan
$wslAnsible = wsl ansible --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Ansible is not installed in WSL." -ForegroundColor Red
    Write-Host "Would you like to install Ansible in WSL now?" -ForegroundColor Yellow
    $installAnsible = Read-Host "(y/N)"
    
    if ($installAnsible -eq "y" -or $installAnsible -eq "Y") {
        Write-Host "Installing Ansible in WSL..." -ForegroundColor Cyan
        wsl sudo apt update
        wsl sudo apt install -y ansible
        
        # Verify installation
        $wslAnsible = wsl ansible --version 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to install Ansible in WSL." -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "Please install Ansible in WSL with 'wsl sudo apt update; sudo apt install -y ansible'" -ForegroundColor Yellow
        exit 1
    }
}

# Create the wrapper scripts
Write-Host "Creating Ansible wrapper scripts..." -ForegroundColor Cyan

# Create PowerShell wrapper
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

# Create batch wrapper
$batchPath = "$env:USERPROFILE\ansible.bat"
$batchContent = "@echo off`r`npowershell -ExecutionPolicy Bypass -File `"%USERPROFILE%\ansible.ps1`" %*"
Set-Content -Path $batchPath -Value $batchContent

# Add to PATH if needed
$currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
$modified = $false

if (-not $currentPath.Contains($env:USERPROFILE)) {
    [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$env:USERPROFILE", "User")
    $modified = $true
}

# Success message
Write-Host "✅ Ansible wrapper setup complete!" -ForegroundColor Green
Write-Host "   PowerShell wrapper: $wrapperPath" -ForegroundColor Green
Write-Host "   Batch file wrapper: $batchPath" -ForegroundColor Green

if ($modified) {
    Write-Host "`nℹ️ Your PATH environment variable has been updated." -ForegroundColor Yellow
    Write-Host "   You need to restart your terminal for the 'ansible' command to work." -ForegroundColor Yellow
} else {
    Write-Host "`n🎉 The 'ansible' command should now work in your terminal!" -ForegroundColor Cyan
}

# Test the wrapper
Write-Host "`nWould you like to test the wrapper now? (y/N)" -ForegroundColor Cyan
$testWrapper = Read-Host
if ($testWrapper -eq "y" -or $testWrapper -eq "Y") {
    Write-Host "`nRunning 'ansible --version':" -ForegroundColor Cyan
    & "$env:USERPROFILE\ansible.ps1" --version
}

Write-Host "`nNow you can use 'ansible' commands directly in PowerShell and they'll be forwarded to WSL." -ForegroundColor Cyan

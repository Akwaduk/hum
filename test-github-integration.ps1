# PowerShell script to test hum GitHub integration

Write-Host "Testing hum GitHub integration..." -ForegroundColor Cyan

# Check prereqs
Write-Host "Checking prerequisites..." -ForegroundColor Cyan
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "GitHub CLI (gh) not found! Please install it first." -ForegroundColor Red
    exit 1
}

try {
    $ghStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Not authenticated with GitHub. Please run 'gh auth login' first." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error checking GitHub authentication status. Please run 'gh auth login'." -ForegroundColor Red
    exit 1
}

# Generate a unique test repo name with timestamp
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$testRepoName = "hum-test-$timestamp"

# Create a temporary project
Write-Host "Creating test project..." -ForegroundColor Cyan
$tempDir = Join-Path $env:TEMP $testRepoName
New-Item -ItemType Directory -Path $tempDir -Force
Set-Location $tempDir

# Create a test file
"# Test Repository" | Out-File -FilePath README.md
"Created for testing hum GitHub integration on $timestamp" | Add-Content -Path README.md

# Initialize git
git init
git add README.md
git commit -m "Initial commit"

# Test hum create with GitHub integration
Write-Host "Testing hum create with GitHub integration..." -ForegroundColor Cyan
Set-Location $env:TEMP
$humProjectPath = "C:\src\hum"  # Update this path if needed
dotnet run --project $humProjectPath create $testRepoName --template dotnet-minimal --dir $tempDir

# Check results
Write-Host "Testing if repository was created on GitHub..." -ForegroundColor Cyan
try {
    $repoCheck = gh repo view $testRepoName 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Success! Repository was created on GitHub" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed! Repository was not created on GitHub" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Failed! Repository was not created on GitHub" -ForegroundColor Red
    exit 1
}

# Cleanup
Write-Host "Cleaning up test repository..." -ForegroundColor Cyan
gh repo delete $testRepoName --yes

Write-Host "Test completed successfully!" -ForegroundColor Green

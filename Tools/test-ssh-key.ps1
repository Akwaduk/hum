# SSH Key Format Test Script
param(
    [Parameter(Mandatory=$true)]
    [string]$PrivateKeyPath
)

Write-Host "Testing SSH key format compatibility" -ForegroundColor Cyan
Write-Host "Key path: $PrivateKeyPath"

# Check if file exists
if (-not (Test-Path $PrivateKeyPath)) {
    Write-Host "Error: File not found at $PrivateKeyPath" -ForegroundColor Red
    exit 1
}

# Read the key file
$keyContent = Get-Content $PrivateKeyPath -Raw

# Check for key format
$format = "Unknown"
if ($keyContent -match "-----BEGIN RSA PRIVATE KEY-----") {
    $format = "PEM (PKCS#1 RSA)"
    Write-Host "Key format: $format - Compatible with SSH.NET" -ForegroundColor Green
}
elseif ($keyContent -match "-----BEGIN PRIVATE KEY-----") {
    $format = "PEM (PKCS#8 unencrypted)"
    Write-Host "Key format: $format - Compatible with SSH.NET" -ForegroundColor Green
}
elseif ($keyContent -match "-----BEGIN ENCRYPTED PRIVATE KEY-----") {
    $format = "PEM (PKCS#8 encrypted)"
    Write-Host "Key format: $format - Should be compatible with SSH.NET if correct passphrase is provided" -ForegroundColor Yellow
}
elseif ($keyContent -match "-----BEGIN OPENSSH PRIVATE KEY-----") {
    $format = "OpenSSH native format"
    Write-Host "Key format: $format - NOT compatible with SSH.NET" -ForegroundColor Red
    Write-Host "Convert key with: ssh-keygen -p -m PEM -f $PrivateKeyPath" -ForegroundColor Cyan
}
else {
    Write-Host "Key format: $format - Unknown format, might not be compatible with SSH.NET" -ForegroundColor Red
}

# Check line endings
if ($keyContent -match "`r`n") {
    Write-Host "Line endings: CRLF (Windows style)" -ForegroundColor Yellow
    Write-Host "Some SSH implementations may prefer LF line endings" -ForegroundColor Yellow
}
else {
    Write-Host "Line endings: LF (Unix style)" -ForegroundColor Green
}

Write-Host "`nSample SSH.NET code to test this key:" -ForegroundColor Cyan
Write-Host @"
try {
    var keyFile = new PrivateKeyFile("$PrivateKeyPath");
    // If encrypted, use: var keyFile = new PrivateKeyFile("$PrivateKeyPath", "your-passphrase");
    
    var connectionInfo = new ConnectionInfo("your-host", "your-username", 
        new PrivateKeyAuthenticationMethod("your-username", keyFile));
        
    using (var client = new SshClient(connectionInfo))
    {
        client.Connect();
        if (client.IsConnected) {
            Console.WriteLine("Connection successful!");
            client.Disconnect();
        }
    }
}
catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
"@

Write-Host "`nFor more debugging, try this command to get info about the key:" -ForegroundColor Cyan
Write-Host "ssh-keygen -l -f $PrivateKeyPath"

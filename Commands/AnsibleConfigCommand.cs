using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using hum.Models;
using hum.Services;
using Renci.SshNet;

namespace hum.Commands
{
    public class AnsibleConfigCommand : Command
    {
        public AnsibleConfigCommand() : base("ansible-config", "Interactively configure and validate Ansible orchestrator settings")
        {
            this.SetHandler(HandleCommand);
        }

        private async Task HandleCommand(InvocationContext context)
        {
            Console.WriteLine("🤖 Configuring Ansible Orchestrator Connection");
            Console.WriteLine();

            var configService = new ConfigurationService();
            var settings = await configService.LoadSettingsAsync();
            var ansibleConfig = settings.AnsibleConfig ?? new AnsibleConfig();

            // Interactively get settings
            ansibleConfig.Host = PromptForInput("Enter the remote host (e.g., your-ansible-server.example.com):", ansibleConfig.Host);
            ansibleConfig.User = PromptForInput("Enter the SSH user:", ansibleConfig.User);

            if (Confirm("Do you want to generate a new SSH key? (y/n)"))
            {
                try
                {
                    await GenerateAndDeploySshKey(ansibleConfig);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Key generation failed: {ex.Message}");
                    Console.WriteLine("Falling back to manual key entry...\n");
                    ansibleConfig.PrivateKeyPath = PromptForPrivateKey("Enter the absolute path to your SSH private key:", ansibleConfig.PrivateKeyPath);
                }
            }
            else
            {
                ansibleConfig.PrivateKeyPath = PromptForPrivateKey("Enter the absolute path to your SSH private key:", ansibleConfig.PrivateKeyPath);
            }

            // Validate connection
            if (await TestSshConnection(ansibleConfig))
            {
                settings.AnsibleConfig = ansibleConfig;
                await configService.SaveSettingsAsync(settings);
                Console.WriteLine("\n✅ Ansible configuration saved and validated successfully!");
            }
            else
            {
                Console.WriteLine("\n❌ Failed to validate Ansible configuration. Settings were not saved.");
            }
        }

        private string PromptForInput(string prompt, string? defaultValue)
        {
            Console.WriteLine(prompt);
            if (!string.IsNullOrEmpty(defaultValue))
            {
                Console.Write($"(current: {defaultValue}): ");
            }
            string? input = Console.ReadLine();
            return !string.IsNullOrEmpty(input) ? input : defaultValue ?? "";
        }

        private string PromptForPrivateKey(string prompt, string? defaultValue)
        {            while (true)
            {
                string keyPath = PromptForInput(prompt, defaultValue);
                if (File.Exists(keyPath))
                {
                    return keyPath;
                }
                else
                {
                    Console.WriteLine("   ❌ File not found. Please enter a valid path.");
                    defaultValue = null; // Clear default value after first failure
                }
            }
        }

        private async Task<bool> TestSshConnection(AnsibleConfig config, string? passphrase = null)
        {
            if (string.IsNullOrEmpty(config.Host) || string.IsNullOrEmpty(config.User) || string.IsNullOrEmpty(config.PrivateKeyPath))
            {
                Console.WriteLine("\n   ❌ Host, user, and private key path are all required.");
                return false;
            }

            Console.WriteLine($"\nAttempting to connect to {config.User}@{config.Host}...");

            try
            {
                // First check if the key file exists and is readable
                if (!File.Exists(config.PrivateKeyPath))
                {
                    Console.WriteLine($"   ❌ Key file not found: {config.PrivateKeyPath}");
                    return false;
                }
                
                // Check the key format
                var keyContent = await File.ReadAllTextAsync(config.PrivateKeyPath);
                var keyFormat = "unknown";
                
                if (keyContent.Contains("-----BEGIN RSA PRIVATE KEY-----"))
                    keyFormat = "PEM (PKCS#1)";
                else if (keyContent.Contains("-----BEGIN PRIVATE KEY-----"))
                    keyFormat = "PEM (PKCS#8 unencrypted)";
                else if (keyContent.Contains("-----BEGIN ENCRYPTED PRIVATE KEY-----"))
                    keyFormat = "PEM (PKCS#8 encrypted)";
                else if (keyContent.Contains("-----BEGIN OPENSSH PRIVATE KEY-----"))
                    keyFormat = "OpenSSH native (not compatible with SSH.NET)";
                
                Console.WriteLine($"   📄 Detected key format: {keyFormat}");
                
                // Convert OpenSSH format if needed
                if (keyFormat == "OpenSSH native (not compatible with SSH.NET)")
                {
                    Console.WriteLine("   ⚠️ Converting key to PEM format for compatibility...");
                    var tempPath = Path.Combine(Path.GetDirectoryName(config.PrivateKeyPath)!, "temp_key");
                    File.Copy(config.PrivateKeyPath, tempPath, true);
                    
                    using (var convertProcess = new System.Diagnostics.Process())
                    {
                        convertProcess.StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ssh-keygen",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        
                        convertProcess.StartInfo.ArgumentList.Add("-p");
                        convertProcess.StartInfo.ArgumentList.Add("-f");
                        convertProcess.StartInfo.ArgumentList.Add(tempPath);
                        convertProcess.StartInfo.ArgumentList.Add("-m");
                        convertProcess.StartInfo.ArgumentList.Add("PEM");
                        convertProcess.StartInfo.ArgumentList.Add("-P");
                        convertProcess.StartInfo.ArgumentList.Add(passphrase ?? "");
                        convertProcess.StartInfo.ArgumentList.Add("-N");
                        convertProcess.StartInfo.ArgumentList.Add(passphrase ?? "");
                        
                        convertProcess.Start();
                        await convertProcess.WaitForExitAsync();
                        
                        if (convertProcess.ExitCode == 0)
                        {
                            File.Copy(tempPath, config.PrivateKeyPath, true);
                            Console.WriteLine("   ✅ Key converted successfully");
                        }
                        else
                        {
                            var error = await convertProcess.StandardError.ReadToEndAsync();
                            Console.WriteLine($"   ❌ Failed to convert key: {error}");
                        }
                        
                        try { File.Delete(tempPath); } catch { }
                    }
                }
                
                // Now try the SSH connection
                Console.WriteLine("   🔑 Loading SSH key...");
                PrivateKeyFile keyFile;
                
                // Before attempting to load the key, check if it appears to be encrypted
                bool keyIsEncrypted = keyFormat.Contains("encrypted") || 
                                     keyContent.Contains("ENCRYPTED") || 
                                     keyContent.Contains("Proc-Type: 4,ENCRYPTED");
                
                Console.WriteLine($"   Key appears to be {(keyIsEncrypted ? "encrypted" : "unencrypted")}");
                
                // Always prompt for passphrase if the key looks encrypted and no passphrase was provided
                if (keyIsEncrypted && string.IsNullOrEmpty(passphrase))
                {
                    Console.WriteLine("   🔑 This key requires a passphrase.");
                    Console.Write("   Please enter the passphrase: ");
                    passphrase = ReadPassword();
                    Console.WriteLine();
                    
                    if (string.IsNullOrEmpty(passphrase))
                    {
                        Console.WriteLine("   ❌ Empty passphrase provided for an encrypted key. Cannot proceed.");
                        return false;
                    }
                }
                
                try
                {
                    Console.WriteLine($"   Loading key with{(string.IsNullOrEmpty(passphrase) ? "out" : "")} passphrase...");
                    
                    keyFile = string.IsNullOrEmpty(passphrase)
                        ? new PrivateKeyFile(config.PrivateKeyPath)
                        : new PrivateKeyFile(config.PrivateKeyPath, passphrase);
                    
                    Console.WriteLine("   ✅ Key loaded successfully");
                }
                catch (Org.BouncyCastle.Crypto.InvalidCipherTextException ex)
                {
                    Console.WriteLine($"   ❌ Invalid passphrase or corrupt key file: {ex.Message}");
                    Console.Write("   Try again with a different passphrase? (y/n): ");
                    var retry = Console.ReadLine()?.ToLower() == "y";
                    if (retry)
                    {
                        Console.Write("   Enter passphrase: ");
                        string? newPassphrase = ReadPassword();
                        Console.WriteLine();
                        return await TestSshConnection(config, newPassphrase);
                    }
                    return false;
                }
                catch (Renci.SshNet.Common.SshPassPhraseNullOrEmptyException)
                {
                    Console.WriteLine("   ❌ The key requires a passphrase but none was provided.");
                    Console.Write("   Please enter a passphrase for this key: ");
                    string? newPassphrase = ReadPassword();
                    Console.WriteLine();
                    
                    if (string.IsNullOrEmpty(newPassphrase))
                    {
                        Console.WriteLine("   ❌ Empty passphrase provided. Cannot proceed with an encrypted key.");
                        return false;
                    }
                    
                    return await TestSshConnection(config, newPassphrase);
                }

                var connectionInfo = new ConnectionInfo(config.Host, config.User, new PrivateKeyAuthenticationMethod(config.User, keyFile));
                
                Console.WriteLine("   🌐 Connecting...");
                
                // Create a separate connection info with explicit settings
                var customConnectionInfo = new ConnectionInfo(
                    config.Host,
                    config.User,
                    new PrivateKeyAuthenticationMethod(config.User, keyFile)
                );
                
                // Set explicit timeouts and options
                customConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
                customConnectionInfo.Encoding = System.Text.Encoding.UTF8;
                
                using (var client = new SshClient(customConnectionInfo))
                {
                    try
                    {
                        // Connect with a cancellation token
                        using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30)))
                        {
                            await Task.Run(() => {
                                try {
                                    client.Connect();
                                }
                                catch (Exception ex) when (!(ex is OperationCanceledException)) {
                                    Console.WriteLine($"   Connection error: {ex.Message}");
                                    throw;
                                }
                            }, cts.Token);
                        }
                        
                        if (client.IsConnected)
                        {
                            Console.WriteLine("   ✅ SSH connection successful!");
                            
                            // Just get the server info, don't try to run commands
                            try
                            {
                                Console.WriteLine($"   Server: {client.ConnectionInfo.ServerVersion}");
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("   (Unable to retrieve server version)");
                            }
                            
                            client.Disconnect();
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("   ❌ SSH connection failed for an unknown reason.");
                            return false;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("   ❌ Connection timed out after 30 seconds.");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ Connection error: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"   Details: {ex.InnerException.Message}");
                        }
                        return false;
                    }
                }
            }
            catch (Renci.SshNet.Common.SshAuthenticationException ex) when (ex.Message.Contains("private key is encrypted"))
            {
                Console.WriteLine("   🔑 Your SSH private key is encrypted.");
                Console.Write("   Please enter your passphrase: ");
                string? newPassphrase = ReadPassword();
                Console.WriteLine(); 
                return await TestSshConnection(config, newPassphrase);
            }
            catch (Renci.SshNet.Common.SshPassPhraseNullOrEmptyException)
            {
                // If we've already tried with a passphrase and still got this exception, there's something wrong
                if (!string.IsNullOrEmpty(passphrase))
                {
                    Console.WriteLine("   ❌ Key requires a passphrase, but the provided passphrase was rejected.");
                    Console.WriteLine("   This could mean the key format is incompatible with SSH.NET.");
                    return false;
                }
                
                Console.WriteLine("   🔑 Your SSH private key is encrypted and requires a passphrase.");
                Console.Write("   Please enter your passphrase: ");
                string? newPassphrase = ReadPassword();
                Console.WriteLine(); 
                
                if (string.IsNullOrEmpty(newPassphrase))
                {
                    Console.WriteLine("   ❌ Passphrase cannot be empty for this key.");
                    return false;
                }
                
                // Try again with the new passphrase
                return await TestSshConnection(config, newPassphrase);
            }
            catch (Org.BouncyCastle.Crypto.InvalidCipherTextException ex)
            {
                Console.WriteLine($"   ❌ Error decrypting key: {ex.Message}");
                Console.WriteLine("   This usually means the passphrase is incorrect or the key format is incompatible.");
                Console.Write("   Try again with a different passphrase? (y/n): ");
                var retry = Console.ReadLine()?.ToLower() == "y";
                if (retry)
                {
                    Console.Write("   Enter passphrase: ");
                    string? newPassphrase = ReadPassword();
                    Console.WriteLine();
                    return await TestSshConnection(config, newPassphrase);
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ SSH connection failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        private async Task GenerateAndDeploySshKey(AnsibleConfig config)
        {
            try
            {
                Console.WriteLine("\n🔧 Generating a new SSH key pair...");
                var (privateKeyPath, publicKeyPath) = await GenerateSshKeyPair();
                config.PrivateKeyPath = privateKeyPath;

                Console.WriteLine("\n🚀 Deploying the public key to the remote server...");
                await DeployPublicKeyWithPassword(config, publicKeyPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Failed to generate or deploy SSH key: {ex.Message}");
                throw; // Re-throw to let the caller handle it
            }
        }

        private async Task<(string privateKeyPath, string publicKeyPath)> GenerateSshKeyPair()
        {
            var configDir = new ConfigurationService().ConfigDirectory;
            var keyDir = Path.Combine(configDir, "keys");
            if (!Directory.Exists(keyDir)) { Directory.CreateDirectory(keyDir); }

            var privateKeyPath = Path.Combine(keyDir, "hum_ansible_id_rsa");
            var publicKeyPath = privateKeyPath + ".pub";

            Console.Write("Enter a passphrase for the new key (or leave blank for none): ");
            var passphrase = ReadPassword();
            Console.WriteLine();
            
            // Verify the passphrase if it's provided
            if (!string.IsNullOrEmpty(passphrase))
            {
                Console.Write("Confirm passphrase: ");
                var confirmPassphrase = ReadPassword();
                Console.WriteLine();
                
                if (passphrase != confirmPassphrase)
                {
                    throw new Exception("Passphrases do not match. Please try again.");
                }
            }

            try
            {
                // Generate keys using ssh-keygen for maximum compatibility
                Console.WriteLine("   Generating key using ssh-keygen...");
                
                // Delete any existing key files to avoid conflicts
                if (File.Exists(privateKeyPath)) File.Delete(privateKeyPath);
                if (File.Exists(publicKeyPath)) File.Delete(publicKeyPath);

                // First create a key without a passphrase
                using (var genProcess = new System.Diagnostics.Process())
                {
                    genProcess.StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ssh-keygen",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    // Add arguments individually for security and clarity
                    genProcess.StartInfo.ArgumentList.Add("-t");
                    genProcess.StartInfo.ArgumentList.Add("rsa");
                    genProcess.StartInfo.ArgumentList.Add("-b");
                    genProcess.StartInfo.ArgumentList.Add("4096");
                    genProcess.StartInfo.ArgumentList.Add("-f");
                    genProcess.StartInfo.ArgumentList.Add(privateKeyPath);
                    genProcess.StartInfo.ArgumentList.Add("-N");  // No passphrase initially
                    genProcess.StartInfo.ArgumentList.Add("");
                    genProcess.StartInfo.ArgumentList.Add("-m");  // Force PEM format
                    genProcess.StartInfo.ArgumentList.Add("PEM");
                    genProcess.StartInfo.ArgumentList.Add("-C");
                    genProcess.StartInfo.ArgumentList.Add("hum-cli-generated-key");
                    
                    try
                    {
                        genProcess.Start();
                        await genProcess.WaitForExitAsync();
                        
                        var output = await genProcess.StandardOutput.ReadToEndAsync();
                        var error = await genProcess.StandardError.ReadToEndAsync();
                        
                        if (genProcess.ExitCode != 0)
                        {
                            throw new Exception($"ssh-keygen error (exit code {genProcess.ExitCode}): {error}");
                        }
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        throw new Exception($"Failed to run ssh-keygen. Make sure OpenSSH is installed and in your PATH. Error: {ex.Message}");
                    }
                }

                // If a passphrase was provided, encrypt the key
                if (!string.IsNullOrEmpty(passphrase))
                {
                    Console.WriteLine("   Adding passphrase to key...");
                    using (var encryptProcess = new System.Diagnostics.Process())
                    {
                        encryptProcess.StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ssh-keygen",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        
                        encryptProcess.StartInfo.ArgumentList.Add("-p"); // Change passphrase
                        encryptProcess.StartInfo.ArgumentList.Add("-f");
                        encryptProcess.StartInfo.ArgumentList.Add(privateKeyPath);
                        encryptProcess.StartInfo.ArgumentList.Add("-N"); // New passphrase
                        encryptProcess.StartInfo.ArgumentList.Add(passphrase);
                        encryptProcess.StartInfo.ArgumentList.Add("-P"); // Old passphrase (empty)
                        encryptProcess.StartInfo.ArgumentList.Add("");
                        encryptProcess.StartInfo.ArgumentList.Add("-m"); // Force PEM format
                        encryptProcess.StartInfo.ArgumentList.Add("PEM");
                        
                        try
                        {
                            encryptProcess.Start();
                            await encryptProcess.WaitForExitAsync();
                            
                            var output = await encryptProcess.StandardOutput.ReadToEndAsync();
                            var error = await encryptProcess.StandardError.ReadToEndAsync();
                            
                            if (encryptProcess.ExitCode != 0)
                            {
                                throw new Exception($"Failed to encrypt key: {error}");
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error encrypting key: {ex.Message}");
                        }
                    }
                }
                
                // Validate the generated files
                if (!File.Exists(privateKeyPath))
                {
                    throw new Exception("Private key file was not created");
                }
                
                if (!File.Exists(publicKeyPath))
                {
                    throw new Exception("Public key file was not created");
                }
                
                // Test the format with our PowerShell helper
                try
                {
                    using (var testProcess = new System.Diagnostics.Process())
                    {
                        testProcess.StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        
                        testProcess.StartInfo.ArgumentList.Add("-File");
                        testProcess.StartInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Tools", "test-ssh-key.ps1"));
                        testProcess.StartInfo.ArgumentList.Add(privateKeyPath);
                        
                        testProcess.Start();
                        await testProcess.WaitForExitAsync();
                    }
                }
                catch 
                {
                    // Ignore any errors with the test script
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate SSH key pair: {ex.Message}", ex);
            }

            Console.WriteLine($"   ✅ New key pair created:");
            Console.WriteLine($"      Private key: {privateKeyPath}");
            Console.WriteLine($"      Public key: {publicKeyPath}");

            return (privateKeyPath, publicKeyPath);
        }

        private async Task DeployPublicKeyWithPassword(AnsibleConfig config, string publicKeyPath)
        {
            Console.Write($"   Enter the password for {config.User}@{config.Host} to deploy the key: ");
            var password = ReadPassword();
            Console.WriteLine();

            try
            {
                var connectionInfo = new ConnectionInfo(config.Host, config.User, new PasswordAuthenticationMethod(config.User, password));
                using (var client = new SshClient(connectionInfo))
                {
                    await Task.Run(() => client.Connect());
                    Console.WriteLine("   ✅ Connected with password.");

                    var publicKeyText = await File.ReadAllTextAsync(publicKeyPath);
                    client.RunCommand("mkdir -p ~/.ssh");
                    client.RunCommand("chmod 700 ~/.ssh");
                    client.RunCommand($"echo \"{publicKeyText}\" >> ~/.ssh/authorized_keys");
                    client.RunCommand("chmod 600 ~/.ssh/authorized_keys");

                    Console.WriteLine("   ✅ Public key deployed successfully.");
                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to deploy public key: {ex.Message}");
            }
        }

        private string ReadPassword()
        {
            var pass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            return pass;
        }

        private bool Confirm(string prompt)
        {
            Console.Write($"{prompt} ");
            var response = Console.ReadLine()?.ToLower();
            return response == "y" || response == "yes";
        }

        private string ExportRsaPrivateKeyAsPem(System.Security.Cryptography.RSA rsa)
        {
            var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
            var base64PrivateKey = Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN PRIVATE KEY-----\n{base64PrivateKey}\n-----END PRIVATE KEY-----";
        }

        private string ConvertRsaParametersToOpenSshPublicKey(System.Security.Cryptography.RSAParameters rsaParams)
        {
            // OpenSSH public key format is: ssh-rsa <base64-encoded-public-key> comment
            // where <base64-encoded-public-key> is a base64 encoding of:
            // 4 bytes: "ssh-rsa" as a length-prefixed string
            // e: RSA exponent as a length-prefixed integer
            // n: RSA modulus as a length-prefixed integer

            using var ms = new MemoryStream();
            
            // Add "ssh-rsa" identifier
            var sshRsa = System.Text.Encoding.ASCII.GetBytes("ssh-rsa");
            WriteIntBigEndian(ms, sshRsa.Length);
            ms.Write(sshRsa, 0, sshRsa.Length);
            
            // Add the exponent
            if (rsaParams.Exponent == null)
                throw new InvalidOperationException("RSA exponent is null");
                
            WriteIntBigEndian(ms, rsaParams.Exponent.Length);
            ms.Write(rsaParams.Exponent, 0, rsaParams.Exponent.Length);
            
            // Add the modulus
            if (rsaParams.Modulus == null)
                throw new InvalidOperationException("RSA modulus is null");
                
            // The modulus needs a leading 0x00 byte if the highest bit is set to avoid 
            // interpretation as a negative number
            byte[] modulus = rsaParams.Modulus;
            if ((modulus.Length > 0) && ((modulus[0] & 0x80) == 0x80))
            {
                var tempModulus = new byte[modulus.Length + 1];
                tempModulus[0] = 0x00;
                Array.Copy(modulus, 0, tempModulus, 1, modulus.Length);
                modulus = tempModulus;
            }

            WriteIntBigEndian(ms, modulus.Length);
            ms.Write(modulus, 0, modulus.Length);
            
            return Convert.ToBase64String(ms.ToArray());
        }

        private void WriteIntBigEndian(Stream stream, int value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}

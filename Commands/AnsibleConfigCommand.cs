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
                var keyFile = passphrase == null
                    ? new PrivateKeyFile(config.PrivateKeyPath)
                    : new PrivateKeyFile(config.PrivateKeyPath, passphrase);

                var connectionInfo = new ConnectionInfo(config.Host, config.User, new PrivateKeyAuthenticationMethod(config.User, keyFile));

                using (var client = new SshClient(connectionInfo))
                {
                    await Task.Run(() => client.Connect());
                    if (client.IsConnected)
                    {
                        Console.WriteLine("   ✅ SSH connection successful!");
                        client.Disconnect();
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("   ❌ SSH connection failed for an unknown reason.");
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
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ SSH connection failed: {ex.Message}");
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

            try
            {
                // Create an RSA key pair directly using .NET
                using var rsa = System.Security.Cryptography.RSA.Create(4096);
                var privateKey = string.Empty;
                var publicKey = string.Empty;

                // Generate keys in PEM format which SSH.NET understands
                try
                {
                    string privateKey;
                    
                    if (string.IsNullOrEmpty(passphrase))
                    {
                        privateKey = ExportRsaPrivateKeyAsPem(rsa);
                    }
                    else
                    {
                        var privateKeyBytes = rsa.ExportEncryptedPkcs8PrivateKey(
                            passphrase,
                            new System.Security.Cryptography.PbeParameters(
                                System.Security.Cryptography.PbeEncryptionAlgorithm.Aes256Cbc,
                                System.Security.Cryptography.HashAlgorithmName.SHA256,
                                10000));
                        
                        privateKey = "-----BEGIN ENCRYPTED PRIVATE KEY-----\n" +
                                    Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                                    "\n-----END ENCRYPTED PRIVATE KEY-----";
                    }

                    // Export public key in OpenSSH format
                    var rsaParams = rsa.ExportParameters(false);
                    var publicKey = $"ssh-rsa {ConvertRsaParametersToOpenSshPublicKey(rsaParams)} hum-cli-generated-key";

                    // Save the keys to files with UTF-8 encoding without BOM
                    await File.WriteAllTextAsync(privateKeyPath, privateKey, new System.Text.UTF8Encoding(false));
                    await File.WriteAllTextAsync(publicKeyPath, publicKey, new System.Text.UTF8Encoding(false));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error creating key files: {ex.Message}", ex);
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

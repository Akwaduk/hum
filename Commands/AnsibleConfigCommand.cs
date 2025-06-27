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
            ansibleConfig.PrivateKeyPath = PromptForPrivateKey("Enter the absolute path to your SSH private key:", ansibleConfig.PrivateKeyPath);

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

        private async Task<bool> TestSshConnection(AnsibleConfig config)
        {
            if (string.IsNullOrEmpty(config.Host) || string.IsNullOrEmpty(config.User) || string.IsNullOrEmpty(config.PrivateKeyPath))
            {
                Console.WriteLine("\n   ❌ Host, user, and private key path are all required.");
                return false;
            }

            Console.WriteLine($"\nAttempting to connect to {config.User}@{config.Host}...");

            try
            {
                var keyFile = new PrivateKeyFile(config.PrivateKeyPath);
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
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ SSH connection failed: {ex.Message}");
                return false;
            }
        }
    }
}

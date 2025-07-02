using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using hum.Services;

namespace hum.Commands
{
    public class InventoryCommand : Command
    {
        private readonly ConfigurationService _configService;

        public InventoryCommand(ConfigurationService? configService = null) : base("inventory", "List the current inventory and validate Ansible configuration")
        {
            _configService = configService ?? new ConfigurationService();
            this.SetHandler(HandleCommand);
        }

        private async Task HandleCommand(InvocationContext context)
        {
            Console.WriteLine("🔍 Checking Ansible configuration and listing inventory...");
            Console.WriteLine();

            bool ansibleOk = await CheckAnsibleOrRemoteConfig();

            if (ansibleOk)
            {
                await ListInventory();
            }
            else
            {
                Console.WriteLine("❌ Cannot list inventory because Ansible is not configured correctly.");
            }
        }

        private async Task<bool> CheckAnsibleOrRemoteConfig()
        {
            Console.Write("Checking Ansible... ");

            // First, check if remote Ansible config is available
            var settings = await _configService.LoadSettingsAsync();
            var config = settings.AnsibleConfig;
            if (!string.IsNullOrEmpty(config?.Host))
            {
                Console.WriteLine($"✅ Using remote Ansible at {config.Host}");
                return true;
            }

            // Second, try native Ansible
            if (await CheckNativeAnsible())
            {
                return true;
            }

            // Third, try WSL Ansible
            if (await CheckWslAnsible())
            {
                return true;
            }

            // Nothing worked, show error message
            Console.WriteLine("❌ No Ansible installation found");
            Console.WriteLine("   Ansible is required for managing inventories, but you have options:");
            Console.WriteLine();
            Console.WriteLine("   📋 Option 1: Set up a remote Ansible server (Recommended for Windows)");
            Console.WriteLine("      Run 'hum ansible-config' to configure a remote server connection");
            Console.WriteLine();
            Console.WriteLine("   📋 Option 2: Use WSL (Windows Subsystem for Linux)");
            Console.WriteLine("      Run 'wsl --install -d Ubuntu' then 'wsl sudo apt install -y ansible'");
            Console.WriteLine();
            Console.WriteLine("   📋 Option 3: Install Ansible on another machine");
            Console.WriteLine("      Then use Option 1 to connect to it remotely");
            Console.WriteLine();
            Console.WriteLine("   ℹ️ Note: Direct Windows installation via pip is not recommended due to compatibility issues");
            return false;
        }

        private async Task<bool> CheckNativeAnsible()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ansible",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    string version = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    Console.WriteLine($"✅ {version}");
                    return true;
                }
            }
            catch (Exception)
            {
                // Silently fail and try other methods
            }
            return false;
        }

        private async Task<bool> CheckWslAnsible()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wsl",
                        Arguments = "ansible --version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    string version = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    Console.WriteLine($"✅ {version} (via WSL)");
                    return true;
                }
            }
            catch (Exception)
            {
                // Silently fail and try other methods
            }
            return false;
        }

        private async Task ListInventory()
        {
            Console.WriteLine();
            Console.WriteLine("📜 Listing Ansible inventory...");
            Console.WriteLine();

            // First try with remote config if available
            var settings = await _configService.LoadSettingsAsync();
            var config = settings.AnsibleConfig;
            if (!string.IsNullOrEmpty(config?.Host))
            {
                await ListInventoryViaRemote(config);
                return;
            }

            // Try native ansible-inventory
            if (await TryListInventoryWithNative())
            {
                return;
            }

            // Try WSL ansible-inventory
            if (await TryListInventoryWithWsl())
            {
                return;
            }

            // If we got here, all methods failed
            Console.WriteLine("❌ Failed to list inventory. No method available.");
        }

        private async Task ListInventoryViaRemote(Models.AnsibleConfig config)
        {
            try
            {
                Console.WriteLine($"Connecting to remote Ansible server {config.Host}...");
                
                var keyPath = config.PrivateKeyPath;
                if (string.IsNullOrEmpty(keyPath))
                {
                    keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "id_rsa");
                }

                if (!File.Exists(keyPath))
                {
                    Console.WriteLine($"❌ SSH key not found at {keyPath}");
                    Console.WriteLine("   Please configure a valid SSH key path with 'hum ansible-config'");
                    return;
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ssh",
                        // Add connection timeout flags
                        Arguments = $"-o ConnectTimeout=5 -o BatchMode=yes -i \"{keyPath}\" {config.User}@{config.Host} \"ansible-inventory --list -y\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                var timeout = Task.Delay(10000); // 10 second timeout
                
                process.Start();
                
                // Create tasks for reading output/error and waiting for exit
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var exitTask = process.WaitForExitAsync();
                
                // Wait for process to complete or timeout
                if (await Task.WhenAny(exitTask, timeout) == timeout)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            Console.WriteLine("❌ Connection to remote server timed out.");
                            Console.WriteLine("   Check that the server is reachable and your SSH key is authorized.");
                            return;
                        }
                    }
                    catch { /* Ignore errors during kill */ }
                }
                
                // Get output now that process has completed or been killed
                string output = await outputTask;
                string error = await errorTask;
                
                if (process.ExitCode == 0)
                {
                    Console.WriteLine(output);
                }
                else
                {
                    Console.WriteLine($"❌ Failed to list inventory from remote host:");
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Console.WriteLine($"   {error.Replace("\n", "\n   ")}");
                    }
                    Console.WriteLine("   You may need to:");
                    Console.WriteLine("   1. Verify the remote host is online");
                    Console.WriteLine("   2. Check that your SSH key is authorized on the remote host");
                    Console.WriteLine("   3. Ensure Ansible is installed on the remote host");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error connecting to remote host: {ex.Message}");
            }
        }

        private async Task<bool> TryListInventoryWithNative()
        {
            try
            {
                Console.WriteLine("Trying with native Ansible...");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ansible-inventory",
                        Arguments = "--list -y",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                var timeout = Task.Delay(5000); // 5 second timeout
                
                process.Start();
                
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var exitTask = process.WaitForExitAsync();
                
                // Wait for process to complete or timeout
                if (await Task.WhenAny(exitTask, timeout) == timeout)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            Console.WriteLine("   Native Ansible command timed out, trying other methods...");
                            return false;
                        }
                    }
                    catch { /* Ignore errors during kill */ }
                }
                
                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode == 0)
                {
                    Console.WriteLine(output);
                    return true;
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    // Only log detailed error info in debug mode
                    Console.WriteLine("   Native Ansible command failed, trying other methods...");
                }
            }
            catch (Exception)
            {
                // Silently fail and try other methods
            }
            return false;
        }

        private async Task<bool> TryListInventoryWithWsl()
        {
            try
            {
                Console.WriteLine("Trying with WSL Ansible...");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wsl",
                        Arguments = "ansible-inventory --list -y",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                var timeout = Task.Delay(5000); // 5 second timeout
                
                process.Start();
                
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var exitTask = process.WaitForExitAsync();
                
                // Wait for process to complete or timeout
                if (await Task.WhenAny(exitTask, timeout) == timeout)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            Console.WriteLine("   WSL Ansible command timed out.");
                            return false;
                        }
                    }
                    catch { /* Ignore errors during kill */ }
                }
                
                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode == 0)
                {
                    Console.WriteLine(output);
                    return true;
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    // Only log detailed error info in debug mode
                    Console.WriteLine("   WSL Ansible command failed.");
                }
            }
            catch (Exception)
            {
                // Silently fail
            }
            return false;
        }
    }
}

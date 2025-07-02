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
            Console.WriteLine("❌ Not found or error occurred");
            Console.WriteLine("   Ansible is required for managing inventories.");
            Console.WriteLine("   Please either:");
            Console.WriteLine("   1. Install Ansible locally");
            Console.WriteLine("   2. Configure a remote Ansible server with `hum ansible-config`");
            Console.WriteLine();
            Console.WriteLine("   For Windows users, we recommend:");
            Console.WriteLine("   - WSL2 with Ubuntu: `wsl --install -d Ubuntu` then `wsl sudo apt install -y ansible`");
            Console.WriteLine("   - Or configure a remote Linux server with Ansible installed");
            Console.WriteLine("   Native Windows installation via pip has compatibility issues");
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

            // First try reading local inventory files directly
            if (await TryReadLocalInventoryFile())
            {
                return;
            }
            
            // Then try with remote config if available
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
            Console.WriteLine();
            Console.WriteLine("To create an inventory file manually, create one of these files:");
            Console.WriteLine("  - ./inventory.yml");
            Console.WriteLine("  - ./ansible/inventory.yml");
            Console.WriteLine("  - ~/.ansible/inventory");
            Console.WriteLine();
            Console.WriteLine("Example inventory file format:");
            Console.WriteLine("---");
            Console.WriteLine("all:");
            Console.WriteLine("  children:");
            Console.WriteLine("    webservers:");
            Console.WriteLine("      hosts:");
            Console.WriteLine("        webserver1:");
            Console.WriteLine("          ansible_host: 10.0.0.100");
            Console.WriteLine("          ansible_user: username");
            Console.WriteLine("      vars:");
            Console.WriteLine("        environment: production");
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

                // Upload local inventory file if it exists
                var localInventoryPath = Path.Combine(Environment.CurrentDirectory, "inventory.yml");
                if (File.Exists(localInventoryPath))
                {
                    Console.WriteLine("Found local inventory file, attempting to use it with remote Ansible...");
                    
                    // First, let's check if the local file and remote inventory match
                    string localContent = await File.ReadAllTextAsync(localInventoryPath);
                    
                    // Create a process to run inventory with our local file
                    var processWithLocalFile = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ssh",
                            Arguments = $"-o ConnectTimeout=5 -o BatchMode=yes -i \"{keyPath}\" {config.User}@{config.Host} \"cat > /tmp/hum_inventory.yml << 'EOF'\n{localContent}\nEOF\n\nansible-inventory -i /tmp/hum_inventory.yml --list -y\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    
                    var timeout = Task.Delay(10000); // 10 second timeout
                    
                    processWithLocalFile.Start();
                    
                    var outputTask = processWithLocalFile.StandardOutput.ReadToEndAsync();
                    var errorTask = processWithLocalFile.StandardError.ReadToEndAsync();
                    var exitTask = processWithLocalFile.WaitForExitAsync();
                    
                    if (await Task.WhenAny(exitTask, timeout) == timeout)
                    {
                        try
                        {
                            if (!processWithLocalFile.HasExited)
                            {
                                processWithLocalFile.Kill();
                                Console.WriteLine("❌ Connection to remote server timed out.");
                                return;
                            }
                        }
                        catch { /* Ignore errors during kill */ }
                    }
                    
                    string output = await outputTask;
                    string error = await errorTask;
                    
                    if (processWithLocalFile.ExitCode == 0)
                    {
                        Console.WriteLine(output);
                        return;
                    }
                    else
                    {
                        Console.WriteLine("   Failed to use local inventory with remote Ansible, trying other methods...");
                    }
                }

                // Try with several common inventory file locations on the remote server
                // Use a more efficient approach - check for existence of directories first
                Console.WriteLine("Running quick check for Ansible inventory directories...");
                
                // Let's check if basic directories exist first to avoid checking many individual files
                var checkDirsProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ssh",
                        Arguments = $"-o ConnectTimeout=3 -o BatchMode=yes -o ServerAliveInterval=2 -i \"{keyPath}\" {config.User}@{config.Host} \"test -d ~/ansible && echo 'ansible_dir_exists' ; test -d ~/.ansible && echo 'dot_ansible_exists'\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                var dirCheckTimeout = Task.Delay(5000);
                checkDirsProcess.Start();
                var dirOutput = await checkDirsProcess.StandardOutput.ReadToEndAsync();
                var dirExitTask = checkDirsProcess.WaitForExitAsync();
                
                if (await Task.WhenAny(dirExitTask, dirCheckTimeout) == dirCheckTimeout && !checkDirsProcess.HasExited)
                {
                    try { checkDirsProcess.Kill(); } catch { }
                    Console.WriteLine("Directory check timed out. Trying with limited inventory locations...");
                }
                
                bool hasAnsibleDir = dirOutput.Contains("ansible_dir_exists");
                bool hasDotAnsibleDir = dirOutput.Contains("dot_ansible_exists");
                
                Console.WriteLine($"Remote directories found: " + 
                                 (hasAnsibleDir ? "~/ansible, " : "") + 
                                 (hasDotAnsibleDir ? "~/.ansible" : "") +
                                 (!hasAnsibleDir && !hasDotAnsibleDir ? "none" : ""));
                
                // Prioritize inventory locations based on what directories exist
                var remoteInventoryLocations = new List<string>();
                
                // Always check the specifically mentioned path first
                remoteInventoryLocations.Add("~/ansible/inventory/self-hosted.yaml");
                
                if (hasAnsibleDir)
                {
                    remoteInventoryLocations.Add("~/ansible/inventory/*");
                    remoteInventoryLocations.Add("~/ansible/inventory.yml");
                    remoteInventoryLocations.Add("~/ansible/hosts");
                }
                
                remoteInventoryLocations.Add("~/inventory.yml");
                remoteInventoryLocations.Add("~/inventory.yaml");
                remoteInventoryLocations.Add("~/inventory");
                
                if (hasDotAnsibleDir)
                {
                    remoteInventoryLocations.Add("~/.ansible/inventory");
                    remoteInventoryLocations.Add("~/.ansible/hosts");
                }
                
                remoteInventoryLocations.Add("/etc/ansible/hosts");
                
                Console.WriteLine("Now checking specific inventory files...");
                
                // First, check if any of the common inventory files exist
                foreach (var inventoryPath in remoteInventoryLocations)
                {
                    Console.WriteLine($"Checking for remote inventory at {inventoryPath}...");
                    
                    // Different check command if path contains wildcard
                    string checkCommand = inventoryPath.Contains("*") 
                        ? $"ls {inventoryPath} >/dev/null 2>&1 ; if [ $? -eq 0 ]; then echo 'found'; else echo 'notfound'; fi" 
                        : $"test -f {inventoryPath} ; if [ $? -eq 0 ]; then echo 'found'; else echo 'notfound'; fi";
                    
                    var checkProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ssh",
                            Arguments = $"-o ConnectTimeout=3 -o BatchMode=yes -o ServerAliveInterval=2 -o ServerAliveCountMax=2 -i \"{keyPath}\" {config.User}@{config.Host} \"{checkCommand}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    
                    // Add timeout for this check command too
                    var checkTimeout = Task.Delay(5000); // 5 second timeout for the check
                    
                    checkProcess.Start();
                    
                    var checkOutputTask = checkProcess.StandardOutput.ReadToEndAsync();
                    var checkExitTask = checkProcess.WaitForExitAsync();
                    
                    // Wait for the check process to complete or timeout
                    if (await Task.WhenAny(checkExitTask, checkTimeout) == checkTimeout)
                    {
                        try
                        {
                            if (!checkProcess.HasExited)
                            {
                                checkProcess.Kill();
                                Console.WriteLine("   - Check timed out after 5 seconds, skipping this location...");
                                Console.WriteLine("   - This may indicate SSH connectivity issues or server load");
                                continue;
                            }
                        }
                        catch { continue; }
                    }
                    
                    string checkResult = await checkOutputTask;
                    
                    if (checkProcess.ExitCode != 0)
                    {
                        Console.WriteLine("   - Failed to check this location, skipping...");
                        continue;
                    }
                    
                    if (checkResult.Trim() != "found")
                    {
                        Console.WriteLine("   - No inventory found at this location");
                        continue;
                    }
                    
                    Console.WriteLine($"Found remote inventory at: {inventoryPath}");
                    
                    // Handle wildcard paths
                    string inventoryArg = inventoryPath;
                    if (inventoryPath.Contains("*"))
                    {
                        // For wildcard paths, get the actual file(s)
                        var listProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "ssh",
                                Arguments = $"-o ConnectTimeout=5 -o BatchMode=yes -i \"{keyPath}\" {config.User}@{config.Host} \"find $(echo {inventoryPath} | sed 's|~|'$HOME'|') -type f 2>/dev/null | head -n 1\"",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        
                        listProcess.Start();
                        string firstFile = await listProcess.StandardOutput.ReadToEndAsync();
                        await listProcess.WaitForExitAsync();
                        
                        if (!string.IsNullOrWhiteSpace(firstFile))
                        {
                            inventoryArg = firstFile.Trim();
                            Console.WriteLine($"Using specific inventory file: {inventoryArg}");
                        }
                    }
                    
                    var inventoryProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ssh",
                            Arguments = $"-o ConnectTimeout=5 -o BatchMode=yes -i \"{keyPath}\" {config.User}@{config.Host} \"ansible-inventory -i '{inventoryArg}' --list -y\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    
                    var timeout = Task.Delay(10000);
                    
                    inventoryProcess.Start();
                    
                    var outputTask = inventoryProcess.StandardOutput.ReadToEndAsync();
                    var errorTask = inventoryProcess.StandardError.ReadToEndAsync();
                    var exitTask = inventoryProcess.WaitForExitAsync();
                    
                    if (await Task.WhenAny(exitTask, timeout) == timeout)
                    {
                        try
                        {
                            if (!inventoryProcess.HasExited)
                            {
                                inventoryProcess.Kill();
                                Console.WriteLine("❌ Connection to remote server timed out.");
                                continue;
                            }
                        }
                        catch { continue; }
                    }
                    
                    string output = await outputTask;
                    string error = await errorTask;
                    
                    if (inventoryProcess.ExitCode == 0)
                    {
                        Console.WriteLine(output);
                        return;
                    }
                }
                
                // If no inventory file was found, try with the direct path to self-hosted.yaml
                Console.WriteLine();
                Console.WriteLine("⚠️ No inventory files found at common locations.");
                Console.WriteLine("Trying directly with ansible-inventory command...");
                Console.WriteLine("Running: ansible-inventory -i ~/ansible/inventory/self-hosted.yaml --list -y");
                
                // Use a more robust command with better error handling
                var defaultProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ssh",
                        Arguments = $"-o ConnectTimeout=3 -o BatchMode=yes -o ServerAliveInterval=2 -i \"{keyPath}\" {config.User}@{config.Host} \"echo 'Checking for default inventory...' ; ansible-inventory -i ~/ansible/inventory/self-hosted.yaml --list -y 2>/dev/null || (echo 'Direct inventory command failed, trying default ansible-inventory...' && ansible-inventory --list -y)\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                var defaultTimeout = Task.Delay(10000); // 10 second timeout
                
                defaultProcess.Start();
                
                // Create tasks for reading output/error and waiting for exit
                var defaultOutputTask = defaultProcess.StandardOutput.ReadToEndAsync();
                var defaultErrorTask = defaultProcess.StandardError.ReadToEndAsync();
                var defaultExitTask = defaultProcess.WaitForExitAsync();
                
                // Wait for process to complete or timeout
                if (await Task.WhenAny(defaultExitTask, defaultTimeout) == defaultTimeout)
                {
                    try
                    {
                        if (!defaultProcess.HasExited)
                        {
                            defaultProcess.Kill();
                            Console.WriteLine();
                            Console.WriteLine("❌ Connection to remote server timed out after 10 seconds.");
                            Console.WriteLine();
                            Console.WriteLine("Troubleshooting steps:");
                            Console.WriteLine("1. Check that the remote host is online:");
                            Console.WriteLine($"   - Try: ping {config.Host}");
                            Console.WriteLine("2. Verify your SSH connection works:");
                            Console.WriteLine($"   - Try: ssh -i \"{keyPath}\" {config.User}@{config.Host} echo \"Connection successful\"");
                            Console.WriteLine("3. Check Ansible installation on remote host:");
                            Console.WriteLine($"   - Try: ssh -i \"{keyPath}\" {config.User}@{config.Host} \"ansible --version\"");
                            Console.WriteLine("4. Create inventory manually if needed:");
                            Console.WriteLine($"   - Try: ssh -i \"{keyPath}\" {config.User}@{config.Host} \"mkdir -p ~/ansible/inventory && echo 'all:\\n  hosts:\\n    localhost:\\n      ansible_connection: local' > ~/ansible/inventory/self-hosted.yaml\"");
                            Console.WriteLine();
                            return;
                        }
                    }
                    catch { /* Ignore errors during kill */ }
                }
                
                // Get output now that process has completed or been killed
                string defaultOutput = await defaultOutputTask;
                string defaultError = await defaultErrorTask;

                if (defaultProcess.ExitCode == 0)
                {
                    if (string.IsNullOrWhiteSpace(defaultOutput))
                    {
                        Console.WriteLine("⚠️ Inventory command succeeded but returned empty output.");
                        Console.WriteLine("   This might indicate that no inventory is configured on the remote host.");
                        Console.WriteLine("   You may need to create an inventory file on the remote host.");
                    }
                    else
                    {
                        Console.WriteLine(defaultOutput);
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Failed to list inventory from remote host:");
                    if (!string.IsNullOrWhiteSpace(defaultError))
                    {
                        Console.WriteLine($"   {defaultError.Replace("\n", "\n   ")}");
                    }
                    else
                    {
                        Console.WriteLine("   No error message returned. Exit code: " + defaultProcess.ExitCode);
                    }
                    Console.WriteLine("   You may need to:");
                    Console.WriteLine("   1. Verify the remote host is online");
                    Console.WriteLine("   2. Check that your SSH key is authorized on the remote host");
                    Console.WriteLine("   3. Ensure Ansible is installed on the remote host");
                    Console.WriteLine("   4. Create an inventory file on the remote host (~/.ansible/inventory)");
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
                            Console.WriteLine("❌ WSL Ansible command timed out.");
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

        private async Task<bool> TryReadLocalInventoryFile()
        {
            try
            {
                Console.WriteLine("Looking for local inventory files...");
                
                // Common inventory file locations to check
                var possibleLocations = new[]
                {
                    Path.Combine(Environment.CurrentDirectory, "inventory"),
                    Path.Combine(Environment.CurrentDirectory, "inventory.yml"),
                    Path.Combine(Environment.CurrentDirectory, "inventory.yaml"),
                    Path.Combine(Environment.CurrentDirectory, "hosts"),
                    Path.Combine(Environment.CurrentDirectory, "ansible", "inventory"),
                    Path.Combine(Environment.CurrentDirectory, "ansible", "inventory.yml"),
                    Path.Combine(Environment.CurrentDirectory, "ansible", "hosts"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ansible", "inventory"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ansible", "hosts")
                };
                
                foreach (var path in possibleLocations)
                {
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"Found inventory file at: {path}");
                        Console.WriteLine();
                        
                        string content = await File.ReadAllTextAsync(path);
                        Console.WriteLine(content);
                        return true;
                    }
                }
                
                Console.WriteLine("No local inventory files found in common locations.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading local inventory file: {ex.Message}");
                return false;
            }
        }
    }
}

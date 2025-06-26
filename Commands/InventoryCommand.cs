using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace hum.Commands
{
    public class InventoryCommand : Command
    {
        public InventoryCommand() : base("inventory", "List the current inventory and validate Ansible configuration")
        {
            this.SetHandler(HandleCommand);
        }

        private async Task HandleCommand(InvocationContext context)
        {
            Console.WriteLine("🔍 Checking Ansible configuration and listing inventory...");
            Console.WriteLine();

            bool ansibleOk = await CheckAnsible();

            if (ansibleOk)
            {
                await ListInventory();
            }
            else
            {
                Console.WriteLine("❌ Cannot list inventory because Ansible is not configured correctly.");
            }
        }

        private async Task<bool> CheckAnsible()
        {
            Console.Write("Checking Ansible... ");

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
                    // just get the first line of the output
                    string version = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];
                    Console.WriteLine($"✅ {version}");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Not found or error occurred");
                    Console.WriteLine("   Install Ansible and ensure it is in your PATH. See https://docs.ansible.com/ansible/latest/installation_guide/index.html");
                    return false;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("❌ Not found or error occurred");
                Console.WriteLine("   Install Ansible and ensure it is in your PATH. See https://docs.ansible.com/ansible/latest/installation_guide/index.html");
                return false;
            }
        }

        private async Task ListInventory()
        {
            Console.WriteLine();
            Console.WriteLine("📜 Listing Ansible inventory...");
            Console.WriteLine();

            try
            {
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

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine(output);
                }
                else
                {
                    Console.WriteLine("❌ Failed to list inventory.");
                    Console.WriteLine(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listing inventory: {ex.Message}");
            }
        }
    }
}

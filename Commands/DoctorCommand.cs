using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using hum.Services;

namespace hum.Commands
{
    public class DoctorCommand : Command
    {
        public DoctorCommand() : base("doctor", "Validate configuration and connectivity")
        {
            this.SetHandler(HandleCommand);
        }

        private async Task HandleCommand(InvocationContext context)
        {
            Console.WriteLine("🔍 Running hum environment diagnostics...");
            Console.WriteLine();

            bool allChecksPass = true;

            // Check .NET SDK
            allChecksPass &= await CheckDotNetSdk();

            // Check Git
            allChecksPass &= await CheckGit();            // Check GitHub CLI (now required)
            allChecksPass &= await CheckGitHubCli();            // Check hum configuration (now optional)
            await CheckHumConfig();

            Console.WriteLine();
            if (allChecksPass)
            {
                Console.WriteLine("✅ All critical checks passed! hum is ready to use.");
            }
            else
            {
                Console.WriteLine("❌ Some checks failed. Please address the issues above before using hum.");
            }
        }

        private async Task<bool> CheckDotNetSdk()
        {
            Console.Write("Checking .NET SDK... ");
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
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
                    string version = output.Trim();
                    Console.WriteLine($"✅ {version}");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Not found or error occurred");
                    Console.WriteLine("   Install .NET SDK 9.0 or later from https://dotnet.microsoft.com/download");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CheckGit()
        {
            Console.Write("Checking Git... ");
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
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
                    Console.WriteLine($"✅ {output.Trim()}");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Not found");
                    Console.WriteLine("   Install Git from https://git-scm.com/downloads");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return false;
            }
        }        private async Task<bool> CheckGitHubCli()
        {
            Console.Write("Checking GitHub CLI authentication... ");
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = "auth status",
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
                    // Parse the auth status output to get username
                    string statusInfo = !string.IsNullOrEmpty(output) ? output : error;
                    Console.WriteLine($"✅ Authenticated and ready");
                    if (!string.IsNullOrEmpty(statusInfo))
                    {
                        Console.WriteLine($"   {statusInfo.Split('\n')[0].Trim()}");
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Not authenticated");
                    Console.WriteLine("   Run: gh auth login");
                    Console.WriteLine("   This will open your browser for secure OAuth authentication");
                    return false;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("❌ GitHub CLI not found");
                Console.WriteLine("   Install from https://cli.github.com/");
                Console.WriteLine("   Then run: gh auth login");
                return false;
            }
        }        private async Task<bool> CheckHumConfig()
        {
            Console.Write("Checking hum configuration... ");

            try
            {
                var configService = new ConfigurationService();
                var settings = await configService.LoadSettingsAsync();

                // With GitHub CLI, we only need git config for local operations
                bool hasGitUsername = !string.IsNullOrEmpty(settings.DefaultGitConfig?.Username);
                bool hasGitEmail = !string.IsNullOrEmpty(settings.DefaultGitConfig?.Email);

                if (hasGitUsername && hasGitEmail)
                {
                    Console.WriteLine("✅ Git configuration set");
                    return true;
                }
                else
                {
                    Console.WriteLine("⚠️  Git configuration incomplete (optional)");
                    if (!hasGitUsername)
                        Console.WriteLine("   Missing git username. Run: hum config --git-username \"Your Name\"");
                    if (!hasGitEmail)
                        Console.WriteLine("   Missing git email. Run: hum config --git-email \"your@email.com\"");
                    Console.WriteLine("   Note: These are only needed for local git operations");
                    return true; // Not critical since GitHub CLI handles authentication
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading config: {ex.Message}");
                return false;
            }        }
    }
}

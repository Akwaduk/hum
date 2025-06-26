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
            allChecksPass &= await CheckGit();

            // Check GitHub CLI (optional)
            await CheckGitHubCli();

            // Check hum configuration
            allChecksPass &= await CheckHumConfig();

            // Check environment variables
            allChecksPass &= CheckEnvironmentVariables();

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
        }

        private async Task<bool> CheckGitHubCli()
        {
            Console.Write("Checking GitHub CLI (optional)... ");
            
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
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
                    Console.WriteLine($"✅ {output.Split('\n')[0].Trim()}");
                    return true;
                }
                else
                {
                    Console.WriteLine("⚠️  Not found (optional but recommended)");
                    Console.WriteLine("   Install from https://cli.github.com/");
                    return true; // Not critical
                }
            }
            catch (Exception)
            {
                Console.WriteLine("⚠️  Not found (optional but recommended)");
                return true; // Not critical
            }
        }

        private async Task<bool> CheckHumConfig()
        {
            Console.Write("Checking hum configuration... ");

            try
            {
                var configService = new ConfigurationService();
                var settings = await configService.LoadSettingsAsync();

                bool hasGitHubToken = !string.IsNullOrEmpty(settings.GitHubToken);
                bool hasGitHubUsername = !string.IsNullOrEmpty(settings.GitHubUsername);

                if (hasGitHubToken && hasGitHubUsername)
                {
                    Console.WriteLine("✅ GitHub credentials configured");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ GitHub credentials not configured");
                    if (!hasGitHubToken)
                        Console.WriteLine("   Missing GitHub token. Run: hum config --github-token <your-token>");
                    if (!hasGitHubUsername)
                        Console.WriteLine("   Missing GitHub username. Run: hum config --github-username <your-username>");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading config: {ex.Message}");
                return false;
            }
        }

        private bool CheckEnvironmentVariables()
        {
            Console.Write("Checking environment variables... ");

            string? githubToken = Environment.GetEnvironmentVariable("HUM_GITHUB_TOKEN");
            
            if (!string.IsNullOrEmpty(githubToken))
            {
                Console.WriteLine("✅ HUM_GITHUB_TOKEN is set");
                return true;
            }
            else
            {
                Console.WriteLine("⚠️  HUM_GITHUB_TOKEN not set (using config file instead)");
                return true; // Not critical if config file has token
            }
        }
    }
}

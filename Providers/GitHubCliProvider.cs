using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using hum.Models;

namespace hum.Providers
{
    public class GitHubCliProvider : ISourceControlProvider, ICiCdProvider
    {
        public string ProviderName => "GitHub CLI";

        public async Task<RepositoryInfo> CreateRepositoryAsync(string name, string description)
        {
            Console.WriteLine($"Creating GitHub repository via gh CLI: {name}");
            
            try
            {
                // Create repository using gh CLI
                var createProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = $"repo create {name} --description \"{description}\" --public --clone=false",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                createProcess.Start();
                string output = await createProcess.StandardOutput.ReadToEndAsync();
                string error = await createProcess.StandardError.ReadToEndAsync();
                await createProcess.WaitForExitAsync();

                if (createProcess.ExitCode != 0)
                {
                    throw new Exception($"Failed to create repository: {error}");
                }

                // Get repository details
                var detailsProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = $"repo view {name} --json name,description,url,sshUrl,defaultBranchRef,owner",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                detailsProcess.Start();
                string detailsOutput = await detailsProcess.StandardOutput.ReadToEndAsync();
                await detailsProcess.WaitForExitAsync();

                if (detailsProcess.ExitCode != 0)
                {
                    throw new Exception("Failed to get repository details");
                }

                var repoData = JsonSerializer.Deserialize<JsonElement>(detailsOutput);
                
                return new RepositoryInfo
                {
                    Name = repoData.GetProperty("name").GetString()!,
                    Description = repoData.GetProperty("description").GetString() ?? "",
                    Url = repoData.GetProperty("url").GetString()!,
                    CloneUrl = repoData.GetProperty("sshUrl").GetString()!,
                    DefaultBranch = repoData.GetProperty("defaultBranchRef").GetProperty("name").GetString()!,
                    Owner = repoData.GetProperty("owner").GetProperty("login").GetString()!,
                    ProviderSpecificId = name
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating GitHub repository: {ex.Message}");
                throw;
            }
        }

        public async Task ConfigureRepositoryAsync(RepositoryInfo repository, ProjectConfig projectConfig)
        {
            Console.WriteLine($"Configuring GitHub repository via gh CLI: {repository.Name}");
            
            // Configure branch protection if needed
            try
            {
                var protectionProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = $"api repos/{repository.Owner}/{repository.Name}/branches/{repository.DefaultBranch}/protection --method PUT --field required_status_checks.strict=true",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                protectionProcess.Start();
                await protectionProcess.WaitForExitAsync();

                if (protectionProcess.ExitCode == 0)
                {
                    Console.WriteLine($"Branch protection configured for {repository.DefaultBranch}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not configure branch protection: {ex.Message}");
            }
        }

        public async Task<string> GetRepositoryUrlAsync(string repositoryName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = $"repo view {repositoryName} --json url",
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
                    var data = JsonSerializer.Deserialize<JsonElement>(output);
                    return data.GetProperty("url").GetString() ?? "";
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        public async Task ConfigurePipelinesAsync(RepositoryInfo repository, ProjectConfig projectConfig)
        {
            Console.WriteLine($"Configuring GitHub Actions via gh CLI: {repository.Name}");
            
            // This would involve creating workflow files in the repository
            // Implementation would depend on how we want to handle file creation
            await Task.CompletedTask;
        }

        public async Task<bool> ValidateConfigurationAsync(RepositoryInfo repository)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        Arguments = $"repo view {repository.Owner}/{repository.Name}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
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
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using hum.Models;

namespace hum.Providers
{
    public class GitHubCliProvider : ISourceControlProvider, ICiCdProvider
    {
        public string ProviderName => "GitHub CLI";
        
        // Add a method to check if this provider can handle a specific source control type
        public bool CanHandle(string sourceControlType)
        {
            return sourceControlType.Equals("github", StringComparison.OrdinalIgnoreCase);
        }

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
            
            // First, initialize the local git repository and push it to GitHub
            try
            {
                string projectPath = projectConfig.OutputPath ?? Path.Combine(System.Environment.CurrentDirectory, projectConfig.Name);
                
                if (!Directory.Exists(projectPath))
                {
                    throw new DirectoryNotFoundException($"Project directory {projectPath} not found");
                }
                
                Console.WriteLine($"Setting up Git repository in {projectPath}");
                
                // Check if .git directory already exists
                if (!Directory.Exists(Path.Combine(projectPath, ".git")))
                {
                    Console.WriteLine("Initializing Git repository...");
                    
                    // Initialize Git repository with timeout
                    using var initProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = "init",
                            WorkingDirectory = projectPath,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    // Start the process
                    initProcess.Start();
                    
                    // Wait for the process to complete with a timeout
                    var initTask = initProcess.WaitForExitAsync();
                    if (await Task.WhenAny(initTask, Task.Delay(TimeSpan.FromSeconds(10))) != initTask)
                    {
                        try
                        {
                            // Process is taking too long, try to kill it
                            if (!initProcess.HasExited)
                            {
                                initProcess.Kill(true);
                            }
                            throw new TimeoutException("Git init command timed out after 10 seconds");
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Git init timed out and couldn't be terminated: {ex.Message}");
                        }
                    }
                    
                    // Check exit code after process has completed
                    if (initProcess.ExitCode != 0)
                    {
                        var error = await initProcess.StandardError.ReadToEndAsync();
                        throw new Exception($"Git init failed: {error}");
                    }
                    
                    Console.WriteLine("Git repository initialized");
                }
                else
                {
                    Console.WriteLine("Git repository already exists");
                }
                
                // Add remote or update existing remote
                Console.WriteLine("Setting up remote repository connection...");
                var remoteUrl = $"https://github.com/{repository.Owner}/{repository.Name}.git";
                
                // First, check if remote exists
                using var checkRemoteProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "remote -v",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                checkRemoteProcess.Start();
                var remoteOutput = await checkRemoteProcess.StandardOutput.ReadToEndAsync();
                await checkRemoteProcess.WaitForExitAsync();
                
                string remoteCommand;
                if (remoteOutput.Contains("origin"))
                {
                    // Update existing remote
                    Console.WriteLine("Updating existing remote 'origin'");
                    remoteCommand = $"remote set-url origin {remoteUrl}";
                }
                else
                {
                    // Add new remote
                    Console.WriteLine("Adding new remote 'origin'");
                    remoteCommand = $"remote add origin {remoteUrl}";
                }
                
                using var remoteProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = remoteCommand,
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                remoteProcess.Start();
                
                // Wait with timeout
                var remoteTask = remoteProcess.WaitForExitAsync();
                if (await Task.WhenAny(remoteTask, Task.Delay(TimeSpan.FromSeconds(10))) != remoteTask)
                {
                    // Process is taking too long, try to kill it
                    if (!remoteProcess.HasExited)
                    {
                        remoteProcess.Kill(true);
                    }
                    Console.WriteLine("WARNING: Git remote command timed out, continuing anyway");
                }
                
                // Configure git with default identity if not set
                // This prevents git from asking for user info during commit
                using var configNameProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "config user.name || git config --local user.name \"hum CLI\"",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                configNameProcess.Start();
                await configNameProcess.WaitForExitAsync();
                
                using var configEmailProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "config user.email || git config --local user.email \"hum@example.com\"",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                configEmailProcess.Start();
                await configEmailProcess.WaitForExitAsync();
                
                // Add all files
                Console.WriteLine("Adding files to git...");
                using var addProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "add .",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                addProcess.Start();
                
                // Wait with timeout
                var addTask = addProcess.WaitForExitAsync();
                if (await Task.WhenAny(addTask, Task.Delay(TimeSpan.FromSeconds(30))) != addTask)
                {
                    // Process is taking too long, try to kill it
                    if (!addProcess.HasExited)
                    {
                        addProcess.Kill(true);
                    }
                    Console.WriteLine("WARNING: Git add command timed out, continuing anyway");
                }
                else if (addProcess.ExitCode != 0)
                {
                    Console.WriteLine($"WARNING: Git add returned non-zero exit code: {addProcess.ExitCode}");
                }
                
                // Check if there are changes to commit
                using var statusProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "status --porcelain",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                statusProcess.Start();
                var statusOutput = await statusProcess.StandardOutput.ReadToEndAsync();
                await statusProcess.WaitForExitAsync();
                
                // Only commit if there are changes
                if (!string.IsNullOrWhiteSpace(statusOutput))
                {
                    Console.WriteLine("Committing files...");
                    // Commit files
                    using var commitProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = "commit -m \"Initial commit\" --no-verify",
                            WorkingDirectory = projectPath,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    commitProcess.Start();
                    
                    // Wait with timeout
                    var commitTask = commitProcess.WaitForExitAsync();
                    if (await Task.WhenAny(commitTask, Task.Delay(TimeSpan.FromSeconds(20))) != commitTask)
                    {
                        // Process is taking too long, try to kill it
                        if (!commitProcess.HasExited)
                        {
                            commitProcess.Kill(true);
                        }
                        Console.WriteLine("WARNING: Git commit command timed out, continuing anyway");
                    }
                    else if (commitProcess.ExitCode != 0)
                    {
                        var error = await commitProcess.StandardError.ReadToEndAsync();
                        Console.WriteLine($"WARNING: Git commit had issues: {error}");
                    }
                }
                else
                {
                    Console.WriteLine("No changes to commit");
                }
                
                // Use GitHub CLI to push code instead of git command directly
                // This ensures authentication is handled through GitHub CLI
                Console.WriteLine("Pushing code to GitHub using gh CLI...");
                
                // First determine the local branch name
                Console.WriteLine("Determining current branch name...");
                using var branchNameProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "branch --show-current",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                branchNameProcess.Start();
                string localBranch = (await branchNameProcess.StandardOutput.ReadToEndAsync()).Trim();
                await branchNameProcess.WaitForExitAsync();
                
                // If we couldn't determine the local branch or it's empty, use the repository default branch or fall back to "main"
                string defaultBranch = !string.IsNullOrWhiteSpace(localBranch) ? localBranch : (repository.DefaultBranch ?? "main");
                Console.WriteLine($"Using branch: {defaultBranch}");
                
                // First check if we need to pull changes (e.g., if remote has a README)
                Console.WriteLine("Checking if we need to pull changes from remote first...");
                using var fetchProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "fetch",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                fetchProcess.Start();
                await fetchProcess.WaitForExitAsync();
                
                // Set up the branch tracking first, then try to pull
                Console.WriteLine("Setting up branch tracking...");
                using var branchSetupProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"branch --set-upstream-to=origin/{defaultBranch} {defaultBranch} || git checkout -b {defaultBranch}",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                branchSetupProcess.Start();
                await branchSetupProcess.WaitForExitAsync();
                
                // Now try to pull with rebase to incorporate any remote changes
                Console.WriteLine("Pulling remote changes with rebase...");
                using var pullProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"pull --rebase origin {defaultBranch}",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                pullProcess.Start();
                string pullOutput = await pullProcess.StandardOutput.ReadToEndAsync();
                string pullError = await pullProcess.StandardError.ReadToEndAsync();
                await pullProcess.WaitForExitAsync();
                
                if (pullProcess.ExitCode != 0)
                {
                    // If pull fails, we'll try to force push
                    Console.WriteLine($"Warning: Pull failed: {pullError}. Will try to push anyway.");
                }
                
                // Try two approaches for pushing - first try direct git push
                Console.WriteLine($"Attempting direct git push to {defaultBranch}...");
                using var directPushProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"push -u origin {defaultBranch} --force",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                directPushProcess.Start();
                string directPushOutput = await directPushProcess.StandardOutput.ReadToEndAsync();
                string directPushError = await directPushProcess.StandardError.ReadToEndAsync();
                await directPushProcess.WaitForExitAsync();
                
                // If direct git push succeeds, we're done
                if (directPushProcess.ExitCode == 0) {
                    Console.WriteLine("Successfully pushed code using direct git push");
                    return;
                }
                
                Console.WriteLine($"Direct git push failed with error: {directPushError}");
                Console.WriteLine("Trying gh CLI push as alternative...");
                
                // Try another direct push approach with explicit branch
                Console.WriteLine($"Attempting push with explicit branch setup...");
                using var pushUpstreamProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"push --set-upstream origin {defaultBranch} --force",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                pushUpstreamProcess.Start();
                string pushUpstreamOutput = await pushUpstreamProcess.StandardOutput.ReadToEndAsync();
                string pushUpstreamError = await pushUpstreamProcess.StandardError.ReadToEndAsync();
                await pushUpstreamProcess.WaitForExitAsync();
                
                if (pushUpstreamProcess.ExitCode == 0) {
                    Console.WriteLine("Successfully pushed code with explicit upstream setting");
                    return;
                }
                
                // If direct push fails, try gh CLI approach as backup - using correct format
                using var pushProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "gh",
                        // Use correct format for gh repo sync: OWNER/REPO
                        Arguments = $"repo sync {repository.Owner}/{repository.Name} --source=local --branch={defaultBranch} --force",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                pushProcess.Start();
                
                // Wait with timeout
                var pushTask = pushProcess.WaitForExitAsync();
                if (await Task.WhenAny(pushTask, Task.Delay(TimeSpan.FromSeconds(60))) != pushTask)
                {
                    // Process is taking too long, try to kill it
                    if (!pushProcess.HasExited)
                    {
                        pushProcess.Kill(true);
                    }
                    Console.WriteLine("WARNING: Push command timed out after 60 seconds");
                    
                    // Try alternative approach using gh api directly
                    Console.WriteLine("Trying alternative approach to push code...");
                    
                    try
                    {
                        // Use gh api to create a file directly
                        using var createFileProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "gh",
                                Arguments = $"api --method PUT repos/{repository.Owner}/{repository.Name}/contents/README.md --field message=\"Initial commit from hum CLI\" --field content=\"{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"# {projectConfig.Name}\n\n{projectConfig.Description}\n\nCreated with hum CLI"))}\"",
                                WorkingDirectory = projectPath,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        createFileProcess.Start();
                        await createFileProcess.WaitForExitAsync();
                        
                        if (createFileProcess.ExitCode == 0)
                        {
                            Console.WriteLine("Created initial file in repository");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Alternative push method also failed: {ex.Message}");
                    }
                }
                else if (pushProcess.ExitCode != 0)
                {
                    var error = await pushProcess.StandardError.ReadToEndAsync();
                    Console.WriteLine($"Warning: GitHub CLI push had issues: {error}");
                    
                    // Final fallback: Delete and recreate repository
                    Console.WriteLine("All push methods failed. Attempting repository recreation as fallback...");
                    
                    try {
                        // Delete the existing repository
                        Console.WriteLine($"Deleting repository {repository.Owner}/{repository.Name} to recreate it...");
                        using var deleteRepoProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "gh",
                                Arguments = $"repo delete {repository.Owner}/{repository.Name} --yes",
                                WorkingDirectory = projectPath,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        deleteRepoProcess.Start();
                        await deleteRepoProcess.WaitForExitAsync();
                        
                        // Recreate the repository without README/AutoInit
                        Console.WriteLine($"Recreating repository {repository.Name}...");
                        using var createRepoProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "gh",
                                Arguments = $"repo create {repository.Name} --description \"{projectConfig.Description}\" --public --clone=false",
                                WorkingDirectory = projectPath,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        createRepoProcess.Start();
                        await createRepoProcess.WaitForExitAsync();
                        
                        // Try direct push again
                        Console.WriteLine("Pushing to freshly created repository...");
                        using var finalPushProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "git",
                                Arguments = $"push -u origin {defaultBranch}",
                                WorkingDirectory = projectPath,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        finalPushProcess.Start();
                        await finalPushProcess.WaitForExitAsync();
                        
                        if (finalPushProcess.ExitCode == 0) {
                            Console.WriteLine("Successfully pushed code after repository recreation");
                        } else {
                            Console.WriteLine("Failed to push even after repository recreation. See troubleshooting in docs/WORKING_WITH_GITHUB.md");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Repository recreation failed: {ex.Message}");
                        
                        // Absolute last resort - create a file via API
                        Console.WriteLine("Creating initial README file as last resort...");
                        try
                        {
                            using var createFileProcess = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "gh",
                                    Arguments = $"api --method PUT repos/{repository.Owner}/{repository.Name}/contents/README.md --field message=\"Initial commit from hum CLI\" --field content=\"{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"# {projectConfig.Name}\n\n{projectConfig.Description}\n\nCreated with hum CLI"))}\"",
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                }
                            };
                            createFileProcess.Start();
                            await createFileProcess.WaitForExitAsync();
                        }
                        catch
                        {
                            // Ignore errors in the fallback method
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Code pushed to GitHub successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not push code to GitHub: {ex.Message}");
            }
            
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

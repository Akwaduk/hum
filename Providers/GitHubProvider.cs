using System;
using System.Threading.Tasks;
using hum.Models;
using Octokit;

namespace hum.Providers
{
    public class GitHubProvider : ISourceControlProvider, ICiCdProvider
    {
        private readonly GitHubClient _client;
        private readonly string _username;
        
        public bool CanHandle(string sourceControlType)
        {
            return sourceControlType.Equals("github", StringComparison.OrdinalIgnoreCase);
        }

        public GitHubProvider(string token, string username)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("GitHub token is required", nameof(token));
            
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("GitHub username is required", nameof(username));

            _username = username;
            _client = new GitHubClient(new ProductHeaderValue("hum-cli"))
            {
                Credentials = new Credentials(token)
            };
        }

        public string ProviderName => "GitHub";

        public async Task<RepositoryInfo> CreateRepositoryAsync(string name, string description)
        {
            Console.WriteLine($"Creating GitHub repository: {name}");
            
            var newRepo = new NewRepository(name)
            {
                Description = description,
                Private = false,
                AutoInit = true
            };

            try
            {
                var repository = await _client.Repository.Create(newRepo);
                
                return new RepositoryInfo
                {
                    Name = repository.Name,
                    Description = repository.Description,
                    Url = repository.HtmlUrl,
                    CloneUrl = repository.CloneUrl,
                    DefaultBranch = repository.DefaultBranch,
                    Owner = repository.Owner.Login,
                    ProviderSpecificId = repository.Id.ToString()
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
            Console.WriteLine($"Configuring GitHub repository: {repository.Name}");
            
            // Configure branch protection rules
            try
            {
                var branch = await _client.Repository.Branch.Get(repository.Owner, repository.Name, repository.DefaultBranch);
                
                var updateBranchProtection = new BranchProtectionSettingsUpdate(
                    new BranchProtectionRequiredStatusChecksUpdate(true, new[] { "build" }),
                    new BranchProtectionRequiredReviewsUpdate(true, false, 1),
                    new BranchProtectionPushRestrictionsUpdate(),
                    true);
                
                await _client.Repository.Branch.UpdateBranchProtection(
                    repository.Owner, 
                    repository.Name, 
                    repository.DefaultBranch, 
                    updateBranchProtection);
                
                Console.WriteLine($"Branch protection rules configured for {repository.DefaultBranch}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not configure branch protection: {ex.Message}");
                // Continue execution, as this is not critical
            }
        }

        public async Task<string> GetRepositoryUrlAsync(string repositoryName)
        {
            try
            {
                var repository = await _client.Repository.Get(_username, repositoryName);
                return repository.HtmlUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting repository URL: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task ConfigurePipelinesAsync(RepositoryInfo repository, ProjectConfig projectConfig)
        {
            Console.WriteLine($"Configuring GitHub Actions for repository: {repository.Name}");
            
            // Create GitHub Actions workflow files
            try
            {
                // Create the .github/workflows directory structure
                string workflowContent = GenerateWorkflowFile(projectConfig);
                
                // Create the workflow file in the repository
                await CreateFileInRepositoryAsync(
                    repository.Owner,
                    repository.Name,
                    ".github/workflows/ci-cd.yml",
                    workflowContent,
                    "Add CI/CD workflow");
                
                Console.WriteLine("GitHub Actions workflow configured successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring GitHub Actions: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidateConfigurationAsync(RepositoryInfo repository)
        {
            try
            {
                // Check if repository exists
                var repo = await _client.Repository.Get(repository.Owner, repository.Name);
                
                // Check if GitHub Actions are enabled
                var workflows = await _client.Actions.Workflows.List(repository.Owner, repository.Name);
                
                return repo != null && workflows != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task CreateFileInRepositoryAsync(string owner, string repo, string path, string content, string message)
        {
            try
            {
                await _client.Repository.Content.CreateFile(
                    owner,
                    repo,
                    path,
                    new CreateFileRequest(message, content));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating file {path}: {ex.Message}");
                throw;
            }
        }

        private string GenerateWorkflowFile(ProjectConfig projectConfig)
        {
            // Generate appropriate workflow based on project template type
            if (projectConfig.TemplateType.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                // Load the workflow template from the asset file
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "github-workflow.yml");
                
                // If running from the project directory during development
                if (!File.Exists(templatePath))
                {
                    templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "github-workflow.yml");
                }
                
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"GitHub workflow template not found at {templatePath}");
                }
                
                // Read the template and replace placeholders
                string template = File.ReadAllText(templatePath);
                string workflowYaml = template.Replace("{{ProjectName}}", projectConfig.Name);
                
                return workflowYaml;
            }
            
            // For other project types, add more workflow templates
            return string.Empty;
     
        }
    }
}

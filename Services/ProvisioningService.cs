using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using hum.Models;
using hum.Providers;

namespace hum.Services
{
    public class ProvisioningService
    {
        private readonly List<IProjectTemplateProvider> _projectTemplateProviders;
        private readonly List<ISourceControlProvider> _sourceControlProviders;
        private readonly List<ICiCdProvider> _ciCdProviders;
        private readonly List<IInfrastructureProvider> _infrastructureProviders;

        public ProvisioningService(
            IEnumerable<IProjectTemplateProvider> projectTemplateProviders,
            IEnumerable<ISourceControlProvider> sourceControlProviders,
            IEnumerable<ICiCdProvider> ciCdProviders,
            IEnumerable<IInfrastructureProvider> infrastructureProviders)
        {
            _projectTemplateProviders = projectTemplateProviders.ToList();
            _sourceControlProviders = sourceControlProviders.ToList();
            _ciCdProviders = ciCdProviders.ToList();
            _infrastructureProviders = infrastructureProviders.ToList();
        }

        public async Task<string> ProvisionProjectAsync(ProjectConfig projectConfig)
        {
            Console.WriteLine($"Starting provisioning for project: {projectConfig.Name}");
            
            // 1. Create the project from template
            var projectPath = await CreateProjectFromTemplateAsync(projectConfig);
            
            // 2. Create and configure source control repository
            var repository = await CreateSourceControlRepositoryAsync(projectConfig);
            
            // 3. Configure CI/CD pipelines
            await ConfigureCiCdPipelinesAsync(repository, projectConfig);
            
            // 4. Configure infrastructure
            await ConfigureInfrastructureAsync(projectConfig, repository);
            
            Console.WriteLine($"Project {projectConfig.Name} provisioned successfully!");
            Console.WriteLine($"Project path: {projectPath}");
            Console.WriteLine($"Repository URL: {repository.Url}");
            
            return projectPath;
        }

        private async Task<string> CreateProjectFromTemplateAsync(ProjectConfig projectConfig)
        {
            var provider = _projectTemplateProviders.FirstOrDefault(p => p.CanHandle(projectConfig.TemplateType));
            
            if (provider == null)
            {
                throw new InvalidOperationException($"No project template provider found for type: {projectConfig.TemplateType}");
            }
            
            Console.WriteLine($"Using project template provider: {provider.TemplateName}");
            return await provider.CreateProjectAsync(projectConfig);
        }

        private async Task<RepositoryInfo> CreateSourceControlRepositoryAsync(ProjectConfig projectConfig)
        {
            var provider = _sourceControlProviders.FirstOrDefault(p => 
                p.ProviderName.Equals(projectConfig.SourceControlProvider, StringComparison.OrdinalIgnoreCase));
            
            if (provider == null)
            {
                throw new InvalidOperationException($"No source control provider found for: {projectConfig.SourceControlProvider}");
            }
            
            Console.WriteLine($"Using source control provider: {provider.ProviderName}");
            
            // Create the repository
            var repository = await provider.CreateRepositoryAsync(projectConfig.Name, projectConfig.Description);
            
            // Configure the repository
            await provider.ConfigureRepositoryAsync(repository, projectConfig);
            
            return repository;
        }

        private async Task ConfigureCiCdPipelinesAsync(RepositoryInfo repository, ProjectConfig projectConfig)
        {
            var provider = _ciCdProviders.FirstOrDefault(p => 
                p.ProviderName.Equals(projectConfig.CiCdProvider, StringComparison.OrdinalIgnoreCase));
            
            if (provider == null)
            {
                throw new InvalidOperationException($"No CI/CD provider found for: {projectConfig.CiCdProvider}");
            }
            
            Console.WriteLine($"Using CI/CD provider: {provider.ProviderName}");
            await provider.ConfigurePipelinesAsync(repository, projectConfig);
        }

        private async Task ConfigureInfrastructureAsync(ProjectConfig projectConfig, RepositoryInfo repository)
        {
            var provider = _infrastructureProviders.FirstOrDefault(p => 
                p.ProviderName.Equals(projectConfig.InfrastructureProvider, StringComparison.OrdinalIgnoreCase));
            
            if (provider == null)
            {
                throw new InvalidOperationException($"No infrastructure provider found for: {projectConfig.InfrastructureProvider}");
            }
            
            Console.WriteLine($"Using infrastructure provider: {provider.ProviderName}");
            await provider.ConfigureInfrastructureAsync(projectConfig, repository);
        }
    }
}

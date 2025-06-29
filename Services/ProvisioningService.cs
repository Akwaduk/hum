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
            // Make sure we have a template type to work with
            if (string.IsNullOrEmpty(projectConfig.TemplateType))
            {
                throw new InvalidOperationException("Template type not specified in project configuration");
            }
            
            // For dotnet templates, ensure they have the 'dotnet-' prefix if not already present
            string templateType = projectConfig.TemplateType;
            if (!templateType.StartsWith("dotnet-", StringComparison.OrdinalIgnoreCase) &&
                (templateType.Equals("webapi", StringComparison.OrdinalIgnoreCase) ||
                 templateType.Equals("blazor", StringComparison.OrdinalIgnoreCase) ||
                 templateType.Equals("blazorapp", StringComparison.OrdinalIgnoreCase) ||
                 templateType.Equals("worker", StringComparison.OrdinalIgnoreCase) ||
                 templateType.Equals("console", StringComparison.OrdinalIgnoreCase)))
            {
                templateType = "dotnet-" + templateType;
                Console.WriteLine($"Detected dotnet template, using template type: {templateType}");
                
                // Update the project config to reflect the actual template type used
                projectConfig.TemplateType = templateType;
            }
            
            // Try to find a provider that can handle this template type
            var provider = _projectTemplateProviders.FirstOrDefault(p => p.CanHandle(projectConfig.TemplateType));
            
            if (provider == null)
            {
                // List available templates for better error messaging
                Console.WriteLine("Available template providers:");
                foreach (var availableProvider in _projectTemplateProviders)
                {
                    Console.WriteLine($"- {availableProvider.TemplateName}");
                }
                
                throw new InvalidOperationException($"No project template provider found for type: {projectConfig.TemplateType}. Make sure your template type is supported.");
            }
            
            Console.WriteLine($"Using project template provider: {provider.TemplateName}");
            return await provider.CreateProjectAsync(projectConfig);
        }

        private async Task<RepositoryInfo> CreateSourceControlRepositoryAsync(ProjectConfig projectConfig)
        {
            // Ensure we have a source control provider type specified
            if (string.IsNullOrEmpty(projectConfig.SourceControlProvider))
            {
                // Default to github if not specified
                projectConfig.SourceControlProvider = "github";
                Console.WriteLine("No source control provider specified, defaulting to: github");
            }
            
            // First try by calling CanHandle
            var provider = _sourceControlProviders.FirstOrDefault(p => 
                p.CanHandle(projectConfig.SourceControlProvider!));
            
            if (provider == null)
            {
                // If that doesn't work, try by provider name
                provider = _sourceControlProviders.FirstOrDefault(p => 
                    p.ProviderName.Equals(projectConfig.SourceControlProvider, StringComparison.OrdinalIgnoreCase));
            }
            
            if (provider == null)
            {
                // Add more detailed error information
                Console.WriteLine($"Looking for source control provider for type: '{projectConfig.SourceControlProvider}'");
                Console.WriteLine("Available source control providers:");
                foreach (var availableProvider in _sourceControlProviders)
                {
                    Console.WriteLine($"- {availableProvider.ProviderName} (handles '{projectConfig.SourceControlProvider}'? {availableProvider.CanHandle(projectConfig.SourceControlProvider!)})");
                }
                
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
            // Ensure we have a CI/CD provider type specified
            if (string.IsNullOrEmpty(projectConfig.CiCdProvider))
            {
                // Default to github if not specified
                projectConfig.CiCdProvider = "github";
                Console.WriteLine("No CI/CD provider specified, defaulting to: github");
            }
            
            // Attempt to find a provider by name first
            var provider = _ciCdProviders.FirstOrDefault(p => 
                p.ProviderName.Equals(projectConfig.CiCdProvider, StringComparison.OrdinalIgnoreCase));
            
            if (provider == null)
            {
                // Otherwise try using the source control provider for CI/CD
                provider = _ciCdProviders.FirstOrDefault(p => 
                    p is ISourceControlProvider sourceControlProvider && 
                    sourceControlProvider.CanHandle(projectConfig.CiCdProvider!));
            }
            
            if (provider == null)
            {
                Console.WriteLine($"Looking for CI/CD provider for type: '{projectConfig.CiCdProvider}'");
                Console.WriteLine("Available CI/CD providers:");
                foreach (var availableProvider in _ciCdProviders)
                {
                    Console.WriteLine($"- {availableProvider.ProviderName}");
                }
                
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

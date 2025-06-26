using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using hum.Models;
using hum.Services;

namespace hum.Commands
{
    public class CreateCommand : Command
    {
        public CreateCommand() : base("create", "Create and deploy a new service with full deployment pipeline")
        {
            var nameArgument = new Argument<string>(
                "name",
                "The name of the service to create");

            var templateOption = new Option<string>(
                "--template",
                () => "dotnet-webapi",
                "The template to use (e.g., dotnet-webapi, dotnet-worker)");

            var envOption = new Option<string>(
                "--env",
                () => "staging",
                "The target environment (e.g., staging, production)");

            var hostOption = new Option<string>(
                "--host",
                "The target host server for deployment");

            var orgOption = new Option<string>(
                "--org",
                "GitHub organization (overrides config default)");

            var descriptionOption = new Option<string>(
                "--description",
                "Description of the service");

            var outputOption = new Option<string>(
                "--output",
                "The output directory for the project");

            AddArgument(nameArgument);
            AddOption(templateOption);
            AddOption(envOption);
            AddOption(hostOption);
            AddOption(orgOption);
            AddOption(descriptionOption);
            AddOption(outputOption);

            this.SetHandler(context => HandleCommand(
                context,
                nameArgument,
                templateOption,
                envOption,
                hostOption,
                orgOption,
                descriptionOption,
                outputOption
            ));
        }

        private async Task HandleCommand(
            InvocationContext context,
            Argument<string> nameArgument,
            Option<string> templateOption,
            Option<string> envOption,
            Option<string> hostOption,
            Option<string> orgOption,
            Option<string> descriptionOption,
            Option<string> outputOption)
        {
            string? name = context.ParseResult.GetValueForArgument(nameArgument);
            string? template = context.ParseResult.GetValueForOption(templateOption);
            string? environment = context.ParseResult.GetValueForOption(envOption);
            string? host = context.ParseResult.GetValueForOption(hostOption);
            string? org = context.ParseResult.GetValueForOption(orgOption);
            string? description = context.ParseResult.GetValueForOption(descriptionOption);
            string? output = context.ParseResult.GetValueForOption(outputOption);

            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Error: Service name is required.");
                return;
            }            var configService = new ConfigurationService();
            var settings = await configService.LoadSettingsAsync();

            // Check if GitHub CLI is authenticated instead of manual token
            var githubCliProvider = new Providers.GitHubCliProvider();
            bool isAuthenticated = await githubCliProvider.IsAuthenticatedAsync();
            
            if (!isAuthenticated)
            {
                Console.WriteLine("GitHub CLI not authenticated. Please run 'gh auth login' first.");
                Console.WriteLine("This will open your browser for secure OAuth authentication.");
                return;
            }

            // Use provided description or generate a default one
            string serviceDescription = description ?? $"{template} service for {name}";

            Console.WriteLine($"Creating service '{name}' using template '{template}'");
            Console.WriteLine($"Target environment: {environment}");
            if (!string.IsNullOrEmpty(host))
            {
                Console.WriteLine($"Target host: {host}");
            }

            // Create project configuration
            var projectConfig = await configService.CreateDefaultProjectConfigAsync(name, serviceDescription, template!);

            if (!string.IsNullOrEmpty(output))
            {
                projectConfig.OutputPath = output;
            }

            // Configure deployment environment
            if (projectConfig.DeploymentConfig == null)
            {
                projectConfig.DeploymentConfig = new DeploymentConfig();
            }

            // Add the target environment and host
            var deploymentEnv = new Models.Environment
            {
                Name = environment!,
                HostName = host ?? $"{name}-server",
                DeploymentPath = $"/var/www/{name}"
            };

            // Add environment-specific variables
            deploymentEnv.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment == "production" ? "Production" : "Staging";
            deploymentEnv.EnvironmentVariables["PROJECT_NAME"] = name;

            projectConfig.DeploymentConfig.Environments.Clear();
            projectConfig.DeploymentConfig.Environments.Add(deploymentEnv);

            // Set GitHub organization if provided
            if (!string.IsNullOrEmpty(org))
            {
                projectConfig.AdditionalOptions["github_org"] = org;
            }            // Create providers
            var projectTemplateProvider = new Providers.DotNetTemplateProvider();
            var sourceControlProvider = new Providers.GitHubCliProvider(); // Use GitHub CLI instead
            var infrastructureProvider = new Providers.AnsibleProvider(null);

            // Create provisioning service
            var provisioningService = new ProvisioningService(
                new[] { projectTemplateProvider },
                new[] { sourceControlProvider },
                new[] { sourceControlProvider },  // GitHub provider also implements ICiCdProvider
                new[] { infrastructureProvider }
            );

            try
            {
                // Provision the project
                string projectPath = await provisioningService.ProvisionProjectAsync(projectConfig);                Console.WriteLine();
                Console.WriteLine($"✅ Service '{name}' created successfully!");
                Console.WriteLine($"📁 Project path: {projectPath}");
                Console.WriteLine($"🔗 Repository created via GitHub CLI");
                Console.WriteLine();
                Console.WriteLine("Next steps:");
                Console.WriteLine($"1. Review the generated configuration in {projectPath}");
                Console.WriteLine("2. The repository has been created and is ready for development");
                Console.WriteLine("3. The service will be automatically deployed to the specified environment");
                Console.WriteLine();
                Console.WriteLine($"View repository: gh repo view {name} --web");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating service: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Details: {ex.InnerException.Message}");
                }
                Console.WriteLine();
                Console.WriteLine("Troubleshooting:");
                Console.WriteLine("- Verify GitHub token has repository creation permissions");
                Console.WriteLine("- Check that the template name is valid");
                Console.WriteLine("- Ensure target host is accessible for deployment");
            }
        }
    }
}

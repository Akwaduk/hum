using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using hum.Models;
using hum.Services;

namespace hum.Commands
{
    public class InitCommand : Command
    {
        public InitCommand() : base("init", "Initialize a new project with default configuration")
        {
            var nameOption = new Option<string>(
                "--name",
                "The name of the project")
            {
                IsRequired = true
            };
            
            var descriptionOption = new Option<string>(
                "--description",
                "The description of the project")
            {
                IsRequired = true
            };
            
            var templateOption = new Option<string>(
                "--template",
                () => "dotnet",
                "The template to use (e.g., dotnet, svelte)");
                
            var outputOption = new Option<string>(
                "--output",
                "The output directory for the project");
                
            var sourceControlOption = new Option<string>(
                "--source-control",
                () => "github",
                "The source control provider to use");
                
            var cicdOption = new Option<string>(
                "--cicd",
                () => "github",
                "The CI/CD provider to use");
                
            var infraOption = new Option<string>(
                "--infra",
                () => "ansible",
                "The infrastructure provider to use");
            
            AddOption(nameOption);
            AddOption(descriptionOption);
            AddOption(templateOption);
            AddOption(outputOption);
            AddOption(sourceControlOption);
            AddOption(cicdOption);
            AddOption(infraOption);
            
            // Corrected SetHandler to pass the context and option instances to the handler method.
            // Also removed the malformed HandleCommand definition that was here.
            this.SetHandler(context => HandleCommand(
                context, 
                nameOption, 
                descriptionOption, 
                templateOption, 
                outputOption, 
                sourceControlOption, 
                cicdOption, 
                infraOption
            ));
        }
        
        // Corrected HandleCommand to take Option<string> instances
        private async Task HandleCommand(
            InvocationContext context,
            Option<string> nameOption,
            Option<string> descriptionOption,
            Option<string> templateOption,
            Option<string> outputOption,
            Option<string> sourceControlOption,
            Option<string> cicdOption,
            Option<string> infraOption)
        {
            // Retrieve option values using the passed Option<string> instances
            string? name = context.ParseResult.GetValueForOption(nameOption);
            string? description = context.ParseResult.GetValueForOption(descriptionOption);
            string? template = context.ParseResult.GetValueForOption(templateOption);
            string? output = context.ParseResult.GetValueForOption(outputOption);
            string? sourceControl = context.ParseResult.GetValueForOption(sourceControlOption);
            string? cicd = context.ParseResult.GetValueForOption(cicdOption);
            string? infra = context.ParseResult.GetValueForOption(infraOption);
            
            // Added null checks for required options
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Error: Project name is required.");
                return;
            }
            if (string.IsNullOrEmpty(description))
            {
                Console.WriteLine("Error: Project description is required.");
                return;
            }
            
            var configService = new ConfigurationService();
            var settings = await configService.LoadSettingsAsync();
            
            if (string.IsNullOrEmpty(settings.GitHubToken) || string.IsNullOrEmpty(settings.GitHubUsername))
            {
                Console.WriteLine("GitHub credentials not configured. Please run 'hum config' first.");
                return;
            }
            
            // Create project configuration
            var projectConfig = await configService.CreateDefaultProjectConfigAsync(name, description, template!); // Added null-forgiving operator for template
            
            if (!string.IsNullOrEmpty(output))
            {
                projectConfig.OutputPath = output;
            }
            
            // Ensure template, sourceControl, cicd, and infra have default values if not provided
            // The Option constructors already provide default values, so direct assignment is fine.
            projectConfig.TemplateType = template!;
            projectConfig.SourceControlProvider = sourceControl!;
            projectConfig.CiCdProvider = cicd!;
            projectConfig.InfrastructureProvider = infra!;
            
            // Create providers
            var projectTemplateProvider = new Providers.DotNetTemplateProvider();
            var sourceControlProvider = new Providers.GitHubProvider(settings.GitHubToken, settings.GitHubUsername);
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
                string projectPath = await provisioningService.ProvisionProjectAsync(projectConfig);
                
                Console.WriteLine($"Project {name} provisioned successfully!");
                Console.WriteLine($"Project path: {projectPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error provisioning project: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}

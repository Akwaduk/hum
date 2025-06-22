using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using hum.Models;
using hum.Services;
// Add fully qualified using for System.Environment to resolve ambiguity
using SystemEnvironment = System.Environment;

namespace hum.Commands
{
    public class TemplateCommand : Command
    {
        public TemplateCommand() : base("template", "Manage project templates")
        {
            // Create subcommands
            var saveCommand = new Command("save", "Save a project configuration as a template");
            var nameOptionSave = new Option<string>(
                "--name",
                "The name of the template")
            {
                IsRequired = true
            };
            var projectPathOptionSave = new Option<string>(
                "--project-path",
                "Path to the project to save as a template")
            {
                IsRequired = true
            };
            saveCommand.AddOption(nameOptionSave);
            saveCommand.AddOption(projectPathOptionSave);
            
            var listCommand = new Command("list", "List available templates");
            
            var useCommand = new Command("use", "Use a template to create a new project");
            var nameOptionUse = new Option<string>(
                "--name",
                "The name of the template to use")
            {
                IsRequired = true
            };
            var projectNameOptionUse = new Option<string>(
                "--project-name",
                "The name of the new project")
            {
                IsRequired = true
            };
            var descriptionOptionUse = new Option<string>(
                "--description",
                "The description of the new project")
            {
                IsRequired = true
            };
            var outputOptionUse = new Option<string>(
                "--output",
                "The output directory for the new project");
            useCommand.AddOption(nameOptionUse);
            useCommand.AddOption(projectNameOptionUse);
            useCommand.AddOption(descriptionOptionUse);
            useCommand.AddOption(outputOptionUse);
            
            saveCommand.SetHandler(context => HandleSaveCommand(context, nameOptionSave, projectPathOptionSave));
            listCommand.SetHandler(HandleListCommand);
            useCommand.SetHandler(context => HandleUseCommand(context, nameOptionUse, projectNameOptionUse, descriptionOptionUse, outputOptionUse));
            
            AddCommand(saveCommand);
            AddCommand(listCommand);
            AddCommand(useCommand);
        }
        
        private async Task HandleSaveCommand(InvocationContext context, Option<string> nameOption, Option<string> projectPathOption)
        {
            string? name = context.ParseResult.GetValueForOption(nameOption);
            string? projectPath = context.ParseResult.GetValueForOption(projectPathOption);
            
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("Error: Template name is required.");
                return;
            }
            if (string.IsNullOrEmpty(projectPath))
            {
                Console.WriteLine("Error: Project path is required.");
                return;
            }
            
            if (!Directory.Exists(projectPath))
            {
                Console.WriteLine($"Project directory not found: {projectPath}");
                return;
            }
            
            var configService = new ConfigurationService();
            
            try
            {
                var settings = await configService.LoadSettingsAsync(); // Load settings once

                // Create a basic project config from the project
                var projectConfig = new ProjectConfig
                {
                    Name = Path.GetFileName(projectPath),
                    Description = $"Template created from {Path.GetFileName(projectPath)}",
                    TemplateType = "dotnet", // Assuming dotnet for now
                    SourceControlProvider = "GitHub",
                    CiCdProvider = "GitHub",
                    InfrastructureProvider = "Ansible",
                    GitConfig = settings.DefaultGitConfig, // Use loaded settings
                    DeploymentConfig = new DeploymentConfig(),
                    OutputPath = projectPath // Assuming OutputPath should store the original project's path
                };
                
                // Save the template
                await configService.SaveProjectTemplateAsync(name, projectConfig);
                Console.WriteLine($"Template '{name}' saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving template: {ex.Message}");
            }
        }
        
        private async Task HandleListCommand(InvocationContext context)
        {
            var configService = new ConfigurationService();
            string templatesDir = Path.Combine(
                SystemEnvironment.GetFolderPath(SystemEnvironment.SpecialFolder.UserProfile), // Disambiguated Environment
                ".hum",
                "templates");
                
            if (!Directory.Exists(templatesDir))
            {
                Console.WriteLine("No templates found.");
                return;
            }
            
            var templateFiles = Directory.GetFiles(templatesDir, "*.json");
            
            if (templateFiles.Length == 0)
            {
                Console.WriteLine("No templates found.");
                return;
            }
            
            Console.WriteLine("Available templates:");
            foreach (var file in templateFiles)
            {
                string templateName = Path.GetFileNameWithoutExtension(file);
                try
                {
                    var template = await configService.LoadProjectTemplateAsync(templateName);
                    Console.WriteLine($"- {templateName}: {template.Description} ({template.TemplateType})");
                }
                catch
                {
                    Console.WriteLine($"- {templateName}: [Error loading template]");
                }
            }
        }
        
        private async Task HandleUseCommand(InvocationContext context, Option<string> nameOption, Option<string> projectNameOption, Option<string> descriptionOption, Option<string> outputOption)
        {
            string? templateName = context.ParseResult.GetValueForOption(nameOption);
            string? projectName = context.ParseResult.GetValueForOption(projectNameOption);
            string? description = context.ParseResult.GetValueForOption(descriptionOption);
            string? output = context.ParseResult.GetValueForOption(outputOption);
            
            // Add null checks for required options
            if (string.IsNullOrEmpty(templateName))
            {
                Console.WriteLine("Error: Template name is required.");
                return;
            }
            if (string.IsNullOrEmpty(projectName))
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
            
            try
            {
                // Load the template
                var templateConfig = await configService.LoadProjectTemplateAsync(templateName);
                
                // Update with new project details
                templateConfig.Name = projectName;
                templateConfig.Description = description;
                
                if (!string.IsNullOrEmpty(output))
                {
                    templateConfig.OutputPath = output;
                }
                else
                {
                    templateConfig.OutputPath = Path.Combine(SystemEnvironment.CurrentDirectory, projectName); // Disambiguated Environment
                }
                
                // Create providers
                var projectTemplateProvider = new Providers.DotNetTemplateProvider();
                var sourceControlProvider = new Providers.GitHubProvider(settings.GitHubToken, settings.GitHubUsername);
                var infrastructureProvider = new Providers.AnsibleProvider(null);
                
                // Create provisioning service
                var provisioningService = new Services.ProvisioningService(
                    new[] { projectTemplateProvider },
                    new[] { sourceControlProvider },
                    new[] { sourceControlProvider },  // GitHub provider also implements ICiCdProvider
                    new[] { infrastructureProvider }
                );
                
                // Provision the project
                string projectPath = await provisioningService.ProvisionProjectAsync(templateConfig);
                
                Console.WriteLine($"Project {projectName} created from template '{templateName}' successfully!");
                Console.WriteLine($"Project path: {projectPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error using template: {ex.Message}");
            }
        }
    }
}

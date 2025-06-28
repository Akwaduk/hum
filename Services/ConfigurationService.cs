using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using hum.Models;

namespace hum.Services
{
    public class ConfigurationService
    {
        public string ConfigDirectory { get; }
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigurationService(string? configDirectory = null) // Made configDirectory nullable
        {
            ConfigDirectory = configDirectory ?? Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                ".hum");
                
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // Ensure config directory exists
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            string settingsPath = Path.Combine(ConfigDirectory, "settings.json");
            
            if (!File.Exists(settingsPath))
            {
                // Create default settings
                var defaultSettings = new AppSettings
                {
                    GitHubToken = "",
                    GitHubUsername = "",
                    DefaultGitConfig = new GitConfig
                    {
                        Username = "",
                        Email = "",
                        DefaultBranch = "main",
                        InitializeWithReadme = true,
                        IgnoreTemplate = "dotnet"
                    },
                    DefaultDeploymentConfig = new DeploymentConfig(),
                    AnsibleConfig = new AnsibleConfig()
                };
                
                await SaveSettingsAsync(defaultSettings);
                return defaultSettings;
            }
            
            try
            {
                string json = await File.ReadAllTextAsync(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
                if (settings.AnsibleConfig == null)
                {
                    settings.AnsibleConfig = new AnsibleConfig();
                }
                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return new AppSettings { AnsibleConfig = new AnsibleConfig() };
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            string settingsPath = Path.Combine(ConfigDirectory, "settings.json");
            
            try
            {
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                await File.WriteAllTextAsync(settingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                throw;
            }
        }

        public async Task<ProjectConfig> LoadProjectTemplateAsync(string templateName)
        {
            string templatesDir = Path.Combine(ConfigDirectory, "templates");
            
            if (!Directory.Exists(templatesDir))
            {
                Directory.CreateDirectory(templatesDir);
            }
            
            string templatePath = Path.Combine(templatesDir, $"{templateName}.json");
            
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template not found: {templateName}");
            }
            
            try
            {
                string json = await File.ReadAllTextAsync(templatePath);
                return JsonSerializer.Deserialize<ProjectConfig>(json, _jsonOptions) ?? throw new System.Text.Json.JsonException($"Failed to deserialize template {templateName} or it was empty.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading template: {ex.Message}");
                throw;
            }
        }

        public async Task SaveProjectTemplateAsync(string templateName, ProjectConfig config)
        {
            string templatesDir = Path.Combine(ConfigDirectory, "templates");
            
            if (!Directory.Exists(templatesDir))
            {
                Directory.CreateDirectory(templatesDir);
            }
            
            string templatePath = Path.Combine(templatesDir, $"{templateName}.json");
            
            try
            {
                string json = JsonSerializer.Serialize(config, _jsonOptions);
                await File.WriteAllTextAsync(templatePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving template: {ex.Message}");
                throw;
            }
        }

        public async Task<ProjectConfig> CreateDefaultProjectConfigAsync(string name, string description, string templateType)
        {
            var settings = await LoadSettingsAsync();
            
            return new ProjectConfig
            {
                Name = name,
                Description = description,
                TemplateType = templateType,
                OutputPath = Path.Combine(System.Environment.CurrentDirectory, name),
                SourceControlProvider = "GitHub",
                CiCdProvider = "GitHub",
                InfrastructureProvider = "Ansible",
                GitConfig = settings.DefaultGitConfig,
                DeploymentConfig = settings.DefaultDeploymentConfig,
                AnsibleConfig = settings.AnsibleConfig
            };
        }
    }

    public class AppSettings
    {
        public string? GitHubToken { get; set; } // Made nullable
        public string? GitHubUsername { get; set; } // Made nullable
        public GitConfig? DefaultGitConfig { get; set; } // Made nullable
        public DeploymentConfig? DefaultDeploymentConfig { get; set; } // Made nullable
        public AnsibleConfig? AnsibleConfig { get; set; }
    }
}

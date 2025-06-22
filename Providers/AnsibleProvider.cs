using System;
using System.IO;
using System.Threading.Tasks;
using hum.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace hum.Providers
{
    public class AnsibleProvider : IInfrastructureProvider
    {
        private readonly string _ansiblePath;
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public AnsibleProvider(string? ansiblePath) // Changed string to string?
        {
            _ansiblePath = ansiblePath ?? Path.Combine(System.Environment.CurrentDirectory, "ansible");
            
            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
                
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public string ProviderName => "Ansible";

        public async Task ConfigureInfrastructureAsync(ProjectConfig projectConfig, RepositoryInfo repositoryInfo)
        {
            Console.WriteLine($"Configuring Ansible infrastructure for project: {projectConfig.Name}");
            
            // Ensure Ansible directory exists
            EnsureAnsibleDirectoryStructure();
            
            // Create inventory file
            await UpdateInventoryAsync(projectConfig, repositoryInfo);
            
            // Create playbook
            await CreatePlaybookAsync(projectConfig);
            
            Console.WriteLine("Ansible infrastructure configured successfully");
        }

        public async Task UpdateInventoryAsync(ProjectConfig projectConfig, RepositoryInfo repositoryInfo)
        {
            Console.WriteLine($"Updating Ansible inventory for project: {projectConfig.Name}");
            
            var inventoryPath = Path.Combine(_ansiblePath, "inventory.yml");
            var inventory = new AnsibleInventory();
            
            // Try to load existing inventory if it exists
            if (File.Exists(inventoryPath))
            {
                try
                {
                    var existingInventory = await File.ReadAllTextAsync(inventoryPath);
                    inventory = _deserializer.Deserialize<AnsibleInventory>(existingInventory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load existing inventory: {ex.Message}");
                    // Continue with new inventory
                }
            }
            
            // Add or update project in inventory
            if (projectConfig.DeploymentConfig != null)
            {
                foreach (var env in projectConfig.DeploymentConfig.Environments)
                {
                    var hostGroup = inventory.GetOrCreateGroup(env.Name);
                    var host = hostGroup.GetOrCreateHost(env.HostName);
                    
                    // Set variables for this host
                    host.Variables["project_name"] = projectConfig.Name;
                    host.Variables["deploy_path"] = env.DeploymentPath;
                    host.Variables["repository_url"] = repositoryInfo.CloneUrl;
                    
                    // Add any environment-specific variables
                    foreach (var kvp in env.EnvironmentVariables)
                    {
                        host.Variables[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            // Save inventory
            var inventoryYaml = _serializer.Serialize(inventory);
            await File.WriteAllTextAsync(inventoryPath, inventoryYaml);
            
            Console.WriteLine($"Ansible inventory updated at {inventoryPath}");
        }

        public Task<bool> ValidateConfigurationAsync(ProjectConfig projectConfig)
        {
            try
            {
                var inventoryPath = Path.Combine(_ansiblePath, "inventory.yml");
                var playbookPath = Path.Combine(_ansiblePath, "deploy.yml");
                
                return Task.FromResult(File.Exists(inventoryPath) && File.Exists(playbookPath));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private void EnsureAnsibleDirectoryStructure()
        {
            // Create main Ansible directory
            if (!Directory.Exists(_ansiblePath))
            {
                Directory.CreateDirectory(_ansiblePath);
            }
            
            // Create roles directory
            var rolesPath = Path.Combine(_ansiblePath, "roles");
            if (!Directory.Exists(rolesPath))
            {
                Directory.CreateDirectory(rolesPath);
                
                // Create common roles
                CreateRoleStructure(Path.Combine(rolesPath, "common"));
                CreateRoleStructure(Path.Combine(rolesPath, "web"));
                CreateRoleStructure(Path.Combine(rolesPath, "dotnet"));
            }
        }

        private void CreateRoleStructure(string rolePath)
        {
            if (!Directory.Exists(rolePath))
            {
                Directory.CreateDirectory(rolePath);
                
                // Create standard Ansible role directories
                Directory.CreateDirectory(Path.Combine(rolePath, "tasks"));
                Directory.CreateDirectory(Path.Combine(rolePath, "handlers"));
                Directory.CreateDirectory(Path.Combine(rolePath, "templates"));
                Directory.CreateDirectory(Path.Combine(rolePath, "files"));
                Directory.CreateDirectory(Path.Combine(rolePath, "vars"));
                Directory.CreateDirectory(Path.Combine(rolePath, "defaults"));
                Directory.CreateDirectory(Path.Combine(rolePath, "meta"));
                
                // Create main.yml in tasks
                File.WriteAllText(
                    Path.Combine(rolePath, "tasks", "main.yml"),
                    "---\n# Tasks for role " + Path.GetFileName(rolePath) + "\n");
            }
        }

        private async Task CreatePlaybookAsync(ProjectConfig projectConfig)
        {
            var playbookPath = Path.Combine(_ansiblePath, "deploy.yml");
            
            // Load the playbook template from the asset file
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ansible-playbook.yml");
            
            // If running from the project directory during development
            if (!File.Exists(templatePath))
            {
                templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "ansible-playbook.yml");
            }
            
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Ansible playbook template not found at {templatePath}");
            }
            
            // Read the template and replace placeholders
            string template = await File.ReadAllTextAsync(templatePath);
            string playbookContent = template.Replace("{{ProjectName}}", projectConfig.Name);
            
            await File.WriteAllTextAsync(playbookPath, playbookContent);
            Console.WriteLine($"Created Ansible playbook at {playbookPath}");
        }
    }

    // Helper classes for Ansible inventory (changed to internal)
    internal class AnsibleInventory
    {
        public AnsibleInventory()
        {
            All = new HostGroup { Name = "all" };
        }
        
        public HostGroup All { get; set; }
        
        public HostGroup GetOrCreateGroup(string name)
        {
            if (All.Children == null)
            {
                All.Children = new System.Collections.Generic.Dictionary<string, HostGroup>();
            }
            
            if (!All.Children.ContainsKey(name))
            {
                All.Children[name] = new HostGroup { Name = name };
            }
            
            return All.Children[name];
        }
    }

    internal class HostGroup
    {
        public required string Name { get; set; }
        public System.Collections.Generic.Dictionary<string, HostGroup> Children { get; set; } = new System.Collections.Generic.Dictionary<string, HostGroup>();
        public System.Collections.Generic.Dictionary<string, Host> Hosts { get; set; } = new System.Collections.Generic.Dictionary<string, Host>();
        
        public Host GetOrCreateHost(string hostname)
        {
            if (Hosts == null)
            {
                Hosts = new System.Collections.Generic.Dictionary<string, Host>();
            }
            
            if (!Hosts.ContainsKey(hostname))
            {
                Hosts[hostname] = new Host { Name = hostname };
            }
            
            return Hosts[hostname];
        }
    }

    internal class Host
    {
        public Host()
        {
            Variables = new System.Collections.Generic.Dictionary<string, object>();
        }
        
        public required string Name { get; set; }
        public System.Collections.Generic.Dictionary<string, object> Variables { get; set; }
    }

    internal class Inventory // Changed from private to internal
    {
        public required InventoryGroup All { get; set; }
    }

    internal class InventoryGroup // Changed from private to internal
    {
        public required Dictionary<string, InventoryHostGroup> Children { get; set; }
    }

    internal class InventoryHostGroup // Changed from private to internal
    {
        public required Dictionary<string, HostVars> Hosts { get; set; }
    }

    internal class HostVars // Changed from private to internal
    {
        public required string ansible_user { get; set; }
    }
}

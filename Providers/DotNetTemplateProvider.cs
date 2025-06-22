using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using hum.Models;

namespace hum.Providers
{
    public class DotNetTemplateProvider : IProjectTemplateProvider
    {
        public string TemplateName => "DotNet";
        
        public bool CanHandle(string templateType)
        {
            return templateType.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        }
        
        public async Task<string> CreateProjectAsync(ProjectConfig projectConfig)
        {
            Console.WriteLine($"Creating .NET project: {projectConfig.Name}");
            
            string projectPath = projectConfig.OutputPath ?? Path.Combine(System.Environment.CurrentDirectory, projectConfig.Name);
            
            // Create the directory if it doesn't exist
            if (!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
            }
            
            // Run dotnet new command to create a new project
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new web -n {projectConfig.Name} -o {projectPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var process = new Process
            {
                StartInfo = processStartInfo
            };
            
            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Failed to create .NET project: {error}");
                }
                
                Console.WriteLine($"Created .NET project at {projectPath}");
                Console.WriteLine(output);
                
                // Configure the project
                await ConfigureProjectAsync(projectPath, projectConfig);
                
                return projectPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating .NET project: {ex.Message}");
                throw;
            }
        }
        
        public async Task ConfigureProjectAsync(string projectPath, ProjectConfig projectConfig)
        {
            Console.WriteLine($"Configuring .NET project at {projectPath}");
            
            // Add a README.md file
            await CreateReadmeAsync(projectPath, projectConfig);
            
            // Add a .gitignore file
            await CreateGitIgnoreAsync(projectPath);
            
            // Add Dockerfile
            await CreateDockerfileAsync(projectPath, projectConfig);
            
            // Create Ansible directory structure
            await CreateAnsibleDirectoryAsync(projectPath, projectConfig);
            
            Console.WriteLine($".NET project configured successfully");
        }
        
        private async Task CreateReadmeAsync(string projectPath, ProjectConfig projectConfig)
        {
            string readmePath = Path.Combine(projectPath, "README.md");
            string content = $@"# {projectConfig.Name}

{projectConfig.Description}

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

- .NET 6.0 SDK or later
- Git

### Installing

1. Clone the repository
   ```
   git clone {projectConfig.GitConfig?.Username}/{projectConfig.Name}.git
   cd {projectConfig.Name}
   ```

2. Build the project
   ```
   dotnet build
   ```

3. Run the project
   ```
   dotnet run
   ```

## Deployment

This project is configured to deploy using Ansible. See the `ansible` directory for deployment configuration.

## Built With

* [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core) - The web framework used
* [GitHub Actions](https://github.com/features/actions) - CI/CD Pipeline
* [Ansible](https://www.ansible.com/) - Deployment automation

## License

This project is licensed under the MIT License - see the LICENSE file for details
";
            
            await File.WriteAllTextAsync(readmePath, content);
            Console.WriteLine($"Created README.md at {readmePath}");
        }
        
        private async Task CreateGitIgnoreAsync(string projectPath)
        {
            string gitignorePath = Path.Combine(projectPath, ".gitignore");
            string content = @"# .NET Core
bin/
obj/
*.user
*.userosscache
*.suo
*.userprefs
.vs/
.vscode/
*.swp
*.*~
project.lock.json
project.fragment.lock.json
artifacts/
**/Properties/launchSettings.json

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
build/
bld/
[Bb]in/
[Oo]bj/
[Oo]ut/
msbuild.log
msbuild.err
msbuild.wrn

# Visual Studio
.vs/
.vscode/
*.nupkg
*.pfx
*.snk

# Rider
.idea/
*.sln.iml

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Logs
logs/
*.log
npm-debug.log*

# Dependency directories
node_modules/
jspm_packages/

# Optional npm cache directory
.npm

# Optional eslint cache
.eslintcache

# Optional REPL history
.node_repl_history

# Output of 'npm pack'
*.tgz

# dotenv environment variable files
.env
.env.local
.env.development.local
.env.test.local
.env.production.local

# Publish directory
publish/
";
            
            await File.WriteAllTextAsync(gitignorePath, content);
            Console.WriteLine($"Created .gitignore at {gitignorePath}");
        }
        
        private async Task CreateDockerfileAsync(string projectPath, ProjectConfig projectConfig)
        {
            string dockerfilePath = Path.Combine(projectPath, "Dockerfile");
            string content = $@"FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY [""{projectConfig.Name}.csproj"", ""{projectConfig.Name}/""]
RUN dotnet restore ""{projectConfig.Name}/{projectConfig.Name}.csproj""
COPY . ""{projectConfig.Name}/""
WORKDIR ""/src/{projectConfig.Name}""
RUN dotnet build ""{projectConfig.Name}.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""{projectConfig.Name}.csproj"" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""{projectConfig.Name}.dll""]
";
            
            await File.WriteAllTextAsync(dockerfilePath, content);
            Console.WriteLine($"Created Dockerfile at {dockerfilePath}");
        }
        
        private async Task CreateAnsibleDirectoryAsync(string projectPath, ProjectConfig projectConfig)
        {
            string ansiblePath = Path.Combine(projectPath, "ansible");
            
            if (!Directory.Exists(ansiblePath))
            {
                Directory.CreateDirectory(ansiblePath);
            }
            
            // Create a basic inventory file
            string inventoryPath = Path.Combine(ansiblePath, "inventory.yml");
            string inventoryContent = @"---
all:
  children:
    production:
      hosts:
        example.com:
          ansible_user: deploy
          deploy_path: /var/www
          project_name: " + projectConfig.Name + @"
    staging:
      hosts:
        staging.example.com:
          ansible_user: deploy
          deploy_path: /var/www
          project_name: " + projectConfig.Name + @"
";
            
            await File.WriteAllTextAsync(inventoryPath, inventoryContent);
            
            // Create a basic playbook
            string playbookPath = Path.Combine(ansiblePath, "deploy.yml");
            string playbookContent = @"---
# Deployment playbook
- name: Deploy web application
  hosts: {{ target_env | default('production') }}
  become: yes
  vars:
    project_name: " + projectConfig.Name + @"
    
  tasks:
    - name: Ensure .NET runtime is installed
      apt:
        name: aspnetcore-runtime-6.0
        state: present
        
    - name: Clone or update repository
      git:
        repo: ""{{ repository_url }}""
        dest: ""{{ deploy_path }}/{{ project_name }}""
        version: main
        
    - name: Build and publish application
      shell: |
        cd ""{{ deploy_path }}/{{ project_name }}""
        dotnet restore
        dotnet publish -c Release -o ./publish
      
    - name: Create systemd service file
      template:
        src: templates/service.j2
        dest: /etc/systemd/system/{{ project_name }}.service
      notify: restart service
        
  handlers:
    - name: restart service
      systemd:
        name: ""{{ project_name }}""
        state: restarted
        daemon_reload: yes
        enabled: yes
";
            
            await File.WriteAllTextAsync(playbookPath, playbookContent);
            
            // Create templates directory
            string templatesPath = Path.Combine(ansiblePath, "templates");
            if (!Directory.Exists(templatesPath))
            {
                Directory.CreateDirectory(templatesPath);
            }
            
            // Create service template
            string servicePath = Path.Combine(templatesPath, "service.j2");
            string serviceContent = @"[Unit]
Description={{ project_name }} service
After=network.target

[Service]
WorkingDirectory={{ deploy_path }}/{{ project_name }}/publish
ExecStart=/usr/bin/dotnet {{ deploy_path }}/{{ project_name }}/publish/{{ project_name }}.dll
Restart=always
RestartSec=10
SyslogIdentifier={{ project_name }}
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
";
            
            await File.WriteAllTextAsync(servicePath, serviceContent);
            
            Console.WriteLine($"Created Ansible configuration at {ansiblePath}");
        }
    }
}

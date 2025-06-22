# Hum

A CLI tool for provisioning and managing web applications with integrated source control, CI/CD pipelines, and infrastructure configuration.

## Features

- Create new web applications from templates (currently .NET, with extensibility for others like Svelte)
- Provision GitHub repositories with proper configuration
- Set up CI/CD pipelines using GitHub Actions
- Configure infrastructure using Ansible
- Save and reuse project templates

## Architecture

Hum is built with a modular provider-based architecture that allows for easy extension:

- **Source Control Providers**: Manage repository creation and configuration (GitHub implementation provided)
- **CI/CD Providers**: Set up continuous integration and deployment pipelines (GitHub Actions implementation provided)
- **Infrastructure Providers**: Configure deployment infrastructure (Ansible implementation provided)
- **Project Template Providers**: Create and configure different types of web applications (.NET implementation provided)

## Installation

### Prerequisites

- .NET 6.0 SDK or later
- Git

### Building from Source

```bash
git clone https://github.com/yourusername/hum.git
cd hum
dotnet build
```

## Usage

### Configuration

Before using Hum, you need to configure it with your GitHub credentials:

```bash
dotnet run -- config --github-username your-username --github-token your-token --git-username "Your Name" --git-email "your.email@example.com"
```

To view your current configuration:

```bash
dotnet run -- config --show
```

### Creating a New Project

To create a new project with default settings:

```bash
dotnet run -- init --name MyProject --description "My awesome project"
```

Additional options:

```bash
dotnet run -- init --name MyProject --description "My awesome project" --template dotnet --output C:\Projects\MyProject
```

### Working with Templates

Save a project configuration as a template:

```bash
dotnet run -- template save --name my-template --project-path C:\Projects\MyProject
```

List available templates:

```bash
dotnet run -- template list
```

Create a new project from a template:

```bash
dotnet run -- template use --name my-template --project-name NewProject --description "New project from template"
```

## Extending Hum

Hum is designed to be extensible. To add support for new providers:

1. Implement the appropriate provider interface:
   - `ISourceControlProvider` for new source control systems
   - `ICiCdProvider` for new CI/CD systems
   - `IInfrastructureProvider` for new infrastructure management tools
   - `IProjectTemplateProvider` for new project types

2. Register your provider in the appropriate service

## License

MIT

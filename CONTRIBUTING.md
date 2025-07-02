# Contributing to hum

Welcome! **hum** is a global .NET CLI tool for bootstrapping applications and deployment pipelines. It streamlines the process from initial idea to production deployment by automating repository creation, CI/CD setup, and infrastructure provisioning.

## Prerequisites

- **.NET SDK ≥ 9.0** - For building and running the tool
- **GitHub CLI (`gh`)** - For repository and CI/CD operations  
- **Ansible 2.15+** - For infrastructure provisioning and configuration
- **Recommended VS Code Extensions**
  - **ms-dotnettools.csharp** - C# language support
  - **DavidAnson.vscode-markdownlint** - Markdown linting
  - **redhat.vscode-yaml** - YAML support for Ansible files

## Setting up your dev environment

```bash
# Clone the repository
git clone https://github.com/organization/hum.git
cd hum

# Restore dependencies
dotnet restore

# Run the tests
dotnet test

# Optional: Run end-to-end tests (requires Ansible)
dotnet test --filter "Category=E2E"
```

## GitHub Workflow for Contributors

1. **Fork the Repository**
   ```bash
   gh repo fork organization/hum --clone=true --remote=true
   cd hum
   ```

2. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make Your Changes**
   - Write code, tests, and documentation
   - Follow the code style guidelines
   - Run tests locally to ensure everything passes

4. **Commit Your Changes**
   - Use **Conventional Commits** format:
     ```bash
     git commit -m "feat: add new template for Flask applications"
     git commit -m "fix: resolve issue with GitHub authentication"
     git commit -m "docs: improve installation instructions"
     ```

5. **Stay in Sync with Upstream**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

6. **Push Changes to Your Fork**
   ```bash
   git push -u origin feature/your-feature-name
   ```

7. **Create a Pull Request**
   ```bash
   gh pr create --title "feat: add new template for Flask applications" --body "Description of the changes and why they're needed"
   ```

## Running the CLI from Source

```bash
# Run the CLI with arguments
dotnet run -- doctor

# Get help for a specific command
dotnet run -- create --help
```

You can also install your development version globally:

```bash
# Package and install locally
dotnet pack --configuration Release --output ./nupkg
dotnet tool install -g --add-source ./nupkg hum
```

## Setting Up GitHub Authentication

The `hum` tool requires GitHub authentication for creating repositories and managing workflows. Follow these steps to set up your credentials:

1. **Generate a Personal Access Token (PAT)**
   - Visit: [GitHub Settings > Developer Settings > Personal Access Tokens](https://github.com/settings/tokens)
   - Click "Generate new token" and select "Fine-grained token"
   - Grant permissions: `repo` (full control), `workflow` (for Actions), and `admin:org` (for organization repos)
   - Copy the generated token

2. **Configure GitHub CLI**
   ```bash
   # Authenticate with GitHub CLI
   gh auth login
   
   # Verify authentication
   gh auth status
   ```

3. **Configure hum with GitHub credentials**
   ```bash
   # Set credentials in hum's config
   hum config --github-username your-username --github-token your-pat-token
   
   # Verify configuration
   hum config --show
   ```

## Ansible Configuration

Ansible is used in two ways within hum:

1. **Template Generation**: The tool generates Ansible playbooks and configuration files within your project templates, which are intended for deploying applications.

2. **Remote Orchestration**: Optionally, hum can communicate with a remote Ansible server to manage deployments.

### Ansible Requirements

- **For local development of hum**: Ansible is only required if you want to run end-to-end (E2E) tests.
- **For users of your generated projects**: Ansible is required only when they want to use the deployment features.

### Windows-Specific Ansible Setup

Since Ansible doesn't have an official winget package, you have two options:

**Option 1: Install via WSL2 (recommended)**
```bash
# Install WSL2 if not already installed
wsl --install -d Ubuntu

# Inside WSL2 Ubuntu terminal
sudo apt update
sudo apt install -y ansible
```

**Option 2: Install via pip (Windows native)**
```bash
# Ensure Python is installed
python --version

# Install Ansible via pip
pip install ansible
```

## Code Style and Standards

- Target C# 10, .NET 9 conventions.  
- An `.editorconfig` is committed.  
- Run `dotnet format` before each commit:
   ```bash
   dotnet format
   ```

## Testing

### Unit Tests & Coverage

```bash
dotnet test /p:CollectCoverage=true
```
- Coverage threshold: **90%**.

### End-to-End Tests

```bash
# Run E2E tests (requires Ansible)
$env:HUM_E2E_SKIP=0
dotnet test --filter "Category=E2E"

# Skip E2E tests
$env:HUM_E2E_SKIP=1
```

## Conventional Commit Message Guide

| Type      | Description               |
|-----------|---------------------------|
| feat      | ✨ New feature             |
| fix       | 🐛 Bug fix                |
| docs      | 📝 Documentation only     |
| ci        | ⚙️ CI configuration       |
| refactor  | ♻️ Code restructuring     |
| chore     | 🔧 Maintenance tasks      |

## Code of Conduct

Be respectful and inclusive. See [CODE_OF_CONDUCT.md](docs/CODE_OF_CONDUCT.md).

## License

This project is licensed under MIT. See [LICENSE](LICENSE).

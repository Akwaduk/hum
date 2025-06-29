# Contributing to hum

Welcome! **hum** is a global .NET CLI tool for bootstrapping applications and deployment pipelines. It streamlines the process from initial idea to production deployment by automating repository creation, CI/CD setup, and infrastructure provisioning.

## Prerequisites

- .NET SDK ≥ 9.0  
- GitHub CLI (`gh`)  
- Ansible 2.15+  
- Recommended VS Code extensions:  
  - **ms-dotnettools.csharp**  
  - **DavidAnson.vscode-markdownlint**  
  - **redhat.vscode-yaml**  

## Setting up your dev environment

```powershell
git clone https://github.com/akwaduk/hum.git
cd hum
dotnet restore       # instead of 'make bootstrap'
dotnet test          # runs unit tests
dotnet run -- doctor # verifies your environment setup
```

## Branch & PR Workflow

- **Fork** the repo (unless you have direct write access), then **branch** from `main`.  
- Use **Conventional Commits** (`feat:`, `fix:`, `docs:`, etc.).  
- Rebase your branch onto the latest `main` before opening a PR.  
- Keep each PR focused on one logical change.

## Running the CLI from Source

```powershell
# Run the CLI with arguments
dotnet run -- doctor

# Get help for a specific command
dotnet run -- create --help
```

You can also install your development version globally:

```powershell
dotnet pack --configuration Release --output ./nupkg
dotnet tool install -g --add-source ./nupkg hum
```

## Code Style

- Target C# 10, .NET 9 conventions.  
- An `.editorconfig` is committed.  
- Run `dotnet format` before each commit.

## Unit Tests & Coverage

```powershell
dotnet test /p:CollectCoverage=true
```
- Coverage threshold: **90%**.

## End-to-End Smoke Tests

- Requires Ansible/AWX credentials.  
- Skip by setting `$env:HUM_E2E_SKIP=1` in PowerShell.
- You can run E2E tests with:
  ```powershell
  # Set environment and run e2e tests
  $env:HUM_E2E_SKIP=0
  dotnet test --filter "Category=E2E"
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

# Contributing to hum

Welcome! **hum** is a global .NET CLI tool for bootstrapping applications and deployment pipelines. It streamlines the process from initial idea to production deployment by automating repository creation, CI/CD setup, and infrastructure provisioning.

## Prerequisites

- .NET SDK ≥ 9.0  
- `gh` CLI  
- Ansible 2.15+  
- Recommended VS Code extensions:  
  - **ms-dotnettools.csharp**  
  - **DavidAnson.vscode-markdownlint**  
  - **redhat.vscode-yaml**  

## Setting up your dev environment

```bash
git clone <repo>
cd hum
make bootstrap          # or 'dotnet restore'
make test               # runs unit tests
make e2e                # optional Ansible-driven smoke tests
```

## Branch & PR Workflow

- **Fork** the repo (unless you have direct write access), then **branch** from `main`.  
- Use **Conventional Commits** (`feat:`, `fix:`, `docs:`, etc.).  
- Rebase your branch onto the latest `main` before opening a PR.  
- Keep each PR focused on one logical change.

## Running the CLI from Source

```bash
# Run the CLI with arguments
dotnet run -- doctor

# Get help for a specific command
dotnet run -- create --help
```

You can also install your development version globally:

```bash
dotnet pack --configuration Release --output ./nupkg
dotnet tool install -g --add-source ./nupkg hum
```

## Code Style

- Target C# 10, .NET 9 conventions.  
- An `.editorconfig` is committed.  
- Run `dotnet format` before each commit.

## Unit Tests & Coverage

```bash
dotnet test /p:CollectCoverage=true
```
- Coverage threshold: **90%**.

## End-to-End Smoke Tests

- Requires Ansible/AWX credentials.  
- Skip by setting `$env:HUM_E2E_SKIP=1` in PowerShell.
- You can run E2E tests with:
  ```bash
  # Set environment and run e2e tests
  export HUM_E2E_SKIP=0
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

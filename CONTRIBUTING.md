# Contributing to hum

Welcome! **hum** is a global .NET CLI tool that bootstraps new services end-to-end—GitHub repo, CI/CD via GitHub Actions, and Ansible inventory—so you can go from "idea" to "running in prod" in under a minute.

## Prerequisites

- .NET SDK ≥ 9.0  
- GitHub CLI (`gh`)  
- Ansible 2.15+  
- Recommended VS Code extensions:  
  - **ms-dotnettools.csharp**  
  - **DavidAnson.vscode-markdownlint**  
  - **redhat.vscode-yaml**  

## Setting up your dev environment

```bash
git clone https://github.com/akwaduk/hum.git
cd hum
make bootstrap       # or: dotnet restore
make test            # runs unit tests
make e2e             # optional Ansible-driven smoke tests
```

## Branch & PR Workflow

- **Fork** the repo (unless you have direct write access), then **branch** from `main`.  
- Use **Conventional Commits** (`feat:`, `fix:`, `docs:`, etc.).  
- Rebase your branch onto the latest `main` before opening a PR.  
- Keep each PR focused on one logical change.

## Running the CLI from Source

```bash
dotnet run --project src/hum -- doctor
```

## Code Style

- Target C# 10, .NET 9 conventions.  
- An `.editorconfig` is committed.  
- Run `make format` (alias for `dotnet format`) before each commit.

## Unit Tests & Coverage

```bash
dotnet test /p:CollectCoverage=true
```
- Coverage threshold: **90%**.

## End-to-End Smoke Tests

- Requires Ansible/AWX credentials.  
- Skip by setting `HUM_E2E_SKIP=1`.

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

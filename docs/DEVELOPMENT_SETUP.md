# Development Setup

This guide walks through setting up your development environment for working on **hum**.

## Prerequisites

- .NET 9.0 SDK or later
- Git
- GitHub CLI (`gh`)
- Ansible 2.15+

## Fork & Clone

1. Fork the repository on GitHub: https://github.com/akwaduk/hum
2. Clone your fork locally:
   ```bash
   git clone https://github.com/<your-username>/hum.git
   cd hum
   ```

## Restore Dependencies

```powershell
# Restore .NET dependencies
make bootstrap
# Or alternatively:
dotnet restore
```

## Running the CLI Locally

```powershell
# Run the doctor command to verify your environment
dotnet run -- doctor

# Run other commands
dotnet run -- init --help
dotnet run -- ansible-config
```

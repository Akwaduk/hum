# Development Setup

This guide walks through setting up your development environment for working on **hum**.

## Prerequisites

- .NET 8.0 SDK or later
- Git

## Fork & Clone

1. Fork the repository on GitHub: https://github.com/your-org/hum
2. Clone your fork locally:
   ```bash
   git clone https://github.com/<your-username>/hum.git
   cd hum
   ```

## Restore Dependencies

```powershell
# Restore .NET dependencies
dotnet restore
```

## Running the CLI Locally

```powershell
# Run the doctor command to verify your environment
dotnet run --project src/hum -- doctor
```

# Contributing to hum

For detailed contribution guidelines and policies, please see the `docs` folder:

- [Code of Conduct](docs/CODE_OF_CONDUCT.md)
- [Development Setup](docs/DEVELOPMENT_SETUP.md)
- [Testing](docs/TESTING.md)
- [Submitting a Pull Request](docs/SUBMITTING_PR.md)
- [Reporting Issues](docs/REPORTING_ISSUES.md)

Thank you for your interest in contributing to **hum**! We welcome contributions of all kinds, including bug reports, feature requests, documentation improvements, and code enhancements.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Running the CLI Locally](#running-the-cli-locally)
- [Building & Testing](#building--testing)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Reporting Issues](#reporting-issues)

## Code of Conduct

Please read and follow our [Code of Conduct](CODE_OF_CONDUCT.md) to ensure a welcoming and respectful environment for everyone.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git
- A GitHub account and, optionally, a Personal Access Token (PAT) with appropriate scopes if you work with private repositories.

### Fork & Clone

1. Fork the repository on GitHub: https://github.com/your-org/hum
2. Clone your fork locally:
   ```bash
   git clone https://github.com/<your-username>/hum.git
   cd hum
   ```

## Development Setup

Install any required tools and dependencies:

```powershell
# Restore .NET dependencies
dotnet restore

# (Optional) Install pre-commit hooks or formatters if configured
# e.g. dotnet tool install -g dotnet-format
```

## Running the CLI Locally

You can run the CLI directly from source:

```powershell
# Run the doctor command to verify your environment
dotnet run --project src/hum -- doctor
```

## Building & Testing

Build and package the tool locally:

```powershell
# Pack the project in Release configuration
dotnet pack -c Release

# Install the packaged tool from the generated nupkg folder
dotnet tool install -g --add-source ./nupkg hum
```

Run any tests (if available):

```powershell
# Example: run unit tests if they exist
dotnet test
```

## Submitting a Pull Request

1. Create a new branch for your work:
   ```bash
   git checkout -b feature/my-new-feature
   ```
2. Make your changes, adhering to the existing code style and formatting.
3. Update documentation and add tests for new functionality whenever possible.
4. Commit your changes with a clear message:
   ```bash
   git commit -m "Add feature: description of feature"
   ```
5. Push to your fork:
   ```bash
   git push origin feature/my-new-feature
   ```
6. Open a Pull Request against the `main` branch of the upstream repository.

Our maintainers will review your PR and provide feedback.

## Reporting Issues

If you encounter bugs or have feature requests, please open an issue on GitHub: https://github.com/your-org/hum/issues

Include the following when reporting an issue:

- A clear and descriptive title
- Steps to reproduce the problem
- Expected vs. actual behavior
- Any relevant logs or error messages

---

Thank you for helping improve **hum**! We appreciate your contributions and feedback.

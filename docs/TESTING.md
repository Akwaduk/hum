# Testing

This document describes how to run and write tests for **hum**.

## Running Existing Tests

To run all tests in the solution:
```powershell
# Run all tests via the make task
make test

# Or directly with dotnet:
dotnet test

# Run tests with code coverage
dotnet test /p:CollectCoverage=true
```

## Running End-to-End Tests

The project includes Ansible-driven smoke tests:
```powershell
# Run the e2e tests
make e2e
```

Note: You can skip the e2e tests by setting `HUM_E2E_SKIP=1` in your environment.

## Writing New Tests

1. Create a new test project alongside `src/hum` (e.g., `tests/hum.Tests`).
2. Add reference to the main project:
   ```bash
dotnet add tests/hum.Tests/hum.Tests.csproj reference src/hum/hum.csproj
   ```
3. Write test classes and methods using xUnit or NUnit.
4. Run `dotnet test` to validate your changes.

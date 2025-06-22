# Testing

This document describes how to run and write tests for **hum**.

## Running Existing Tests

If there are unit or integration tests, run:
```powershell
# Run all tests in the solution
dotnet test
```

## Writing New Tests

1. Create a new test project alongside `src/hum` (e.g., `tests/hum.Tests`).
2. Add reference to the main project:
   ```bash
dotnet add tests/hum.Tests/hum.Tests.csproj reference src/hum/hum.csproj
   ```
3. Write test classes and methods using xUnit or NUnit.
4. Run `dotnet test` to validate your changes.

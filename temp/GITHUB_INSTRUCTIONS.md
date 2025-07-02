# GitHub Contribution Workflow

This document provides a step-by-step guide for contributing to the hum project through GitHub. It covers everything from forking the repository to creating pull requests and addressing feedback.

## Initial Setup

### 1. Fork the Repository

1. Visit the [hum repository](https://github.com/organization/hum)
2. Click the "Fork" button in the top-right corner
3. Select your account as the destination

Alternatively, use GitHub CLI:
```powershell
gh repo fork organization/hum --clone=true --remote=true
cd hum
```

### 2. Clone Your Fork

```powershell
git clone https://github.com/YOUR-USERNAME/hum.git
cd hum
```

### 3. Add Upstream Remote

```powershell
git remote add upstream https://github.com/organization/hum.git
git fetch upstream
```

## Making Contributions

### 1. Create a Feature Branch

Always create a new branch for each contribution:

```powershell
git checkout -b feature/descriptive-branch-name
```

Use prefixes like:
- `feature/` for new features
- `fix/` for bug fixes
- `docs/` for documentation updates
- `refactor/` for code refactoring

### 2. Develop Your Changes

1. Make your code changes
2. Follow the coding standards
3. Run tests to verify your changes
4. Format your code:
   ```powershell
   dotnet format
   ```

### 3. Commit Your Changes

Follow the conventional commits standard:

```powershell
# Format: <type>[optional scope]: <description>
git commit -m "feat: add support for Python Flask templates"
git commit -m "fix: resolve GitHub authentication timeout"
git commit -m "docs: improve Windows installation guide"
```

Common types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style/formatting
- `refactor`: Code refactoring
- `test`: Adding/updating tests
- `chore`: Maintenance tasks

### 4. Stay Updated with Upstream

Before pushing your changes:

```powershell
git fetch upstream
git rebase upstream/main
```

If there are conflicts, resolve them:
```powershell
# After resolving conflicts
git add .
git rebase --continue
```

### 5. Push Your Changes

```powershell
git push -u origin feature/descriptive-branch-name
```

## Creating a Pull Request

### 1. Open a Pull Request

Using GitHub CLI:
```powershell
gh pr create --title "feat: add support for Python Flask templates" --body "Description of the changes"
```

Or through the GitHub web interface:
1. Visit your fork on GitHub
2. Click "Compare & pull request"
3. Add a descriptive title and detailed description
4. Click "Create pull request"

### 2. PR Best Practices

- Keep PRs focused on a single change
- Include a clear description of what the changes do and why
- Link to any related issues (`Fixes #123`)
- Add screenshots for UI changes
- Update documentation if needed
- Include tests for new functionality

### 3. Address Feedback

- Be responsive to reviews
- Make requested changes promptly
- Push additional commits to the same branch
- Use `git push -f` only if necessary (after rebasing)
- Add comments to explain complex decisions

## After Your PR is Merged

1. Update your local main:
   ```powershell
   git checkout main
   git pull upstream main
   ```

2. Delete your feature branch:
   ```powershell
   git branch -d feature/descriptive-branch-name
   git push origin --delete feature/descriptive-branch-name
   ```

## GitHub Actions Workflow

The hum project uses GitHub Actions for CI/CD:

1. **Continuous Integration**
   - Builds the project
   - Runs unit tests
   - Checks code formatting
   - Calculates test coverage
   - Validates documentation

2. **Pull Request Checks**
   - Automatically triggered on new PRs and updates
   - Results appear in PR comments
   - All checks must pass before merging

3. **Release Process**
   - Release workflows are triggered by tags
   - Creates GitHub releases
   - Publishes packages to NuGet

## Troubleshooting GitHub Issues

### Authentication Problems

If you have trouble with GitHub authentication:

1. Ensure GitHub CLI is authenticated:
   ```powershell
   gh auth status
   ```

2. Re-authenticate if needed:
   ```powershell
   gh auth login
   ```

3. Check your git configuration:
   ```powershell
   git config --list
   ```

### PR Build Failures

If your PR build fails:

1. Check the GitHub Actions logs
2. Run the same checks locally:
   ```powershell
   dotnet build
   dotnet test
   dotnet format --verify-no-changes
   ```

3. Make necessary fixes and push again

## Need Help?

If you encounter any issues with the GitHub contribution process:

1. Check the [GitHub documentation](https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests)
2. Review existing issues and PRs for similar problems
3. Ask for help in the project's discussion forums
4. Contact the maintainers for guidance

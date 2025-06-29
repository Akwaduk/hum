# Working with GitHub in hum Development

This guide explains how hum integrates with GitHub and how to troubleshoot common issues when developing or enhancing hum's GitHub functionality.

## How hum Uses GitHub

The hum CLI provides GitHub integration through two provider implementations:

1. `GitHubCliProvider.cs` - Uses the GitHub CLI (`gh`) as an external process
2. `GitHubProvider.cs` - Uses the Octokit library for direct GitHub API access

## Common Development Issues

### Repository Push Conflicts

When developing hum's GitHub integration functionality, you might encounter issues where the first git push fails because:

1. The remote repository is created with a README (through `AutoInit = true` in GitHubProvider)
2. The local repository doesn't have that README
3. Git refuses to push because the remote branch has commits not in local

We've implemented multiple solutions to handle this issue:

1. **Solution in GitHubProvider**: Setting `AutoInit = false` when creating repositories
   ```csharp
   var newRepo = new NewRepository(name)
   {
       Description = description,
       Private = false,
       AutoInit = false // No initial README to avoid conflicts
   };
   ```

2. **Solution in GitHubCliProvider**: Pulling remote changes before pushing
   ```csharp
   // Pull with rebase before push
   git pull --rebase origin main
   ```

3. **Force push fallback**: If the pull approach fails, use force push
   ```csharp
   // Try direct force push if pull fails
   git push -u origin main --force
   ```

4. **Repository recreation**: If all else fails, delete and recreate the repository
   ```csharp
   // Delete existing repository
   gh repo delete owner/name --yes
   
   // Create clean repository without README
   gh repo create name --public --clone=false
   
   // Push to the fresh repository
   git push -u origin main
   ```

### Manual Fix for Push Issues

If you're still experiencing the issue where only a README.md is in the repository:

1. Delete the GitHub repository:
   ```bash
   gh repo delete <your-repo-name> --yes
   ```

2. Create a new repository without initialization:
   ```bash
   gh repo create <your-repo-name> --public --clone=false
   ```

3. Push your local code:
   ```bash
   # Make sure you have a local commit
   git add .
   git commit -m "Initial commit"
   
   # Force push to the new repository
   git push -u origin main --force
   ```

### Testing GitHub Integration

When testing changes to GitHub integration:

1. Use `gh auth status` to verify you're authenticated
2. For local debugging, set up environment variables:
   ```bash
   # If your code needs these
   export GITHUB_TOKEN=your_token_here
   export GITHUB_USERNAME=your_username
   ```
3. Run integration tests with real repositories:
   ```bash
   # Create test repositories with a unique name
   make test-github-integration
   ```

## Code Changes

When modifying GitHub-related code, remember:

- Both providers must implement the `ISourceControlProvider` interface
- Both must have the `CanHandle(string sourceControlType)` method
- Prefer using `GitHubCliProvider` over direct API calls when possible
- Consider timeouts and error handling for all external processes
- Add logging for debugging repository issues

## Useful Commands

For troubleshooting GitHub integration:

```bash
# Check GitHub CLI auth status
gh auth login --status

# Get details about a repository
gh repo view <repo-name> --json name,description,url

# Create a test repository
gh repo create <test-repo-name> --public --clone=false

# Delete a test repository
gh repo delete <test-repo-name> --yes

# Fix "push rejected" errors
git pull --rebase origin main
git push -u origin main --force

# Check git remote configuration
git remote -v
git remote set-url origin https://github.com/username/repo.git

# Debug git push issues
git fetch
git status
git remote show origin
```

## How to Test Your Changes

After making changes to the GitHub integration code:

1. Build the project:
   ```bash
   dotnet build
   ```

2. Run the test script:
   ```bash
   # On Windows
   .\test-github-integration.ps1
   
   # On Linux/macOS
   bash ./test-github-integration.sh
   ```

3. Or test manually:
   ```bash
   # Create a test project with a unique name
   dotnet run -- create test-$(date +%s) --template dotnet-minimal
   ```

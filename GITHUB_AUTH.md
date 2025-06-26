# GitHub Authentication Guide

## Modern Authentication with GitHub CLI (Recommended)

As of 2025, hum uses **GitHub CLI** for secure, modern authentication instead of manual Personal Access Tokens.

### Why GitHub CLI?

✅ **More Secure** - Uses OAuth flow instead of long-lived tokens  
✅ **Easier Setup** - No manual token management  
✅ **Better UX** - Browser-based authentication  
✅ **Future-proof** - Follows GitHub's current best practices  

### Quick Setup

1. **Install GitHub CLI** (if not already installed):
   ```bash
   # Windows (via winget)
   winget install GitHub.cli
   
   # Windows (via Chocolatey)
   choco install gh
   
   # macOS
   brew install gh
   
   # Linux
   curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
   echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
   sudo apt update
   sudo apt install gh
   ```

2. **Authenticate with GitHub**:
   ```bash
   gh auth login
   ```
   
   This will:
   - Open your browser
   - Ask you to authorize the GitHub CLI
   - Securely store your credentials

3. **Verify Authentication**:
   ```bash
   # Check if you're logged in
   gh auth status
   
   # Or use hum's built-in check
   hum doctor
   ```

### Using hum with GitHub CLI

Once authenticated, hum will automatically use your GitHub CLI credentials:

```bash
# Create a new service (no manual token needed!)
hum create my-api --template dotnet-webapi --env production

# hum will automatically:
# ✅ Use your GitHub CLI authentication
# ✅ Create the repository in your account/org
# ✅ Set up CI/CD workflows
```

### Advanced Configuration

#### Organization Access
If you work with GitHub organizations, make sure you authorize them:

```bash
# Check which orgs you have access to
gh auth status

# If you need to add org access, re-run login
gh auth login --scopes "repo,write:org"
```

#### Repository Permissions
The GitHub CLI authentication provides these permissions automatically:
- **Repository creation** in your account and authorized organizations
- **Content management** (push code, create files)
- **Actions management** (create workflows)
- **Branch protection** (configure repository settings)

### Troubleshooting

#### "Not authenticated" error
```bash
# Re-authenticate
gh auth login

# Check status
gh auth status

# Test repository access
gh repo list
```

#### "Permission denied" for organization
```bash
# Login with organization access
gh auth login --scopes "repo,write:org,read:org"

# Or refresh existing authentication
gh auth refresh --scopes "repo,write:org,read:org"
```

#### Token expired
```bash
# GitHub CLI handles token refresh automatically
# If issues persist, re-login
gh auth logout
gh auth login
```

### Migration from Personal Access Tokens

If you were previously using manual tokens with hum:

1. **Remove old configuration**:
   ```bash
   # Clear old token settings (optional)
   hum config --show  # see current config
   ```

2. **Set up GitHub CLI**:
   ```bash
   gh auth login
   ```

3. **Verify**:
   ```bash
   hum doctor  # should show GitHub CLI authentication ✅
   ```

### Security Benefits

✅ **No long-lived tokens** - GitHub CLI uses short-lived tokens  
✅ **Automatic refresh** - No manual token rotation needed  
✅ **Scoped permissions** - Only the permissions you authorize  
✅ **Revocable** - Easy to revoke access from GitHub settings  
✅ **Audit trail** - All actions logged to your GitHub account  

### For Advanced Users: GitHub Apps

For production deployments or team usage, consider setting up a GitHub App instead of individual authentication. This provides:

- Fine-grained permissions
- Installation-based access control
- Better audit trails
- Team management capabilities

Contact your DevOps team for GitHub App setup if needed for your organization.

---

**Need help?** Run `hum doctor` to check your authentication status and get specific guidance.

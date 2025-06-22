# hum

![hummingbird](Assets/hum.png)

**From idea to running prototype in under 60 seconds.**

hum is a CLI tool that eliminates the friction between having an app idea and seeing it running on your infrastructure. Built specifically for on-premise Debian/Linux environments, hum automates the entire bootstrap process—from repository creation to deployment—so you can focus on building features instead of configuring pipelines.

## Why hum?

Every new service requires the same tedious setup: create a repository, configure CI/CD, set up deployment scripts, provision infrastructure, and wire everything together. This process typically takes hours or days and involves copying configuration from previous projects.

hum reduces this to a single command that:
- Creates a GitHub repository from your chosen template
- Sets up a complete CI/CD pipeline with GitHub Actions
- Configures your Ansible inventory for deployment
- Provisions the service on your infrastructure
- Establishes monitoring and backup procedures

## Quick Start

```bash
# Install hum globally
dotnet tool install -g hum

# Create and deploy a new web API
hum create my-orders-api --template dotnet-webapi --env production

# Your service is now running at https://my-orders-api.yourdomain.com
```

That's it. Your idea is now a running service with proper CI/CD, monitoring, and backups.

## How It Works

hum orchestrates your existing infrastructure tools rather than replacing them:

1. **Repository Creation**: Uses GitHub's API to create a new repository from your template library
2. **CI/CD Setup**: Configures GitHub Actions workflows tailored to your deployment pipeline
3. **Infrastructure Provisioning**: Updates your Ansible inventory and triggers deployment
4. **Service Deployment**: Deploys your service to available servers with proper load balancing
5. **Operations Setup**: Configures systemd services, NGINX routing, SSL certificates, and backup procedures

```
Developer → hum → GitHub → GitHub Actions → Ansible → Production Server
    ↓           ↓        ↓              ↓         ↓
   Idea    Repository  CI/CD      Deployment   Running App
```

## Prerequisites

- **.NET 8.0+** - Required to run hum
- **GitHub account** - With Personal Access Token for repository management
- **Ansible control node** - For infrastructure automation (v2.15+)
- **Target servers** - Debian/Linux servers managed by your Ansible inventory

Optional but recommended:
- **AWX/Ansible Tower** - For web-based deployment management
- **gh CLI** - Enhanced GitHub integration

## Installation & Configuration

```bash
# Install hum
dotnet tool install -g hum

# Set up authentication
export HUM_GITHUB_TOKEN="your_github_pat"

# Configure defaults (optional)
hum config init
```

Create `~/.config/hum/config.yaml` for your environment:

```yaml
org: your-company
inventory_repo: git@github.com:your-company/infrastructure.git
default_template: dotnet-webapi
default_environment: staging
awx:
  url: https://awx.internal/api/
  token_env: AWX_TOKEN
```

## Available Commands

| Command | Purpose |
|---------|---------|
| `hum create <name>` | Bootstrap a new service with full deployment pipeline |
| `hum templates` | List available project templates |
| `hum servers` | Show available deployment targets |
| `hum status <name>` | Check service deployment status |
| `hum destroy <name>` | Safely remove service and clean up resources |
| `hum doctor` | Validate configuration and connectivity |

## Templates

hum includes templates for common service types:

- **dotnet-webapi** - ASP.NET Core Web API with OpenAPI
- **dotnet-worker** - Background service with message processing
- **static-site** - Static website with build pipeline
- **database-service** - PostgreSQL/MySQL service with migrations

Custom templates can be added to your organization's template repository.

## Infrastructure Integration

hum works with your existing infrastructure:

**Ansible Integration**
- Updates inventory files with new service configuration
- Triggers deployment playbooks automatically
- Manages secrets through Ansible Vault

**Server Management**
- Automatically selects available servers based on capacity
- Configures load balancing and health checks
- Sets up SSL certificates and domain routing

**Monitoring & Operations**
- Configures systemd services with proper logging
- Sets up automated backups and retention policies  
- Enables health monitoring and alerting

## Example Workflow

```bash
# Create a new order processing service
hum create order-processor --template dotnet-webapi --env production

# hum will:
# 1. Create GitHub repo "order-processor" from template
# 2. Configure CI/CD pipeline for .NET builds
# 3. Update Ansible inventory with service configuration
# 4. Deploy to available production server
# 5. Configure NGINX, SSL, systemd, and backups
# 6. Verify service health and availability

# Result: https://order-processor.yourdomain.com is live
```

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

**Development Setup**
```bash
# Run from source
dotnet run --project src/hum -- doctor

# Build and test locally  
dotnet pack -c Release
dotnet tool install -g --add-source ./nupkg hum
```

## License

MIT License - see [LICENSE](LICENSE) for details.

---

*Logo © 2025 Erik Johnson - free to use within this project context.*
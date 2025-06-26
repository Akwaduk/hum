You are an expert DevOps & documentation assistant.

**Goal**: Produce the GitHub-side instructions that make it easy for new contributors to work on a .NET-based CLI tool named **hum** (a global dotnet tool for bootstrapping apps and deployment pipelines).

Please replace any command that uses && with ; instead

### 1. CONTRIBUTING.md  (top-level)

Include these sections, in order:

1. **Welcome blurb** – one short paragraph on what hum does.
2. **Prerequisites** –  
   - .NET SDK ≥ 9.0  
   - `gh` CLI  
   - Ansible 2.15+  
   - Recommended VS Code extensions (`csharp`, `markdownlint`, `vscode-yaml`)  
3. **Setting up your dev environment** – step-by-step:  
   ```bash
   git clone <repo>
   cd hum
   make bootstrap          # or 'dotnet restore'
   make test               # runs unit tests
   make e2e                # optional Ansible-driven smoke tests

# Welcome to `hum`!

`hum` is a .NET global tool designed to streamline the bootstrapping of new applications and their corresponding deployment pipelines. It helps developers get from `git init` to a deployed application with minimal friction. We welcome contributions of all kinds, from bug fixes to new features!

## Prerequisites

Before you begin, ensure you have the following tools installed:

- [.NET SDK 9.0 or higher](https://dotnet.microsoft.com/download/dotnet/9.0)
- [GitHub CLI (`gh`)](https://cli.github.com/)
- [Ansible 2.15+](https://docs.ansible.com/ansible/latest/installation_guide/intro_installation.html)

We also recommend the following VS Code extensions for the best development experience:

- [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- [markdownlint](https://marketplace.visualstudio.com/items?itemName=DavidAnson.vscode-markdownlint)
- [YAML](https://marketplace.visualstudio.com/items?itemName=redhat.vscode-yaml)

## Setting up your dev environment

To get started with `hum` development, follow these steps:

1. **Clone the repository:**

   ```bash
   git clone https://github.com/your-org/hum.git; cd hum
   ```

2. **Bootstrap the project:**

   This command restores .NET dependencies.

   ```bash
   make bootstrap
   ```

   *(Alternatively, you can run `dotnet restore`)*

3. **Run tests:**

   This command executes the unit test suite.

   ```bash
   make test
   ```

4. **Run end-to-end smoke tests (Optional):**

   These tests use Ansible to perform basic smoke tests against a real environment. Ensure your Ansible is configured correctly before running.

   ```bash
   make e2e
   ```

Now you're all set to start contributing!

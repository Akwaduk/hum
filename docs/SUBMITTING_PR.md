# Submitting a Pull Request

Thank you for your contribution! To submit a PR:

1. Fork the repository and create a new branch:
   ```bash
   git checkout -b feature/my-new-feature
   ```
2. Make your changes, ensuring code style and formatting match the project.
3. Add or update tests for new functionality.
4. Commit your changes using Conventional Commits format:
   ```bash
   git commit -m "feat(scope): short, imperative summary"
   ```
   
   Use appropriate types:
   - feat: New feature
   - fix: Bug fix
   - docs: Documentation only
   - ci: CI configuration
   - refactor: Code restructuring
   - chore: Maintenance tasks
5. Push to your fork:
   ```bash
   git push origin feature/my-new-feature
   ```
6. Run tests and formatting checks:
   ```bash
   make format
   make test
   ```

7. Open a Pull Request against the `main` branch of the upstream repository.

8. Fill out the PR template with a description of your changes.

9. Sign the CLA by adding the appropriate text to your PR description if this is your first contribution.

Our maintainers will review your PR and provide feedback. Please address any review comments promptly.

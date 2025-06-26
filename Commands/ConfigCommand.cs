using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using hum.Models;
using hum.Services;

namespace hum.Commands
{
    public class ConfigCommand : Command
    {
        // Only need git config now - GitHub auth handled by CLI
        private readonly Option<string>? gitUsernameOption;
        private readonly Option<string>? gitEmailOption;
        private readonly Option<bool>? showOption;

        public ConfigCommand() : base("config", "Configure hum settings")
        {
            // Only git config options now
            gitUsernameOption = new Option<string>(
                "--git-username",
                "Git username for commits");
                
            gitEmailOption = new Option<string>(
                "--git-email", 
                "Git email for commits");

            showOption = new Option<bool>(
                "--show",
                "Show current configuration");
                
            // Add options to command
            AddOption(gitUsernameOption);
            AddOption(gitEmailOption);
            AddOption(showOption);
            
            this.SetHandler(HandleCommand);
        }

        private async Task HandleCommand(InvocationContext context)
        {
            // Use class fields to get option values
            string? githubToken = context.ParseResult.GetValueForOption(githubTokenOption!); // Added null-forgiving operator as they are initialized in ctor
            string? githubUsername = context.ParseResult.GetValueForOption(githubUsernameOption!);
            string? gitUsername = context.ParseResult.GetValueForOption(gitUsernameOption!);
            string? gitEmail = context.ParseResult.GetValueForOption(gitEmailOption!);
            bool show = context.ParseResult.GetValueForOption(showOption!); // showOption is bool, GetValueForOption<bool> returns non-nullable bool
            
            var configService = new ConfigurationService();
            var settings = await configService.LoadSettingsAsync();
            
            if (show)
            {
                Console.WriteLine("Current Configuration:");
                Console.WriteLine("GitHub Authentication: Use 'gh auth status' to check GitHub CLI");
                Console.WriteLine($"Git Username: {settings.DefaultGitConfig?.Username ?? "Not set"}");
                Console.WriteLine($"Git Email: {settings.DefaultGitConfig?.Email ?? "Not set"}");
                Console.WriteLine("");
                Console.WriteLine("Note: GitHub credentials are now handled by GitHub CLI.");
                Console.WriteLine("Run 'gh auth login' to authenticate with GitHub.");
                return;
            }
            
            bool changed = false;
            
            if (!string.IsNullOrEmpty(githubToken))
            {
                settings.GitHubToken = githubToken;
                changed = true;
            }
            
            if (!string.IsNullOrEmpty(githubUsername))
            {
                settings.GitHubUsername = githubUsername;
                changed = true;
            }
            
            if (!string.IsNullOrEmpty(gitUsername))
            {
                if (settings.DefaultGitConfig == null)
                    settings.DefaultGitConfig = new GitConfig { Username = "", Email = "" };
                settings.DefaultGitConfig.Username = gitUsername;
                changed = true;
            }
            
            if (!string.IsNullOrEmpty(gitEmail))
            {
                if (settings.DefaultGitConfig == null)
                    settings.DefaultGitConfig = new GitConfig { Username = "", Email = "" };
                settings.DefaultGitConfig.Email = gitEmail;
                changed = true;
            }
            
            if (changed)
            {
                await configService.SaveSettingsAsync(settings);
                Console.WriteLine("Configuration updated successfully.");
            }
            else if (!show)
            {
                Console.WriteLine("No changes made to configuration.");
            }
        }
    }
}

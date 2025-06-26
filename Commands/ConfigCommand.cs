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
        // Made fields nullable to satisfy compiler, will be initialized in constructor
        private readonly Option<string>? githubUsernameOption;
        private readonly Option<string>? githubTokenOption;
        private readonly Option<string>? gitUsernameOption;
        private readonly Option<string>? gitEmailOption;
        private readonly Option<bool>? showOption;

        public ConfigCommand() : base("config", "Configure hum settings")
        {
            // Correctly initialize class fields
            githubTokenOption = new Option<string>(
                "--github-token",
                "GitHub personal access token");
                
            githubUsernameOption = new Option<string>( // Corrected assignment
                "--github-username",
                "GitHub username");
                
            gitUsernameOption = new Option<string>( // Corrected assignment
                "--git-username",
                "Git username for commits");
                
            gitEmailOption = new Option<string>( // Corrected assignment
                "--git-email",
                "Git email for commits");

            showOption = new Option<bool>( // Corrected: Single assignment for showOption
                "--show",
                "Show current configuration");
                
            AddOption(githubTokenOption);
            AddOption(githubUsernameOption); 
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
                Console.WriteLine($"GitHub Username: {settings.GitHubUsername}");
                Console.WriteLine($"GitHub Token: {(string.IsNullOrEmpty(settings.GitHubToken) ? "Not set" : "********")}");
                Console.WriteLine($"Git Username: {settings.DefaultGitConfig.Username}");
                Console.WriteLine($"Git Email: {settings.DefaultGitConfig.Email}");
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
                settings.DefaultGitConfig.Username = gitUsername;
                changed = true;
            }
            
            if (!string.IsNullOrEmpty(gitEmail))
            {
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

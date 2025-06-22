namespace hum.Models
{
    public class GitConfig
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string DefaultBranch { get; set; } = "main";
        public bool InitializeWithReadme { get; set; } = true;
        public string IgnoreTemplate { get; set; } = "dotnet"; // Template for .gitignore
        public string[] AdditionalIgnorePatterns { get; set; } = new string[0];
    }
}

using System.Collections.Generic;

namespace hum.Models
{
    public class ProjectConfig
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string TemplateType { get; set; } // e.g., "dotnet", "svelte"
        public string? OutputPath { get; set; }
        public string? SourceControlProvider { get; set; } // e.g., "github"
        public string? CiCdProvider { get; set; } // e.g., "github-actions"
        public string? InfrastructureProvider { get; set; } // e.g., "ansible"
        
        // Git configuration
        public GitConfig? GitConfig { get; set; }
        
        // Deployment configuration
        public DeploymentConfig? DeploymentConfig { get; set; }
        
        // Additional configuration options that might be specific to certain providers
        public Dictionary<string, object> AdditionalOptions { get; set; } = new Dictionary<string, object>();
    }
}

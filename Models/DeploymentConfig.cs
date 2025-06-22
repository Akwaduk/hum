using System.Collections.Generic;

namespace hum.Models
{
    public class DeploymentConfig
    {
        public List<Environment> Environments { get; set; } = new List<Environment>();
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        public List<string> Secrets { get; set; } = new List<string>();
    }

    public class Environment
    {
        public required string Name { get; set; }
        public required string HostName { get; set; }
        public required string DeploymentPath { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    }
}

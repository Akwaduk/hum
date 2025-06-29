using System.Threading.Tasks;
using hum.Models;

namespace hum.Providers
{
    public interface ISourceControlProvider
    {
        string ProviderName { get; }
        
        // Add a method to check if this provider can handle a specific source control type
        bool CanHandle(string sourceControlType);
        
        Task<RepositoryInfo> CreateRepositoryAsync(string name, string description);
        Task ConfigureRepositoryAsync(RepositoryInfo repository, ProjectConfig projectConfig);
        Task<string> GetRepositoryUrlAsync(string repositoryName);
    }
}

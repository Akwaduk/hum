using System.Threading.Tasks;
using hum.Models;

namespace hum.Providers
{
    public interface ISourceControlProvider
    {
        string ProviderName { get; }
        Task<RepositoryInfo> CreateRepositoryAsync(string name, string description);
        Task ConfigureRepositoryAsync(RepositoryInfo repository, ProjectConfig projectConfig);
        Task<string> GetRepositoryUrlAsync(string repositoryName);
    }
}

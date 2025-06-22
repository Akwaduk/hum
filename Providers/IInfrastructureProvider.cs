using System.Threading.Tasks;
using hum.Models;

namespace hum.Providers
{
    public interface IInfrastructureProvider
    {
        string ProviderName { get; }
        Task ConfigureInfrastructureAsync(ProjectConfig projectConfig, RepositoryInfo repositoryInfo);
        Task UpdateInventoryAsync(ProjectConfig projectConfig, RepositoryInfo repositoryInfo);
        Task<bool> ValidateConfigurationAsync(ProjectConfig projectConfig);
    }
}

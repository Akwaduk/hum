using System.Threading.Tasks;
using hum.Models;

namespace hum.Providers
{
    public interface ICiCdProvider
    {
        string ProviderName { get; }
        Task ConfigurePipelinesAsync(RepositoryInfo repository, ProjectConfig projectConfig);
        Task<bool> ValidateConfigurationAsync(RepositoryInfo repository);
    }
}

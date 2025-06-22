using System.Threading.Tasks;
using hum.Models;

namespace hum.Providers
{
    public interface IProjectTemplateProvider
    {
        string TemplateName { get; }
        bool CanHandle(string templateType);
        Task<string> CreateProjectAsync(ProjectConfig projectConfig);
        Task ConfigureProjectAsync(string projectPath, ProjectConfig projectConfig);
    }
}

using LLamaHub.Core.Config;
using LLamaHub.Core.LLamaSharp;

namespace LLamaHub.Core.Services
{
    public interface IModelService
    {
        Task<LLamaHubModelContext> CreateContext(string modelName, string key);
        Task<LLamaHubModelContext> GetContext(string modelName, string key);
        Task<LLamaHubModel> GetModel(string modelName);
        Task<LLamaHubModel> LoadModel(ModelConfig modelConfig);
        Task<bool> RemoveContext(string modelName, string key);
        Task UnloadModel(string modelName);
    }
}
using LLamaHub.Core.Config;
using LLamaHub.Core.LLamaSharp;
using System.Collections.Concurrent;

namespace LLamaHub.Core
{
    public class ModelService : IModelService
    {
        private readonly ConcurrentDictionary<string, LLamaHubModel> _modelInstances;

        public ModelService()
        {
            _modelInstances = new ConcurrentDictionary<string, LLamaHubModel>();
        }


        public Task<LLamaHubModel> LoadModel(ModelConfig modelConfig)
        {
            if (_modelInstances.TryGetValue(modelConfig.Name, out LLamaHubModel model))
                return Task.FromResult(model);

            model = new LLamaHubModel(modelConfig);
            if (!_modelInstances.TryAdd(modelConfig.Name, model))
                throw new Exception($"Failed to cache model {modelConfig.Name}.");

            return Task.FromResult(model);
        }


        public Task UnloadModel(string modelName)
        {
            if (_modelInstances.TryRemove(modelName, out LLamaHubModel model))
            {
                model?.Dispose();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }


        public Task<LLamaHubModel> GetModel(string modelName)
        {
            _modelInstances.TryGetValue(modelName, out LLamaHubModel model);
            return Task.FromResult(model);
        }


        public async Task<LLamaHubModelContext> GetContext(string modelName, string key)
        {
            if (!_modelInstances.TryGetValue(modelName, out LLamaHubModel model))
                throw new Exception("Model not found");

            return await model.GetContext(key);
        }


        public async Task<LLamaHubModelContext> CreateContext(string modelName, string key)
        {
            if (!_modelInstances.TryGetValue(modelName, out LLamaHubModel model))
                throw new Exception("Model not found");

            return await model.CreateContext(key);
        }


        public async Task<bool> RemoveContext(string modelName, string key)
        {
            if (!_modelInstances.TryGetValue(modelName, out LLamaHubModel model))
                throw new Exception("Model not found");

            return await model.RemoveContext(key);
        }
    }
}

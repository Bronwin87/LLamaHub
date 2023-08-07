using LLamaHub.Core.Config;
using LLamaHub.Core.LLamaSharp;
using LLamaHub.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LLamaHub.Core.Services
{
    /// <summary>
    /// Example Service for handling a model session for a websockets connection lifetime
    /// Each websocket connection will create its own unique session and context allowing you to use multiple tabs to compare prompts etc
    /// </summary>
    public class ModelSessionService<T> : IModelSessionService<T>
    {
        private readonly LLamaHubConfig _options;
        private readonly IModelService _modelService;
        private readonly ILogger<ModelSessionService<T>> _logger;
        private readonly ConcurrentDictionary<T, ModelSession> _modelSessions;


        public ModelSessionService(ILogger<ModelSessionService<T>> logger, IOptions<LLamaHubConfig> options, IModelService modelService)
        {
            _logger = logger;
            _options = options.Value;
            _modelService = modelService;
            _modelSessions = new ConcurrentDictionary<T, ModelSession>();
        }

        public Task<ModelSession> GetAsync(T sessionId)
        {
            _modelSessions.TryGetValue(sessionId, out var modelSession);
            return Task.FromResult(modelSession);
        }


        public async Task<ModelSession> CreateAsync(T sessionId, ISessionConfig sessionConfig)
        {
            // Remove existing connections session
            await RemoveAsync(sessionId);

            var modelConfig = _options.Models.FirstOrDefault(x => x.Name == sessionConfig.Model);
            if (modelConfig is null)
                throw new Exception($"Model option '{sessionConfig.Model}' not found");

            //Max instance
            var currentInstances = _modelSessions.Count(x => x.Value.ModelName == modelConfig.Name);
            if (modelConfig.MaxInstances > -1 && currentInstances >= modelConfig.MaxInstances)
                throw new Exception($"Maximum model instances reached");

            // Create context session
            var context = await CreateModelContext(sessionId, modelConfig);
            var modelSession = new ModelSession(context, modelConfig, sessionConfig);
            if (!_modelSessions.TryAdd(sessionId, modelSession))
                throw new Exception($"Failed to create model session");

            return modelSession;
        }


        public async IAsyncEnumerable<InferFragment> InferAsync(T sessionId, string prompt, CancellationTokenSource cancellationTokenSource)
        {
            var modelSession = await GetAsync(sessionId);
            if (modelSession is null)
                yield break;

            // Create unique response id
            var responseId = Guid.NewGuid().ToString();

            // Send begin of response
            var stopwatch = Stopwatch.GetTimestamp();
            yield return new InferFragment
            {
                Id = responseId,
                IsFirst = true
            };

            // Send content of response
            await foreach (var fragment in modelSession.InferAsync(prompt, cancellationTokenSource))
            {
                yield return new InferFragment
                {
                    Id = responseId,
                    Content = fragment
                };
            }

            // Send end of response
            var elapsedTime = Stopwatch.GetElapsedTime(stopwatch);
            var signature = modelSession.IsInferCanceled()
                  ? $"Inference cancelled after {elapsedTime.TotalSeconds:F0} seconds"
                  : $"Inference completed in {elapsedTime.TotalSeconds:F0} seconds";
            yield return new InferFragment
            {
                Id = responseId,
                IsLast = true,
                Content = signature,
                IsCancelled = modelSession.IsInferCanceled(),
                Elapsed = (int)elapsedTime.TotalMilliseconds
            };
        }


        public async Task<bool> RemoveAsync(T sessionId)
        {
            if (_modelSessions.TryRemove(sessionId, out var modelSession))
            {
                modelSession.CancelInfer();
                var llamaModel = await _modelService.GetModel(modelSession.ModelName);
                if (llamaModel is null)
                    return false;

                return await llamaModel.RemoveContext(sessionId.ToString());
            }
            return false;
        }

        public Task<bool> CancelAsync(T sessionId)
        {
            if (_modelSessions.TryGetValue(sessionId, out var modelSession))
            {
                modelSession.CancelInfer();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        private async Task<LLamaHubModelContext> CreateModelContext(T sessionId, ModelConfig modelOption)
        {
            // Create model
            var llamaModel = await _modelService.GetModel(modelOption.Name)
                          ?? await _modelService.LoadModel(modelOption);
            if (llamaModel is null)
                throw new Exception($"Failed to create model, modelName: {modelOption.Name}");

            //Create context
            var llamaModelContext = await llamaModel.GetContext(sessionId.ToString())
                                 ?? await llamaModel.CreateContext(sessionId.ToString());
            if (llamaModelContext is null)
                throw new Exception($"Failed to create model, connectionId: {sessionId}");

            return llamaModelContext;
        }
    }
}

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
    public class ModelSessionService : IModelSessionService
    {
        private readonly LLamaHubConfig _options;
        private readonly ILogger<ModelSessionService> _logger;
        private readonly IModelService _modelService;
        private readonly ConcurrentDictionary<string, ModelSession> _modelSessions;


        public ModelSessionService(ILogger<ModelSessionService> logger, IOptions<LLamaHubConfig> options, IModelService modelService)
        {
            _logger = logger;
            _options = options.Value;
            _modelService = modelService;
            _modelSessions = new ConcurrentDictionary<string, ModelSession>();
        }

        public Task<ModelSession> GetAsync(string sessionId)
        {
            _modelSessions.TryGetValue(sessionId, out var modelSession);
            return Task.FromResult(modelSession);
        }


        public async Task<ModelSession> CreateAsync(string sessionId, CreateSessionModel sessionModel)
        {
            // Remove existing connections session
            await RemoveAsync(sessionId);

            var modelOption = _options.Models.FirstOrDefault(x => x.Name == sessionModel.Model);
            if (modelOption is null)
                throw new Exception($"Model option '{sessionModel.Model}' not found");


            //Max instance
            var currentInstances = _modelSessions.Count(x => x.Value.ModelName == modelOption.Name);
            if (modelOption.MaxInstances > -1 && currentInstances >= modelOption.MaxInstances)
                throw new Exception($"Maximum model instances reached");

            // Create Model/Context
            var llamaModelContext = await CreateModelContext(sessionId, modelOption);

            // Create executor
            ILLamaHubExecutor executor = sessionModel.ExecutorType switch
            {
                LLamaExecutorType.Interactive => new LLamaHubInteractiveExecutor(llamaModelContext),
                LLamaExecutorType.Instruct => new LLamaHubInstructExecutor(llamaModelContext),
                LLamaExecutorType.Stateless => new LLamaHubStatelessExecutor(llamaModelContext),
                _ => default
            };

            // Create Prompt
            var promptOption = new PromptConfig
            {
                Name = "Custom",
                Prompt = sessionModel.Prompt,
                AntiPrompt = CreateListFromCSV(sessionModel.AntiPrompt),
                OutputFilter = CreateListFromCSV(sessionModel.OutputFilter),
            };

            // Create session
            var modelSession = new ModelSession(executor, modelOption, promptOption, sessionModel);
            if (!_modelSessions.TryAdd(sessionId, modelSession))
                throw new Exception($"Failed to create model session");

            return modelSession;
        }


        public async IAsyncEnumerable<ResponseFragment> InferAsync(string sessionId, string prompt, CancellationTokenSource cancellationTokenSource)
        {
            var modelSession = await GetAsync(sessionId);
            if (modelSession is null)
                yield break;

            // Create unique response id
            var responseId = Guid.NewGuid().ToString();

            // Send begin of response
            var stopwatch = Stopwatch.GetTimestamp();
            yield return new ResponseFragment
            {
                Id = responseId,
                IsFirst = true
            };

            // Send content of response
            await foreach (var fragment in modelSession.InferAsync(prompt, cancellationTokenSource))
            {
                yield return new ResponseFragment
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
            yield return new ResponseFragment
            {
                Id = responseId,
                IsLast = true,
                Content = signature,
                IsCancelled = modelSession.IsInferCanceled(),
                Elapsed = (int)elapsedTime.TotalMilliseconds
            };
        }


        public async Task<bool> RemoveAsync(string sessionId)
        {
            if (_modelSessions.TryRemove(sessionId, out var modelSession))
            {
                modelSession.CancelInfer();
                var llamaModel = await _modelService.GetModel(modelSession.ModelName);
                if (llamaModel is null)
                    return false;

                return await llamaModel.RemoveContext(sessionId);
            }
            return false;
        }

        public Task<bool> CancelAsync(string sessionId)
        {
            if (_modelSessions.TryGetValue(sessionId, out var modelSession))
            {
                modelSession.CancelInfer();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        private async Task<LLamaHubModelContext> CreateModelContext(string sessionId, ModelConfig modelOption)
        {
            // Create model
            var llamaModel = await _modelService.GetModel(modelOption.Name)
                          ?? await _modelService.LoadModel(modelOption);
            if (llamaModel is null)
                throw new Exception($"Failed to create model, modelName: {modelOption.Name}");

            //Create context
            var llamaModelContext = await llamaModel.GetContext(sessionId)
                                 ?? await llamaModel.CreateContext(sessionId);
            if (llamaModelContext is null)
                throw new Exception($"Failed to create model, connectionId: {sessionId}");

            return llamaModelContext;
        }

        private List<string> CreateListFromCSV(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return null;

            return csv.Split(",")
                 .Select(x => x.Trim())
                 .ToList();
        }
    }
}

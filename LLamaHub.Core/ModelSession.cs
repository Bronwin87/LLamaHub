using LLama;
using LLama.Abstractions;
using LLamaHub.Core.Config;
using LLamaHub.Core.Helpers;
using LLamaHub.Core.LLamaSharp;

namespace LLamaHub.Core
{
    public class ModelSession
    {
        private readonly IModelParams _modelParams;
        private readonly ILLamaHubExecutor _executor;
        private readonly ISessionConfig _sessionParams;
         
        private IPromptParams _promptParams;
        private ITextStreamTransform _outputTransform;
        private CancellationTokenSource _cancellationTokenSource;

        public ModelSession(LLamaHubModelContext context, IModelParams modelOptions, ISessionConfig sessionConfig)
        {
            _modelParams = modelOptions;
            _sessionParams = sessionConfig;
            _executor = sessionConfig.ExecutorType switch
            {
                LLamaExecutorType.Interactive => new LLamaHubInteractiveExecutor(context),
                LLamaExecutorType.Instruct => new LLamaHubInstructExecutor(context),
                LLamaExecutorType.Stateless => new LLamaHubStatelessExecutor(context),
                _ => default
            };

            InitializePrompt();
        }

        public string ModelName
        {
            get { return _modelParams.ModelAlias; }
        }

        public IAsyncEnumerable<string> InferAsync(string message, CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
            if (_outputTransform is not null)
                return _outputTransform.TransformAsync(_executor.InferAsync(message, _sessionParams, _cancellationTokenSource.Token));

            return _executor.InferAsync(message, _sessionParams, _cancellationTokenSource.Token);
        }


        public void CancelInfer()
        {
            _cancellationTokenSource?.Cancel();
        }

        public bool IsInferCanceled()
        {
            return _cancellationTokenSource?.IsCancellationRequested ?? false;
        }

        private void InitializePrompt()
        {
            // Create Prompt
            _promptParams = new PromptConfig
            {
                Name = "Custom",
                Prompt = _sessionParams.Prompt,
                AntiPrompt = StringHelpers.CommaSeperatedToList(_sessionParams.AntiPrompt),
                OutputFilter = StringHelpers.CommaSeperatedToList(_sessionParams.OutputFilter),
            };

            // Anti prompt
            _sessionParams.AntiPrompts = _promptParams.AntiPrompt?.Concat(_sessionParams.AntiPrompts ?? Enumerable.Empty<string>()).Distinct() ?? _sessionParams.AntiPrompts;
           
            //Output Filter
            if (_promptParams.OutputFilter?.Count > 0)
                _outputTransform = new LLamaTransforms.KeywordTextOutputStreamTransform(_promptParams.OutputFilter, redundancyLength: 8);

            // Run prompt
            foreach (var _ in _executor.Infer(_promptParams.Prompt, _sessionParams))
            {
                // We dont really need the response of the initial prompt, so exit on first token
                break;
            };
        }
    }
}

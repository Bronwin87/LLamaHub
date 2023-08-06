using LLama.Abstractions;
using LLamaHub.Core.LLamaSharp;
using LLamaHub.Web.Common;

namespace LLama.Web.Models
{
    public class ModelSession
    {
        private bool _isFirstInteraction = true;
        private IModelParams _modelParams;
        private PromptOptions _promptOptions;
        private IInferenceParams _inferenceParams;
        private ITextStreamTransform _outputTransform;
        private ILLamaHubExecutor _executor;
        private CancellationTokenSource _cancellationTokenSource;

        public ModelSession(ILLamaHubExecutor executor, IModelParams modelOptions, PromptOptions promptOptions, IInferenceParams inferenceParams)
        {
            _executor = executor;
            _modelParams = modelOptions;
            _promptOptions = promptOptions;
            _inferenceParams = inferenceParams;
            
            _inferenceParams.AntiPrompts = _promptOptions.AntiPrompt?.Concat(_inferenceParams.AntiPrompts ?? Enumerable.Empty<string>()).Distinct() ?? _inferenceParams.AntiPrompts;
            if (_promptOptions.OutputFilter?.Count > 0)
                _outputTransform = new LLamaTransforms.KeywordTextOutputStreamTransform(_promptOptions.OutputFilter, redundancyLength: 5);
        }

        public string ModelName
        {
            get { return _modelParams.ModelAlias; }
        }

        public IAsyncEnumerable<string> InferAsync(string message, CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
            if (_isFirstInteraction)
            {
                _isFirstInteraction = false;
                message = string.Join(" ", _promptOptions.Prompt , message);
            }

            if (_outputTransform is not null)
                return _outputTransform.TransformAsync(_executor.InferAsync(message, _inferenceParams, _cancellationTokenSource.Token));

            return _executor.InferAsync(message, _inferenceParams, _cancellationTokenSource.Token);
        }


        public void CancelInfer()
        {
            _cancellationTokenSource?.Cancel();
        }

        public bool IsInferCanceled()
        {
            return _cancellationTokenSource?.IsCancellationRequested ?? false;
        }
    }
}

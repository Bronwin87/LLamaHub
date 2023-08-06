using LLama.Abstractions;

namespace LLamaHub.Core.LLamaSharp
{
    public interface ILLamaHubExecutor
    {
        /// <summary>
        /// The loaded model for this executor.
        /// </summary>
        public LLamaHubModelContext Context { get; }

        /// <summary>
        /// Infers a response from the model.
        /// </summary>
        /// <param name="text">Your prompt</param>
        /// <param name="inferenceParams">Any additional parameters</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns></returns>
        IEnumerable<string> Infer(string text, IInferenceParams inferenceParams = null, CancellationToken token = default);

        /// <summary>
        /// Asynchronously infers a response from the model.
        /// </summary>
        /// <param name="text">Your prompt</param>
        /// <param name="inferenceParams">Any additional parameters</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns></returns>
        IAsyncEnumerable<string> InferAsync(string text, IInferenceParams inferenceParams = null, CancellationToken token = default);
    }
}

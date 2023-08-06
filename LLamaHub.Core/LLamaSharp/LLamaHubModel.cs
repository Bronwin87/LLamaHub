using LLama.Abstractions;
using LLama.Common;
using LLama.Native;
using System.Collections.Concurrent;

namespace LLamaHub.Core.LLamaSharp
{
    /// <summary>
    /// The abstraction of a LLama model, which holds the context in the native library.
    /// </summary>
    public class LLamaHubModel : IDisposable
    {
        private readonly SafeLlamaModelHandle _modelHandle;
        private readonly ConcurrentDictionary<string, LLamaHubModelContext> _contexts;

        /// <summary>
        /// The model params set for this model.
        /// </summary>
        public IModelParams Params { get; set; }

        /// <summary>
        /// The native handle, which is used to be passed to the native APIs. Please avoid using it 
        /// unless you know what is the usage of the Native API.
        /// </summary>
        public SafeLlamaModelHandle NativeHandle => _modelHandle;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelParams">Model params.</param>
        public LLamaHubModel(IModelParams modelParams)
        {
            _contexts = new ConcurrentDictionary<string, LLamaHubModelContext>();

            Params = modelParams;
            var contextParams = LLamaModelUtils.CreateContextParams(modelParams);
            _modelHandle = SafeLlamaModelHandle.LoadFromFile(modelParams.ModelPath, contextParams);
            if (!string.IsNullOrEmpty(modelParams.LoraAdapter))
                _modelHandle.ApplyLoraFromFile(modelParams.LoraAdapter, modelParams.LoraBase, modelParams.Threads);
        }

        /// <summary>
        /// Creates a new context session on this model
        /// </summary>
        /// <param name="contextId">The unique context identifier</param>
        /// <param name="encoding">The contexts text encoding</param>
        /// <returns>LLamaModelContext for this LLamaModel</returns>
        /// <exception cref="Exception">Context exists</exception>
        public Task<LLamaHubModelContext> CreateContext(string contextId, string encoding = "UTF-8")
        {
            if (_contexts.TryGetValue(contextId, out var context))
                throw new Exception($"Context with id {contextId} already exists.");

            context = new LLamaHubModelContext(this, encoding);
            if (_contexts.TryAdd(contextId, context))
                return Task.FromResult(context);

            return Task.FromResult<LLamaHubModelContext>(null);
        }

        /// <summary>
        /// Get a contexts belonging to this model
        /// </summary>
        /// <param name="contextId">The unique context identifier</param>
        /// <returns>LLamaModelContext for this LLamaModel with the specified contextId</returns>
        public Task<LLamaHubModelContext> GetContext(string contextId)
        {
            if (_contexts.TryGetValue(contextId, out var context))
                return Task.FromResult(context);

            return Task.FromResult<LLamaHubModelContext>(null);
        }

        /// <summary>
        /// Remove a context from this model
        /// </summary>
        /// <param name="contextId">The unique context identifier</param>
        /// <returns>true if removed, otherwise false</returns>
        public Task<bool> RemoveContext(string contextId)
        {
            if (!_contexts.TryRemove(contextId, out var context))
                return Task.FromResult(false);

            context?.Dispose();
            return Task.FromResult(true);
        }


        /// <inheritdoc />
        public virtual void Dispose()
        {
            foreach (var context in _contexts.Values)
            {
                context?.Dispose();
            }
            _modelHandle.Dispose();
        }
    }
}


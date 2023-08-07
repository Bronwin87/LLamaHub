using LLamaHub.Core.Config;
using LLamaHub.Core.Models;

namespace LLamaHub.Core.Services
{
    public interface IModelSessionService<T>
    {
        Task<ModelSession> GetAsync(T sessionId);
        Task<bool> RemoveAsync(T sessionId);
        Task<bool> CancelAsync(T sessionId);

        Task<ModelSession> CreateAsync(T sessionId, ISessionConfig sessionConfig);
        IAsyncEnumerable<InferFragment> InferAsync(T sessionId, string prompt, CancellationTokenSource cancellationTokenSource);
    }

}

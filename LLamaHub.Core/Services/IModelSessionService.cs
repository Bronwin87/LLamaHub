using LLamaHub.Core.Models;

namespace LLamaHub.Core.Services
{
    public interface IModelSessionService
    {
        Task<ModelSession> GetAsync(string sessionId);
        Task<ModelSession> CreateAsync(string sessionId, CreateSessionModel sessionModel);
        Task<bool> RemoveAsync(string sessionId);
        Task<bool> CancelAsync(string sessionId);
        IAsyncEnumerable<ResponseFragment> InferAsync(string sessionId, string prompt, CancellationTokenSource cancellationTokenSource);
    }

}

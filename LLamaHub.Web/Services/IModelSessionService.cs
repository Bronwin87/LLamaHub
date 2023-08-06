using LLama.Web.Models;
using LLamaHub.Web.Common;
using LLamaHub.Web.Models;

namespace LLamaHub.Web.Services
{
    public interface IModelSessionService
    {
        Task<ModelSession> GetAsync(string sessionId);
        Task<IServiceResult<ModelSession>> CreateAsync(string sessionId, CreateSessionModel sessionModel);
        Task<bool> RemoveAsync(string sessionId);
        Task<bool> CancelAsync(string sessionId);
    }

}

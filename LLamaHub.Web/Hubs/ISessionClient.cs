﻿using LLamaHub.Core.Models;
using LLamaHub.Web.Common;

namespace LLamaHub.Web.Hubs
{
    public interface ISessionClient
    {
        Task OnStatus(string connectionId, SessionConnectionStatus status);
        Task OnResponse(InferFragment fragment);
        Task OnError(string error);
    }
}

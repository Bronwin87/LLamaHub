﻿using LLamaHub.Core.Config;
using LLamaHub.Core.Services;
using LLamaHub.Web.Common;
using Microsoft.AspNetCore.SignalR;

namespace LLamaHub.Web.Hubs
{
    public class SessionConnectionHub : Hub<ISessionClient>
    {
        private readonly ILogger<SessionConnectionHub> _logger;
        private readonly IModelSessionService<string> _modelSessionService;

        public SessionConnectionHub(ILogger<SessionConnectionHub> logger, IModelSessionService<string> modelSessionService)
        {
            _logger = logger;
            _modelSessionService = modelSessionService;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.Log(LogLevel.Information, "[OnConnectedAsync], Id: {0}", Context.ConnectionId);

            // Notify client of successful connection
            await Clients.Caller.OnStatus(Context.ConnectionId, SessionConnectionStatus.Connected);
            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.Log(LogLevel.Information, "[OnDisconnectedAsync], Id: {0}", Context.ConnectionId);

            // Remove connections session on dissconnect
            await _modelSessionService.RemoveAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }


        [HubMethodName("LoadModel")]
        public async Task OnLoadModel(SessionConfig sessionModel)
        {
            _logger.Log(LogLevel.Information, "[OnLoadModel] - Load new model, Connection: {0}", Context.ConnectionId);
          
            // Create model session
            var modelSession = await _modelSessionService.CreateAsync(Context.ConnectionId, sessionModel);
            if (modelSession is null)
            {
                await Clients.Caller.OnError("Failed to create model session");
                return;
            }

            // Notify client
            await Clients.Caller.OnStatus(Context.ConnectionId, SessionConnectionStatus.Loaded);
        }


        [HubMethodName("SendPrompt")]
        public async Task OnSendPrompt(string prompt)
        {
            _logger.Log(LogLevel.Information, "[OnSendPrompt] - New prompt received, Connection: {0}", Context.ConnectionId);

            // Send Infer response
            await foreach (var responseFragment in _modelSessionService.InferAsync(Context.ConnectionId, prompt, CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted)))
            {
                await Clients.Caller.OnResponse(responseFragment);
            }
        }

    }
}

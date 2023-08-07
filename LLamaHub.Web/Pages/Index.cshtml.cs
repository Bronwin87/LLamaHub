using LLamaHub.Core.Config;
using LLamaHub.Core.Services;
using LLamaHub.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace LLamaHub.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IModelSessionService<string> _modelSessionService;

        public IndexModel(ILogger<IndexModel> logger, IOptions<LLamaHubConfig> options, IModelSessionService<string> modelSessionService)
        {
            _logger = logger;
            Options = options.Value;
            _modelSessionService = modelSessionService;
        }

        public LLamaHubConfig Options { get; set; }

        [BindProperty]
        public SessionConfig SessionOptions { get; set; }

        public void OnGet()
        {
            SessionOptions = new SessionConfig
            {
                Prompt = "Below is an instruction that describes a task. Write a response that appropriately completes the request.",
                AntiPrompt = "User:",
               // OutputFilter = "User:, Response:"
            };
        }

        public async Task<IActionResult> OnPostCancel(CancelModel model)
        {
            await _modelSessionService.CancelAsync(model.ConnectionId);
            return new JsonResult(default);
        }
    }
}
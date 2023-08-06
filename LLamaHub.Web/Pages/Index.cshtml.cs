using LLamaHub.Core.Config;
using LLamaHub.Web.Common;
using LLamaHub.Web.Models;
using LLamaHub.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace LLamaHub.Web.Pages
{
	public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ModelSessionService _modelSessionService;

        public IndexModel(ILogger<IndexModel> logger, IOptions<LLamaHubConfig> options, ModelSessionService modelSessionService)
        {
            _logger = logger;
            Options = options.Value;
            _modelSessionService = modelSessionService;
        }

        public LLamaHubConfig Options { get; set; }

        [BindProperty]
        public CreateSessionModel SessionOptions { get; set; }

        public void OnGet()
        {
            SessionOptions = new CreateSessionModel
            {
                Prompt = "Below is an instruction that describes a task. Write a response that appropriately completes the request.",
                AntiPrompt = "User:",
                OutputFilter = "User:, Response:"
            };
        }

        public async Task<IActionResult> OnPostCancel(CancelModel model)
        {
            await _modelSessionService.CancelAsync(model.ConnectionId);
            return new JsonResult(default);
        }
    }
}
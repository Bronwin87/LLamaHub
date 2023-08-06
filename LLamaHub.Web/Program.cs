using LLamaHub.Core;
using LLamaHub.Core.Config;
using LLamaHub.Web.Common;
using LLamaHub.Web.Hubs;
using LLamaHub.Web.Services;

namespace LLamaHub.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            // Load InteractiveOptions
            builder.Services.AddOptions<LLamaHubConfig>()
                .PostConfigure(x => x.Initialize())
                .BindConfiguration(nameof(LLamaHubConfig));

            // Services DI
            builder.Services.AddSingleton<IModelService, ModelService>();
            builder.Services.AddSingleton<ModelSessionService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.MapHub<SessionConnectionHub>(nameof(SessionConnectionHub));

            app.Run();
        }
    }
}
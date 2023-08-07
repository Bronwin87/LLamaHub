using LLamaHub.Core.Config;
using LLamaHub.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LLamaHub.Console
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, configuration);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            await serviceProvider.GetService<App>().RunAsync();
        }

        private static void ConfigureServices(ServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddLogging((loggingBuilder) => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());

            serviceCollection.AddOptions<LLamaHubConfig>()
                .PostConfigure(x => x.Initialize())
                .Bind(configuration.GetSection(nameof(LLamaHubConfig)));

            serviceCollection.AddSingleton<IModelService, ModelService>();
            serviceCollection.AddSingleton<IModelSessionService<int>, ModelSessionService<int>>();
            serviceCollection.AddTransient<App>();


        }
    }

}
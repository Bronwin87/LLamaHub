using LLamaHub.Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            await serviceProvider.GetService<App>().RunAsync(args);
        }

        private static void ConfigureServices(ServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddOptions<LLamaHubConfig>()
                .PostConfigure(x => x.Initialize())
                .Bind(configuration.GetSection(nameof(LLamaHubConfig)));
            serviceCollection.AddTransient<App>();
        }
    }

}
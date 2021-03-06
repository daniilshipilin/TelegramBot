namespace TelegramBot.TestBot
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using TelegramBot.TestBot.Service;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = BuildHost(args);

            await host.RunAsync();
        }

        private static IHost BuildHost(string[] args)
        {
            return new HostBuilder()
                             .ConfigureHostConfiguration(configHost =>
                             {
                                 configHost.SetBasePath(Directory.GetCurrentDirectory());
                                 configHost.AddEnvironmentVariables();
                                 configHost.AddCommandLine(args);
                             })
                             .ConfigureAppConfiguration((hostContext, configApp) =>
                             {
                                 configApp.SetBasePath(Directory.GetCurrentDirectory());
                                 configApp.AddEnvironmentVariables();
                                 configApp.AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true);
                                 configApp.AddJsonFile($"appsettings.dev.json", optional: true, reloadOnChange: true);
                                 configApp.AddJsonFile($"appsettings.prod.json", optional: true, reloadOnChange: true);
                                 configApp.AddCommandLine(args);
                             })
                            .ConfigureServices((hostContext, services) =>
                            {
                                services.AddLogging();
                                services.AddHostedService<HostedService>();
                            })
                            .ConfigureLogging((hostContext, configLogging) =>
                            {
                                configLogging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration).CreateLogger());
                                configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                                configLogging.AddConsole();
                                configLogging.AddDebug();
                            })
                            .Build();
        }
    }
}

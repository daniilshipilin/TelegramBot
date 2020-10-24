using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TelegramBot.Service;

namespace TelegramBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            IHost host = new HostBuilder()
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
                     //configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                     //configApp.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true);
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
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .Build();

            host.Run();
        }
    }
}

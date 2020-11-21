namespace TelegramBot.TestBot.Service
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using TelegramBot.TestBot.Helpers;

    public class HostedService : IHostedService
    {
        private readonly IConfiguration configuration;
        private readonly IHostEnvironment environment;
        private readonly ILogger<HostedService> logger;
        private readonly IHostApplicationLifetime appLifetime;

        private BotService? botService;

        public HostedService(
            IConfiguration configuration,
            IHostEnvironment environment,
            ILogger<HostedService> logger,
            IHostApplicationLifetime appLifetime)
        {
            this.configuration = configuration;
            this.environment = environment;
            this.logger = logger;
            this.appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug($"{nameof(StartAsync)} method called");

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug($"{nameof(StopAsync)} method called");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Post-startup code goes here.
        /// </summary>
        private void OnStarted()
        {
            logger.LogDebug($"{nameof(OnStarted)} method called");

            try
            {
                AppSettings.InitSettings(configuration);
                botService = new BotService(logger);
                botService.PrintBotInfo();
                botService.StartReceiving();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        /// On-stopping code goes here.
        /// </summary>
        private void OnStopping()
        {
            logger.LogDebug($"{nameof(OnStopping)} method called");

            try
            {
                botService?.StopReceiving();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        /// Post-stopped code goes here.
        /// </summary>
        private void OnStopped()
        {
            logger.LogDebug($"{nameof(OnStopped)} method called");
        }
    }
}

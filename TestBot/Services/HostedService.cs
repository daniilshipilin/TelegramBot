using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TelegramBot.Service
{
    public class HostedService : IHostedService
    {
        BotService? _botService;

        readonly IConfiguration _configuration;
        readonly IHostEnvironment _environment;
        readonly ILogger<HostedService> _logger;
        readonly IHostApplicationLifetime _lifetime;

        public HostedService(IConfiguration configuration,
                             IHostEnvironment environment,
                             ILogger<HostedService> logger,
                             IHostApplicationLifetime appLifetime)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _lifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"{nameof(StartAsync)} method called");

            _lifetime.ApplicationStarted.Register(OnStarted);
            _lifetime.ApplicationStopping.Register(OnStopping);
            _lifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Post-startup code goes here.
        /// </summary>
        private void OnStarted()
        {
            _logger.LogDebug($"{nameof(OnStarted)} method called");

            try
            {
                _botService = new BotService(_configuration, _logger);
                _botService.PrintBotInfo();
                _botService.StartReceiving();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        /// On-stopping code goes here
        /// </summary>
        private void OnStopping()
        {
            _logger.LogDebug($"{nameof(OnStopping)} method called");

            try
            {
                _botService?.StopReceiving();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        /// <summary>
        /// Post-stopped code goes here.
        /// </summary>
        private void OnStopped()
        {
            _logger.LogDebug($"{nameof(OnStopped)} method called");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"{nameof(StopAsync)} method called");

            return Task.CompletedTask;
        }
    }
}

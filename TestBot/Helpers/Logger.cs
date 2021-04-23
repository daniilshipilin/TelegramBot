namespace TelegramBot.TestBot.Helpers
{
    using System;
    using Microsoft.Extensions.Logging;
    using TelegramBot.TestBot.Service;

    public static class Logger
    {
        private static ILogger<HostedService>? logger;

        public static ILogger<HostedService> GetInstance()
        {
            if (logger is null)
            {
                throw new NullReferenceException(nameof(logger));
            }

            return logger;
        }

        public static void InitLogger(ILogger<HostedService> logger)
        {
            if (Logger.logger is null)
            {
                Logger.logger = logger;
            }
        }
    }
}

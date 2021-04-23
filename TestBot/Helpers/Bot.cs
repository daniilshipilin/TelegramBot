namespace TelegramBot.TestBot.Helpers
{
    using System;
    using Telegram.Bot;

    public static class Bot
    {
        private static ITelegramBotClient? client;

        public static ITelegramBotClient GetInstance()
        {
            if (client is null)
            {
                client = new TelegramBotClient(AppSettings.TelegramBotToken)
                {
                    Timeout = new TimeSpan(0, 0, 60),
                };

                client.SetWebhookAsync(string.Empty);
            }

            return client;
        }
    }
}

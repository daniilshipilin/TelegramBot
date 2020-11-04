namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.Net.Http;

    public static class ApiHttpClient
    {
        static ApiHttpClient()
        {
            Client = new HttpClient()
            {
                Timeout = new TimeSpan(0, 0, 60),
            };

            Client.DefaultRequestHeaders.Accept.Clear();
        }

        public static HttpClient Client { get; }
    }
}

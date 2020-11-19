namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Microsoft.Extensions.Configuration;

    public static class AppSettings
    {
        private static IConfiguration? config;

        public static string PicsDirectory => Path.GetFullPath(config.GetValue<string>("ApplicationSettings:PicsDirectory"));

        public static string LoremPicsumApiBaseUrl => config.GetValue<string>("ApplicationSettings:LoremPicsumApiBaseUrl");

        public static string TelegramBotToken => config.GetValue<string>("ApplicationSettings:TelegramBotToken");

        public static string CoronaApiBaseUrl => config.GetValue<string>("ApplicationSettings:CoronaApiBaseUrl");

        public static DateTime SubscriptionTimerTriggeredAt => DateTime.ParseExact(config.GetValue<string>("ApplicationSettings:SubscriptionTimerTriggeredAt"), "HH:mm:ss", CultureInfo.InvariantCulture);

        public static DateTime MaintenanceTimerTriggeredAt => DateTime.ParseExact(config.GetValue<string>("ApplicationSettings:MaintenanceTimerTriggeredAt"), "HH:mm:ss", CultureInfo.InvariantCulture);

        public static DateTime JokeTimerTriggeredAt => DateTime.ParseExact(config.GetValue<string>("ApplicationSettings:JokeTimerTriggeredAt"), "HH:mm:ss", CultureInfo.InvariantCulture);

        public static bool FirstUserGetsAdminRights => config.GetValue<bool>("ApplicationSettings:FirstUserGetsAdminRights");

        public static string RzhunemoguApiBaseUrl => config.GetValue<string>("ApplicationSettings:RzhunemoguApiBaseUrl");

        public static IReadOnlyList<int> RzhunemoguApiArgumentsList => config!.GetSection("ApplicationSettings:RzhunemoguApiArgumentsList").Get<List<int>>();

        public static string DatabaseConnectionString => config.GetConnectionString("Default");

        /// <summary>
        /// Initializes application settings.
        /// </summary>
        public static void InitSettings(IConfiguration configuration)
        {
            config = configuration;
        }
    }
}

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

        public static IReadOnlyList<TimeSpan> SubscriptionTriggers => GetSectionTimeSpan("ApplicationSettings:SubscriptionTriggers");

        public static IReadOnlyList<TimeSpan> MaintenanceTriggers => GetSectionTimeSpan("ApplicationSettings:MaintenanceTriggers");

        public static IReadOnlyList<TimeSpan> JokeTriggers => GetSectionTimeSpan("ApplicationSettings:JokeTriggers");

        public static bool FirstUserGetsAdminRights => config.GetValue<bool>("ApplicationSettings:FirstUserGetsAdminRights");

        public static bool PermissionDeniedForNewUsers => config.GetValue<bool>("ApplicationSettings:PermissionDeniedForNewUsers");

        public static string RzhunemoguApiBaseUrl => config.GetValue<string>("ApplicationSettings:RzhunemoguApiBaseUrl");

        public static IReadOnlyList<string> RzhunemoguApiArguments => GetSectionString("ApplicationSettings:RzhunemoguApiArguments");

        public static string DatabaseConnectionString => config.GetConnectionString("Default");

        /// <summary>
        /// Initializes application settings.
        /// </summary>
        public static void InitSettings(IConfiguration configuration)
        {
            config = configuration;
        }

        private static IReadOnlyList<TimeSpan> GetSectionTimeSpan(string key)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var result = new List<TimeSpan>();

            foreach (var item in config.GetSection(key).Get<List<string>>())
            {
                result.Add(TimeSpan.ParseExact(item, "hh\\:mm\\:ss", CultureInfo.InvariantCulture));
            }

            return result;
        }

        private static IReadOnlyList<string> GetSectionString(string key)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return config.GetSection(key).Get<List<string>>();
        }
    }
}

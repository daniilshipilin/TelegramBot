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

        public static IReadOnlyList<string> CoronaOutputHighlightCountries => GetSectionStrings("ApplicationSettings:CoronaOutputHighlightCountries");

        public static IReadOnlyList<TimeSpan> CoronaUpdateTriggers => GetSectionTimeSpans("ApplicationSettings:CoronaUpdateTriggers");

        public static IReadOnlyList<TimeSpan> MaintenanceTriggers => GetSectionTimeSpans("ApplicationSettings:MaintenanceTriggers");

        public static IReadOnlyList<TimeSpan> JokeTriggers => GetSectionTimeSpans("ApplicationSettings:JokeTriggers");

        public static bool FirstUserGetsAdminRights => config.GetValue<bool>("ApplicationSettings:FirstUserGetsAdminRights");

        public static bool PermissionDeniedForNewUsers => config.GetValue<bool>("ApplicationSettings:PermissionDeniedForNewUsers");

        public static bool SendServiceStartedStoppedMessageToAdminUsers => config.GetValue<bool>("ApplicationSettings:SendServiceStartedStoppedMessageToAdminUsers");

        public static string RzhunemoguApiBaseUrl => config.GetValue<string>("ApplicationSettings:RzhunemoguApiBaseUrl");

        public static IReadOnlyList<string> RzhunemoguApiArguments => GetSectionStrings("ApplicationSettings:RzhunemoguApiArguments");

        public static string DatabaseConnectionString => config.GetConnectionString("Default");

        /// <summary>
        /// Initializes application settings.
        /// </summary>
        public static void InitAppSettings(IConfiguration configuration)
        {
            if (config is null)
            {
                config = configuration;
            }
        }

        private static IReadOnlyList<TimeSpan> GetSectionTimeSpans(string key)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(key).Get<List<string>>() ?? new List<string>();
            var result = new List<TimeSpan>();

            foreach (var item in section)
            {
                result.Add(TimeSpan.ParseExact(item, "hh\\:mm\\:ss", CultureInfo.InvariantCulture));
            }

            return result;
        }

        private static IReadOnlyList<string> GetSectionStrings(string key)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(key).Get<List<string>>() ?? new List<string>();

            return section;
        }
    }
}

namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using TelegramBot.TestBot.Models;

    public class CoronaApi
    {
        private static List<CaseDistributionJson> cachedRecords = new List<CaseDistributionJson>();

        public static IReadOnlyList<CaseDistributionJson> CashedRecords
        {
            get => cachedRecords.AsReadOnly();

            private set
            {
                cachedRecords = value.ToList();
            }
        }

        public static DateTime RecordsCachedDateUtc { get; private set; }

        public static async Task DownloadCoronaCaseDistributionRecordsAsync(bool overrideCachedData)
        {
            // download data, if last download operation was done yesterday
            if (CashedRecords.Count == 0 ||
                (DateTime.UtcNow - RecordsCachedDateUtc).Days >= 1 ||
                overrideCachedData)
            {
                var records = new List<CaseDistributionJson>();
                using var response = await ApiHttpClient.Client.GetAsync(AppSettings.CoronaApiBaseUrl);

                if (response.IsSuccessStatusCode)
                {
                    // read response in json format
                    string json = await response.Content.ReadAsStringAsync();

                    // create string list for possible errors during json processing
                    var errors = new List<string>();
                    var settings = new JsonSerializerSettings()
                    {
                        Error = (sender, args) =>
                        {
                            // put registered errors in created string list
                            errors.Add(args.ErrorContext.Error.Message);
                            args.ErrorContext.Handled = true;
                        },
                    };

                    // deserialize received json
                    records = JsonConvert.DeserializeObject<List<CaseDistributionJson>>(json, settings);

                    if (errors.Count > 0)
                    {
                        throw new Exception($"JSON deserialization failed{Environment.NewLine}" +
                                            $"{string.Join(Environment.NewLine, errors)}");
                    }
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }

                // filter records before assigning
                CashedRecords = records
                    .Where(x => x.Continent.Equals("Europe"))
                    .GroupBy(x => x.Country)
                    .Select(x => x.Last())
                    .OrderByDescending(x => x.Rate14Day)
                    .ThenBy(x => x.Country)
                    .ToList();

                RecordsCachedDateUtc = DateTime.UtcNow;
            }
        }
    }
}

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
        private static readonly StringComparison Sc = StringComparison.InvariantCultureIgnoreCase;
        private static List<CaseDistributionJson.Record> cachedRecords = new List<CaseDistributionJson.Record>();

        public static IReadOnlyList<CaseDistributionJson.Record> CashedRecords => cachedRecords;

        public static DateTime RecordsCachedDateUtc { get; private set; }

        public static async Task DownloadCoronaCaseDistributionRecordsAsync(bool overrideCachedData)
        {
            // download data, if last download operation was done yesterday
            if (cachedRecords.Count == 0 ||
                (DateTime.UtcNow - RecordsCachedDateUtc).Days >= 1 ||
                overrideCachedData)
            {
                var jsonObj = new CaseDistributionJson();
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
                    jsonObj = JsonConvert.DeserializeObject<CaseDistributionJson>(json, settings);

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

                // filter records
                var records = jsonObj.Records
                    .Where(x => x.ContinentExp.Equals("Europe", Sc))
                    .GroupBy(x => x.CountriesAndTerritories)
                    .Select(x => x.First())
                    .OrderByDescending(x => x.CumulativeNumber)
                    .ThenBy(x => x.CountriesAndTerritories)
                    .ToList();

                cachedRecords = records;
                RecordsCachedDateUtc = DateTime.UtcNow;
            }
        }
    }
}

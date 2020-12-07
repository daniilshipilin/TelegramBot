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

        // cashed records
        private static IList<CaseDistributionJson.Record> cashedRecords = new List<CaseDistributionJson.Record>();
        private static DateTime recordsCachedDateUtc = DateTime.UtcNow;

        public static async Task<(IList<CaseDistributionJson.Record>, DateTime)> DownloadCoronaCaseDistributionRecords(bool overrideCachedData)
        {
            // download data, if last download operation was done yesterday
            if (cashedRecords.Count == 0 ||
                (DateTime.UtcNow.Date - recordsCachedDateUtc.Date).Days >= 1 ||
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

                // compare with cached records
                if (cashedRecords.Count > 0)
                {
                    foreach (var record in records)
                    {
                        var cachedRecord = cashedRecords.ToList().Find(x => x.CountriesAndTerritories.Equals(record.CountriesAndTerritories, Sc));

                        if (cachedRecord is not null)
                        {
                            record.CumulativeNumberIncrease = record.CumulativeNumber > cachedRecord.CumulativeNumber;
                            record.CumulativeNumberIncreasePercentage = 1 - (cachedRecord.CumulativeNumber / record.CumulativeNumber);
                        }
                    }
                }

                cashedRecords = records;
                recordsCachedDateUtc = DateTime.UtcNow;
            }

            return (cashedRecords, recordsCachedDateUtc);
        }
    }
}

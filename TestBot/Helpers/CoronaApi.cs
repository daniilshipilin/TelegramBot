namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using TelegramBot.TestBot.Models;

    public class CoronaApi
    {
        // cashed records
        private static List<CaseDistributionJson.Record>? caseDistributionRecords;
        private static DateTime recordsCachedDateUtc = DateTime.UtcNow;

        public static async Task<(List<CaseDistributionJson.Record>, DateTime)> DownloadCoronaCaseDistributionRecords(bool overrideCachedData)
        {
            // download data, if last download operation was done yesterday
            if (caseDistributionRecords is null ||
                caseDistributionRecords.Count == 0 ||
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

                // select the records from specific region
                var records = jsonObj.Records.FindAll(i => i.ContinentExp.Equals("Europe", StringComparison.InvariantCultureIgnoreCase));
                var newCaseDistributionRecords = new List<CaseDistributionJson.Record>();

                foreach (var record in records)
                {
                    // skip already added country recent record
                    if (!newCaseDistributionRecords.Exists(x => x.CountriesAndTerritories.Equals(record.CountriesAndTerritories, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        newCaseDistributionRecords.Add(record);
                    }
                }

                caseDistributionRecords = newCaseDistributionRecords;
                recordsCachedDateUtc = DateTime.UtcNow;
            }

            return (caseDistributionRecords, recordsCachedDateUtc);
        }
    }
}

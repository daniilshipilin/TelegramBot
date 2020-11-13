namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using TelegramBot.TestBot.Models;

    public class CoronaApi
    {
        public static async Task<DB_CoronaCaseDistributionRecords> DownloadCoronaCaseDistributionRecords(bool overrideCachedData)
        {
            if (DatabaseAccess.DB is null)
            {
                throw new NullReferenceException(nameof(DatabaseAccess.DB));
            }

            DB_CoronaCaseDistributionRecords dbRecord;
            string timestamp = DatabaseAccess.DB.Select_CoronaCaseDistributionRecordsLastTimestamp();
            var lastRecordDateUtc = (timestamp is object) ? DateTime.ParseExact(timestamp, "u", CultureInfo.InvariantCulture) : new DateTime(1, 1, 1);

            // download data, if last download operation was done more than hour ago
            if ((DateTime.UtcNow - lastRecordDateUtc).Hours >= 1 || overrideCachedData)
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
                var caseDistributionRecords = new List<CaseDistributionJson.Record>();

                foreach (var record in records)
                {
                    // skip already added country record
                    if (!caseDistributionRecords.Exists(x => x.CountriesAndTerritories.Equals(record.CountriesAndTerritories, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        caseDistributionRecords.Add(record);
                    }
                }

                // sort list records
                caseDistributionRecords = caseDistributionRecords.OrderByDescending(i => i.CumulativeNumber).ToList();

                // generate output message
                var sb = new StringBuilder();
                sb.AppendLine($"<b>COVID-19 situation update</b>");
                sb.AppendLine("Timestamp\tCountry\tCumulativeNumber");
                sb.Append("<pre>");

                caseDistributionRecords.ForEach(i => sb.AppendLine($"{i.TimeStamp:yyyy-MM-dd}\t{i.CountriesAndTerritories,-12}\t{i.CumulativeNumber:0.00}"));

                sb.AppendLine("</pre>");
                sb.AppendLine($"{caseDistributionRecords.Count} record(s) in total.");

                dbRecord = new DB_CoronaCaseDistributionRecords(sb.ToString());
                DatabaseAccess.DB.Insert_CoronaCaseDistributionRecords(dbRecord);
            }
            else
            {
                dbRecord = DatabaseAccess.DB.Select_CoronaCaseDistributionRecords();
            }

            return dbRecord;
        }
    }
}

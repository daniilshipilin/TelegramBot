namespace TelegramBot.TestBot.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class CaseDistributionJson
    {
        [JsonProperty("records")]
        public IList<Record> Records { get; init; } = new List<Record>();

        public class Record
        {
            [JsonProperty("dateRep")]
            public string ReportDate { get; init; } = string.Empty;

            [JsonProperty("cases_weekly")]
            public int CasesWeekly { get; init; }

            [JsonProperty("deaths_weekly")]
            public int DeathsWeekly { get; init; }

            [JsonProperty("countriesAndTerritories")]
            public string CountriesAndTerritories { get; init; } = string.Empty;

            [JsonProperty("continentExp")]
            public string ContinentExp { get; init; } = string.Empty;

            [JsonProperty("notification_rate_per_100000_population_14-days")]
            public string CumulativeNumberFor14Days { get; init; } = string.Empty;

            public double CumulativeNumber
            {
                get
                {
                    if (double.TryParse(CumulativeNumberFor14Days, out double result))
                    {
                        return result;
                    }

                    return 0;
                }
            }
        }
    }
}

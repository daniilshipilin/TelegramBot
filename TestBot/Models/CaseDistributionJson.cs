namespace TelegramBot.TestBot.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class CaseDistributionJson
    {
        [JsonProperty("records")]
        public IList<Record> Records { get; init; } = new List<Record>();

        public class Record
        {
            [JsonProperty("dateRep")]
            public string DateRep { get; init; } = string.Empty;

            [JsonProperty("day")]
            public int Day { get; init; }

            [JsonProperty("month")]
            public int Month { get; init; }

            [JsonProperty("year")]
            public int Year { get; init; }

            [JsonProperty("cases")]
            public int Cases { get; init; }

            [JsonProperty("deaths")]
            public int Deaths { get; init; }

            [JsonProperty("countriesAndTerritories")]
            public string CountriesAndTerritories { get; init; } = string.Empty;

            [JsonProperty("geoId")]
            public string GeoId { get; init; } = string.Empty;

            [JsonProperty("countryterritoryCode")]
            public string CountryterritoryCode { get; init; } = string.Empty;

            [JsonProperty("popData2019")]
            public int? PopData2019 { get; init; }

            [JsonProperty("continentExp")]
            public string ContinentExp { get; init; } = string.Empty;

            [JsonProperty("Cumulative_number_for_14_days_of_COVID-19_cases_per_100000")]
            public string CumulativeNumberFor14Days { get; init; } = string.Empty;

            public DateTime TimeStamp => DateTime.ParseExact(DateRep, "dd/MM/yyyy", CultureInfo.InvariantCulture);

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

            public bool CumulativeNumberIncrease { get; set; }

            public double CumulativeNumberIncreasePercentage { get; set; }
        }
    }
}

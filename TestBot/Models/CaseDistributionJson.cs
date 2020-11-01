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
        public List<Record> Records { get; set; } = new List<Record>();

        public class Record
        {
            [JsonProperty("dateRep")]
            public string DateRep { get; set; } = string.Empty;

            [JsonProperty("day")]
            public int Day { get; set; }

            [JsonProperty("month")]
            public int Month { get; set; }

            [JsonProperty("year")]
            public int Year { get; set; }

            [JsonProperty("cases")]
            public int Cases { get; set; }

            [JsonProperty("deaths")]
            public int Deaths { get; set; }

            [JsonProperty("countriesAndTerritories")]
            public string CountriesAndTerritories { get; set; } = string.Empty;

            [JsonProperty("geoId")]
            public string GeoId { get; set; } = string.Empty;

            [JsonProperty("countryterritoryCode")]
            public string CountryterritoryCode { get; set; } = string.Empty;

            [JsonProperty("popData2019")]
            public int? PopData2019 { get; set; }

            [JsonProperty("continentExp")]
            public string ContinentExp { get; set; } = string.Empty;

            [JsonProperty("Cumulative_number_for_14_days_of_COVID-19_cases_per_100000")]
            public string CumulativeNumberFor14Days { get; set; } = string.Empty;

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
        }
    }
}

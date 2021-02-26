namespace TelegramBot.TestBot.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class CaseDistributionJson
    {
        [JsonProperty("year_week")]
        public string YearWeek { get; init; } = string.Empty;

        [JsonProperty("weekly_count")]
        public int WeeklyCount { get; init; }

        [JsonProperty("cumulative_count")]
        public int CumulativeCount { get; init; }

        [JsonProperty("country")]
        public string Country { get; init; } = string.Empty;

        [JsonProperty("continent")]
        public string Continent { get; init; } = string.Empty;

        [JsonProperty("rate_14_day")]
        public string CumulativeNumberFor14Days { get; init; } = string.Empty;

        public double Rate14Day
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

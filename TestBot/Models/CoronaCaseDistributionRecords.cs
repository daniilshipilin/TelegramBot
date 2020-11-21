namespace TelegramBot.TestBot.Models
{
    using System;

    public class CoronaCaseDistributionRecords
    {
        public DateTime DateCollectedUtc { get; } = DateTime.UtcNow;

        public string CaseDistributionRecords { get; init; } = string.Empty;
    }
}

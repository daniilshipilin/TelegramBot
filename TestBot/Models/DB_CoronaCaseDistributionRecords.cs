namespace TelegramBot.TestBot.Models
{
    using System;
    using System.Globalization;

    public class DB_CoronaCaseDistributionRecords
    {
        public DB_CoronaCaseDistributionRecords()
        {
            // default ctor
        }

        public DB_CoronaCaseDistributionRecords(string caseDistributionRecords)
        {
            CaseDistributionRecords = caseDistributionRecords;
        }

        public int CaseId { get; set; }

        public string DateCollectedUtc { get; set; } = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture);

        public string CaseDistributionRecords { get; set; } = string.Empty;
    }
}

using System;
using System.Globalization;

namespace TelegramBot.Models
{
    public class DB_CoronaCaseDistributionRecords
    {
        public int CaseId { get; set; }
        public string DateCollectedUtc { get; set; } = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture);
        public string CaseDistributionRecords { get; set; } = string.Empty;

        public DB_CoronaCaseDistributionRecords()
        {

        }

        public DB_CoronaCaseDistributionRecords(string caseDistributionRecords)
        {
            CaseDistributionRecords = caseDistributionRecords;
        }
    }
}

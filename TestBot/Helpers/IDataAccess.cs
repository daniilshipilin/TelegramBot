namespace TelegramBot.TestBot.Helpers
{
    using System.Collections.Generic;
    using TelegramBot.TestBot.Models;

    public interface IDataAccess
    {
        int DBVersion { get; }

        string ConnectionString { get; }

        string DBFilePath { get; }

        void DbCompact();

        void TestDBAccess();

        void CreateDefaultDB();

        IList<DB_Settings> Select_Settings();
    }
}

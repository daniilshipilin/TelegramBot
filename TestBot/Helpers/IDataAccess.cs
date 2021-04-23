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

        IList<DB_TelegramUsers> Select_TelegramUsers();

        IList<DB_TelegramUsers> Select_TelegramUsersIsSubscribedToCoronaUpdates();

        IList<DB_TelegramUsers> Select_TelegramUsersIsAdministrator();

        DB_TelegramUsers Select_TelegramUsers(long chatId);

        int Count_TelegramUsers();

        DB_SqliteSequence LastIndex_TelegramUsers();

        void Insert_TelegramUsers(DB_TelegramUsers user);

        void Update_TelegramUsers(DB_TelegramUsers user);

        void Delete_TelegramUsers(DB_TelegramUsers user);
    }
}

namespace TelegramBot.TestBot.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Dapper;
    using Microsoft.Data.Sqlite;
    using TelegramBot.TestBot.Models;

    public class DataAccess : DataAccessBase
    {
        public DataAccess(string connectionString)
            : base(connectionString)
        {
        }

        public override int DBVersion => 6;

        public IList<DB_TelegramUsers> Select_TelegramUsers()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM TelegramUsers;";

            return db.Query<DB_TelegramUsers>(sql).ToList();
        }

        public IList<DB_TelegramUsers> Select_TelegramUsersIsSubscribed()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM TelegramUsers " +
                         $"WHERE UserIsSubscribed = 1;";

            return db.Query<DB_TelegramUsers>(sql).ToList();
        }

        public IList<DB_TelegramUsers> Select_TelegramUsersIsAdministrator()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM TelegramUsers " +
                         $"WHERE UserIsAdmin = 1;";

            return db.Query<DB_TelegramUsers>(sql).ToList();
        }

        public DB_TelegramUsers Select_TelegramUsers(long chatId)
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM TelegramUsers " +
                         $"WHERE ChatId = {chatId};";

            return db.QueryFirstOrDefault<DB_TelegramUsers>(sql);
        }

        public CoronaCaseDistributionRecords Select_CoronaCaseDistributionRecords()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM CoronaCaseDistributionRecords " +
                         "ORDER BY CaseId DESC LIMIT 1;";

            return db.QueryFirstOrDefault<CoronaCaseDistributionRecords>(sql);
        }

        public string Select_CoronaCaseDistributionRecordsLastTimestamp()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT DateCollectedUtc " +
                         "FROM CoronaCaseDistributionRecords " +
                         "ORDER BY CaseId DESC LIMIT 1;";

            return db.ExecuteScalar<string>(sql);
        }

        public int Count_TelegramUsers()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT COUNT(*) " +
                         "FROM TelegramUsers;";

            return db.ExecuteScalar<int>(sql);
        }

        public DB_SqliteSequence LastIndex_TelegramUsers()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM sqlite_sequence " +
                         "WHERE name = 'TelegramUsers';";

            return db.QueryFirstOrDefault<DB_SqliteSequence>(sql);
        }

        public void Insert_CoronaCaseDistributionRecords(CoronaCaseDistributionRecords record)
        {
            using var db = new SqliteConnection(ConnectionString);

            string sql = "INSERT INTO CoronaCaseDistributionRecords (DateCollectedUtc, CaseDistributionRecords) " +
                         "VALUES (@DateCollectedUtc, @CaseDistributionRecords);";

            db.Execute(sql, record);
        }

        public void Insert_TelegramUsers(DB_TelegramUsers user)
        {
            using var db = new SqliteConnection(ConnectionString);

            string sql = "INSERT INTO TelegramUsers (ChatId, FirstName, LastName, UserName, DateRegisteredUtc, UserIsSubscribed, UserIsAdmin, UserLocationLatitude, UserLocationLongitude) " +
                         "VALUES (@ChatId, @FirstName, @LastName, @UserName, @DateRegisteredUtc, @UserIsSubscribed, @UserIsAdmin, @UserLocationLatitude, @UserLocationLongitude);";

            db.Execute(sql, user);
        }

        public void Update_TelegramUsers(DB_TelegramUsers user)
        {
            using var db = new SqliteConnection(ConnectionString);

            string sql = "UPDATE TelegramUsers " +
                         "SET FirstName = @FirstName, LastName = @LastName, UserName = @UserName, UserIsSubscribed = @UserIsSubscribed, UserIsAdmin = @UserIsAdmin, UserLocationLatitude = @UserLocationLatitude, UserLocationLongitude = @UserLocationLongitude " +
                         "WHERE ChatId = @ChatId;";

            db.Execute(sql, user);
        }

        public void Delete_TelegramUsers(DB_TelegramUsers user)
        {
            using var db = new SqliteConnection(ConnectionString);

            string sql = "DELETE FROM TelegramUsers " +
                         "WHERE ChatId = @ChatId;";

            db.Execute(sql, user);
        }
    }
}

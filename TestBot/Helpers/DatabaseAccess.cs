namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Dapper;
    using Microsoft.Data.Sqlite;
    using TelegramBot.TestBot.Models;

    public class DatabaseAccess
    {
        public const int DBVersion = 5;

        private DatabaseAccess(string connectionString)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            ConnectionString = builder.ConnectionString;
            DBFilePath = Path.GetFullPath(builder.DataSource);
            CheckDBExists();
            TestDBAccess();
        }

        public static DatabaseAccess? DB { get; private set; }

        public string ConnectionString { get; }

        public string DBFilePath { get; }

        public static void InitDatabaseAccess(string connectionString)
        {
            DB = new DatabaseAccess(connectionString);
        }

        public void DbCompact()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "VACUUM main;";

            db.Execute(sql);
        }

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

        public DB_CoronaCaseDistributionRecords Select_CoronaCaseDistributionRecords()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM CoronaCaseDistributionRecords " +
                         "ORDER BY CaseId DESC LIMIT 1;";

            return db.QueryFirstOrDefault<DB_CoronaCaseDistributionRecords>(sql);
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

        public void Insert_CoronaCaseDistributionRecords(DB_CoronaCaseDistributionRecords record)
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

        private void CheckDBExists()
        {
            if (!File.Exists(DBFilePath))
            {
                // create new default db & apply default db schema
                CreateDBSchema();
            }
        }

        private void TestDBAccess()
        {
            var settings = Select_Settings();

            foreach (var setting in settings)
            {
                if (setting.Key.Equals("DB_VERSION"))
                {
                    int currentVer = int.Parse(setting.Value);

                    if (currentVer != DBVersion)
                    {
                        // save previous db
                        File.Move(DBFilePath, $"{DBFilePath}_V{currentVer}_{DateTime.UtcNow:yyyyMMddhhmmssfff}.backup");

                        // db version mismatch - create new 'clean' db
                        CreateDBSchema();
                    }
                }
            }
        }

        private void CreateDBSchema()
        {
            var assembly = Assembly.GetEntryAssembly();

            if (assembly is object)
            {
                using var sr = assembly.GetManifestResourceStream("TelegramBot.TestBot.DB.Users.sql");

                if (sr is object)
                {
                    using var reader = new StreamReader(sr, new UTF8Encoding(false));
                    string sql = reader.ReadToEnd();

                    using var db = new SqliteConnection(ConnectionString);
                    db.Execute(sql);
                }
            }
        }

        private IList<DB_Settings> Select_Settings()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM Settings;";

            return db.Query<DB_Settings>(sql).ToList();
        }
    }
}

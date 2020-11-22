namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Dapper;
    using Microsoft.Data.Sqlite;
    using TelegramBot.TestBot.Models;

    public class SqliteDataAccess : IDataAccess
    {
        public SqliteDataAccess(string connectionString)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            ConnectionString = builder.ConnectionString;
            DBFilePath = Path.GetFullPath(builder.DataSource);
            TestDBAccess();
        }

        public int DBVersion => 6;

        public string ConnectionString { get; }

        public string DBFilePath { get; }

        public void DbCompact()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "VACUUM main;";

            db.Execute(sql);
        }

        public void TestDBAccess()
        {
            if (!File.Exists(DBFilePath))
            {
                CreateDefaultDB();
            }
            else
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

                            // create default db
                            CreateDefaultDB();
                        }
                    }
                }
            }
        }

        public void CreateDefaultDB()
        {
            var file = new FileInfo(DBFilePath);
            file.Directory?.Create();

            var assembly = Assembly.GetEntryAssembly();

            if (assembly is not null)
            {
                // create new 'clean' db
                using var sr1 = assembly.GetManifestResourceStream("TelegramBot.TestBot.DB.Empty.db");

                if (sr1 is not null)
                {
                    using var fs = File.Create(DBFilePath);
                    sr1.Seek(0, SeekOrigin.Begin);
                    sr1.CopyTo(fs);
                }

                // apply db schema
                using var sr2 = assembly.GetManifestResourceStream("TelegramBot.TestBot.DB.Users.sql");

                if (sr2 is not null)
                {
                    using var reader = new StreamReader(sr2);
                    string sql = reader.ReadToEnd();
                    using var db = new SqliteConnection(ConnectionString);
                    db.Execute(sql);
                }
            }
        }

        public IList<DB_Settings> Select_Settings()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM Settings;";

            return db.Query<DB_Settings>(sql).ToList();
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

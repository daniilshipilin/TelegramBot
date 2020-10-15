using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper;
using TelegramBot.Models;

namespace TelegramBot
{
    public class SQLiteDBAccess
    {
        const int DB_VERSION = 3;

        readonly string _connectionString;

        public SQLiteDBAccess(string connectionString)
        {
            _connectionString = connectionString;
            CheckDBExists();
            TestDBAccess();
        }

        private void CheckDBExists()
        {
            var builder = new SQLiteConnectionStringBuilder(_connectionString);
            string dbFilePath = Path.GetFullPath(builder.DataSource);

            if (!File.Exists(dbFilePath))
            {
                if (!Directory.Exists(Path.GetDirectoryName(dbFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dbFilePath));
                }

                var assembly = Assembly.GetEntryAssembly();

                if (assembly is object)
                {
                    using var sr = assembly.GetManifestResourceStream("TelegramBot.DB.Users_empty.db");

                    if (sr is object)
                    {
                        // create new default db
                        using (var fs = File.Create(dbFilePath))
                        {
                            sr.Seek(0, SeekOrigin.Begin);
                            sr.CopyTo(fs);
                        }

                        // apply default db schema
                        CreateDBSchema();
                    }
                }
            }
        }

        private void TestDBAccess()
        {
            var settings = Select_Settings();

            foreach (var setting in settings)
            {
                if (setting.Key.Equals("DB_VERSION"))
                {
                    if (int.Parse(setting.Value) != DB_VERSION)
                    {
                        throw new Exception("DB version mismatch");
                    }
                }
            }
        }

        #region DB queries

        private void CreateDBSchema()
        {
            var assembly = Assembly.GetEntryAssembly();

            if (assembly is object)
            {
                using var sr = assembly.GetManifestResourceStream("TelegramBot.DB.Users.sql");

                if (sr is object)
                {
                    using var reader = new StreamReader(sr, new UTF8Encoding(false));
                    string sql = reader.ReadToEnd();

                    using IDbConnection db = new SQLiteConnection(_connectionString);
                    db.Execute(sql);
                }
            }
        }

        private IList<DB_Settings> Select_Settings()
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "SELECT * " +
                         "FROM Settings;";

            return db.Query<DB_Settings>(sql).ToList();
        }

        public void DbCompact()
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "VACUUM main;";

            db.Execute(sql);
        }

        public IList<DB_TelegramUsers> Select_TelegramUsers()
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "SELECT * " +
                         "FROM TelegramUsers;";

            return db.Query<DB_TelegramUsers>(sql).ToList();
        }

        public IList<DB_TelegramUsers> Select_TelegramUsersIsSubscribed()
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "SELECT * " +
                         "FROM TelegramUsers " +
                         $"WHERE UserIsSubscribed = 'True';";

            return db.Query<DB_TelegramUsers>(sql).ToList();
        }

        public IList<DB_TelegramUsers> Select_TelegramUsersIsAdministrator()
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "SELECT * " +
                         "FROM TelegramUsers " +
                         $"WHERE UserIsAdmin = 'True';";

            return db.Query<DB_TelegramUsers>(sql).ToList();
        }

        public DB_TelegramUsers Select_TelegramUsers(long chatId)
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "SELECT * " +
                         "FROM TelegramUsers " +
                         $"WHERE ChatId = {chatId};";

            return db.QueryFirstOrDefault<DB_TelegramUsers>(sql);
        }

        public DB_CoronaCaseDistributionRecords Select_CoronaCaseDistributionRecords()
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "SELECT * " +
                         "FROM CoronaCaseDistributionRecords " +
                         "ORDER BY CaseID DESC LIMIT 1;";

            return db.QueryFirstOrDefault<DB_CoronaCaseDistributionRecords>(sql);
        }

        public string Select_CoronaCaseDistributionRecordsLastTimestamp()
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "SELECT DateCollectedUtc " +
                         "FROM CoronaCaseDistributionRecords " +
                         "ORDER BY CaseID DESC LIMIT 1;";

            return db.ExecuteScalar<string>(sql);
        }

        public int Count_TelegramUsers()
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);
            string sql = "SELECT COUNT(*) " +
                         "FROM TelegramUsers;";

            return db.ExecuteScalar<int>(sql);
        }

        public void Insert_CoronaCaseDistributionRecords(DB_CoronaCaseDistributionRecords record)
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);

            string sql = "INSERT INTO CoronaCaseDistributionRecords (DateCollectedUtc, CaseDistributionRecords) " +
                         "VALUES (@DateCollectedUtc, @CaseDistributionRecords);";

            db.Execute(sql, record);
        }

        public void Insert_TelegramUsers(DB_TelegramUsers user)
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);

            string sql = "INSERT INTO TelegramUsers (ChatId, FirstName, LastName, UserName, DateRegisteredUtc, UserIsSubscribed, UserIsAdmin) " +
                         "VALUES (@ChatId, @FirstName, @LastName, @UserName, @DateRegisteredUtc, @UserIsSubscribed, @UserIsAdmin);";

            db.Execute(sql, user);
        }

        public void Update_TelegramUsers(DB_TelegramUsers user)
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);

            string sql = "UPDATE TelegramUsers " +
                         "SET FirstName = @FirstName, LastName = @LastName, UserName = @UserName, UserIsSubscribed = @UserIsSubscribed, UserIsAdmin = @UserIsAdmin " +
                         "WHERE ChatId = @ChatId;";

            db.Execute(sql, user);
        }

        public void Delete_TelegramUsers(DB_TelegramUsers user)
        {
            using IDbConnection db = new SQLiteConnection(_connectionString);

            string sql = "DELETE FROM TelegramUsers " +
                           "WHERE ChatId = @ChatId;";

            db.Execute(sql, user);
        }

        #endregion
    }
}

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

    public abstract class DataAccessBase
    {
        public DataAccessBase(string connectionString)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            ConnectionString = builder.ConnectionString;
            DBFilePath = Path.GetFullPath(builder.DataSource);
            TestDBAccess();
        }

        public abstract int DBVersion { get; }

        public string ConnectionString { get; }

        public string DBFilePath { get; }

        public void DbCompact()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "VACUUM main;";

            db.Execute(sql);
        }

        private void TestDBAccess()
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

        private void CreateDefaultDB()
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

        private IList<DB_Settings> Select_Settings()
        {
            using var db = new SqliteConnection(ConnectionString);
            string sql = "SELECT * " +
                         "FROM Settings;";

            return db.Query<DB_Settings>(sql).ToList();
        }
    }
}

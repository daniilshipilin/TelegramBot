namespace TelegramBot.TestBot.Helpers
{
    public static class DataAccess
    {
        private static IDataAccess? db;

        public static IDataAccess GetInstance()
        {
            if (db is null)
            {
                db = new SqliteDataAccess(AppSettings.DatabaseConnectionString);
            }

            return db;
        }
    }
}

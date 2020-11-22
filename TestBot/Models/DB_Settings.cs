namespace TelegramBot.TestBot.Models
{
    public class DB_Settings
    {
        public int SettingId { get; init; }

        public string Key { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;
    }
}

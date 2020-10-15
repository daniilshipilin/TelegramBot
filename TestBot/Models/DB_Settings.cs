namespace TelegramBot.Models
{
    public class DB_Settings
    {
        public int SettingId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}

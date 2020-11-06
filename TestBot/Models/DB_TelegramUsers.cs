namespace TelegramBot.TestBot.Models
{
    using System;
    using System.Globalization;

    public class DB_TelegramUsers
    {
        public int UserId { get; set; }

        public long ChatId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string DateRegisteredUtc { get; set; } = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture);

        public bool UserIsSubscribed { get; set; } = true;

        public bool UserIsAdmin { get; set; } = false;

        public float UserLocationLatitude { get; set; }

        public float UserLocationLongitude { get; set; }
    }
}

namespace TelegramBot.TestBot.Models
{
    using System;
    using System.Globalization;

    public class DB_TelegramUsers
    {
        public int UserId { get; init; }

        public long ChatId { get; init; }

        public string FirstName { get; init; } = string.Empty;

        public string LastName { get; init; } = string.Empty;

        public string UserName { get; init; } = string.Empty;

        public string DateRegisteredUtc { get; init; } = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture);

        public bool UserIsSubscribed { get; init; }

        public bool UserIsAdmin { get; init; }

        public float UserLocationLatitude { get; set; }

        public float UserLocationLongitude { get; set; }
    }
}

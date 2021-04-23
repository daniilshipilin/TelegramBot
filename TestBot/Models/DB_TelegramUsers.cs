namespace TelegramBot.TestBot.Models
{
    using System;
    using System.Globalization;

    public class DB_TelegramUsers
    {
        public int Id { get; init; }

        public long ChatId { get; init; }

        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? UserName { get; init; }

        public string DateRegisteredUtc { get; init; } = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture);

        public bool UserIsSubscribedToCoronaUpdates { get; init; }

        public bool UserIsAdmin { get; init; }

        public float? UserLocationLatitude { get; set; }

        public float? UserLocationLongitude { get; set; }
    }
}

using System;
using System.Globalization;

namespace TelegramBot.Models
{
    public class DB_TelegramUsers
    {
        public int UserId { get; set; }
        public long ChatId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DateRegisteredUtc { get; set; } = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture);
        public string UserIsSubscribed { get; set; } = true.ToString();
        public string UserIsAdmin { get; set; } = false.ToString();

        public DB_TelegramUsers()
        {

        }

        public DB_TelegramUsers(long chatId, string firstName, string lastName, string userName)
        {
            ChatId = chatId;
            FirstName = firstName;
            LastName = lastName;
            UserName = userName;
        }
    }
}

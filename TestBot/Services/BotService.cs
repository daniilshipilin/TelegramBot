namespace TelegramBot.TestBot.Service
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Timers;
    using Microsoft.Extensions.Logging;
    using Telegram.Bot;
    using Telegram.Bot.Args;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types.InputFiles;
    using TelegramBot.TestBot.Helpers;
    using TelegramBot.TestBot.Models;

    public class BotService
    {
        private readonly IList<Timer> notificationTimers = new List<Timer>();
        private readonly ILogger<HostedService> logger;
        private readonly ITelegramBotClient bot;
        private readonly IDataAccess db;
        private readonly DateTime clientStartedDateUtc;

        public BotService()
        {
            logger = Logger.GetInstance();
            bot = Bot.GetInstance();
            db = DataAccess.GetInstance();

            clientStartedDateUtc = DateTime.UtcNow;

            SetupTimers();
        }

        public void PrintBotInfo()
        {
            var botInfo = bot.GetMeAsync().Result;
            logger.LogInformation($"TelegramBot v{GitVersionInformation.InformationalVersion}");
            logger.LogInformation($"Id: {botInfo.Id}\tName: '{botInfo.FirstName}'\tUsername: '{botInfo.Username}'");
            int users = db.Count_TelegramUsers();
            logger.LogInformation($"{users} user(s) found in db");
        }

        public void StartReceiving()
        {
            logger.LogDebug($"{nameof(StartReceiving)} method called");

            SubscribeBotClientEvents();
            bot.StartReceiving();

            if (AppSettings.SendServiceStartedStoppedMessageToAdminUsers)
            {
                NotifyAdministrators($"'{bot.GetMeAsync().Result.FirstName}' started receiving at {DateTime.UtcNow:u}");
            }
        }

        public void StopReceiving()
        {
            logger.LogDebug($"{nameof(StopReceiving)} method called");

            if (AppSettings.SendServiceStartedStoppedMessageToAdminUsers)
            {
                NotifyAdministrators($"'{bot.GetMeAsync().Result.FirstName}' stopped receiving at {DateTime.UtcNow:u}");
            }

            UnSubscribeBotClientEvents();
            bot.StopReceiving();
        }

        private static double CalculateInterval(TimeSpan trigger)
        {
            double diffMs = (trigger - DateTime.UtcNow.TimeOfDay).TotalMilliseconds;

            if (diffMs <= 0D)
            {
                return TimeSpan.FromDays(1).TotalMilliseconds + diffMs; // (one day milliseconds) - (current diff)
            }

            return diffMs;
        }

        private static string GetBotInfo() =>
            $"TelegramBot v{GitVersionInformation.SemVer} made by @daniilshipilin.\n" +
            "This bot supports following commands:\n" +
            "  <b>/start</b> - subscribe to receive messages from the bot;\n" +
            "  <b>/stop</b> - stop receiving messages from the bot;\n" +
            "  <b>/help</b> - display help info;\n" +
            "  <b>/uptime</b> - display service uptime info;\n" +
            "  <b>/date</b> - show current date in UTC format;\n" +
            "  <b>/pic</b> - receive random picture;\n" +
            "  <b>/corona</b> - get current corona situation update;\n" +
            "  <b>/joke</b> - get random joke;\n" +
            "  <b>/fuelcost</b> - fuel consumption calculator.";

        private static string GetPermissionDeniedMessage() =>
            "<pre>" +
            "###################################\n" +
            "#         RESTRICTED AREA         #\n" +
            "#    NO UNAUTHORIZED PERSONNEL    #\n" +
            "#       BEYOUND THIS POINT        #\n" +
            "###################################" +
            "</pre>";

        private static void ResetTimerInterval(Timer timer)
        {
            timer.Interval = TimeSpan.FromDays(1).TotalMilliseconds;
            timer.Enabled = true;
        }

        private static void CreateTimers(IReadOnlyList<TimeSpan> triggers, Action<object, ElapsedEventArgs> method, IList<Timer>? timers = null)
        {
            foreach (var trigger in triggers)
            {
                var timer = new Timer();
                timer.Elapsed += new ElapsedEventHandler(method);
                timer.AutoReset = false;
                timer.Interval = CalculateInterval(trigger);
                timer.Enabled = true;

                timers?.Add(timer);
            }
        }

        private static IEnumerable<string> GetFiles(string path, string searchPatternExpression, SearchOption searchOption)
        {
            var reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);

            return Directory.EnumerateFiles(path, "*", searchOption)
                .Where(file => reSearchPattern.IsMatch(Path.GetExtension(file)));
        }

        private static double GetTotalAllocatedMemoryInMBytes()
        {
            using var p = Process.GetCurrentProcess();

            return p.PrivateMemorySize64 / 1048576D;
        }

        private void SetupTimers()
        {
            CreateTimers(AppSettings.CoronaUpdateTriggers, CoronaUpdateEvent, notificationTimers);
            CreateTimers(AppSettings.MaintenanceTriggers, MaintenanceEvent, notificationTimers);
            CreateTimers(AppSettings.JokeTriggers, JokeEvent, notificationTimers);
        }

        private void SubscribeBotClientEvents()
        {
            bot.OnMessage += OnMessageEvent;
            bot.OnMessageEdited += OnMessageEditedEvent;
            bot.OnUpdate += OnUpdateEvent;
            bot.OnReceiveError += OnReceiveErrorEvent;
        }

        private void UnSubscribeBotClientEvents()
        {
            bot.OnMessage -= OnMessageEvent;
            bot.OnMessageEdited -= OnMessageEditedEvent;
            bot.OnUpdate -= OnUpdateEvent;
            bot.OnReceiveError -= OnReceiveErrorEvent;
        }

        private void CoronaUpdateEvent(object sender, ElapsedEventArgs e)
        {
            ResetTimerInterval((Timer)sender);

            logger.LogDebug($"{nameof(CoronaUpdateEvent)} method called");

            NotifySubscribedUsersCoronaUpdate();
        }

        private void MaintenanceEvent(object sender, ElapsedEventArgs e)
        {
            ResetTimerInterval((Timer)sender);

            logger.LogDebug($"{nameof(MaintenanceEvent)} method called");

            logger.LogInformation("Compacting db");
            db.DbCompact();

            NotifyAdministrators(GetBotUptime(), true);
        }

        private void JokeEvent(object sender, ElapsedEventArgs e)
        {
            ResetTimerInterval((Timer)sender);

            logger.LogDebug($"{nameof(JokeEvent)} method called");

            SendJokesToSubscribedUsers();
        }

        private void SendJokesToSubscribedUsers()
        {
            var users = db.Select_TelegramUsersIsSubscribedToCoronaUpdates();

            if (users.Count == 0)
            {
                logger.LogInformation($"{nameof(SendJokesToSubscribedUsers)} - No users to notify");
                return;
            }

            logger.LogInformation("Sending jokes to subscribed users");

            foreach (var user in users)
            {
                Task.Run(async () => await ExecuteJokeCommand(user.ChatId));
            }
        }

        private void NotifySubscribedUsersCoronaUpdate()
        {
            var users = db.Select_TelegramUsersIsSubscribedToCoronaUpdates();

            if (users.Count == 0)
            {
                logger.LogInformation($"{nameof(NotifySubscribedUsersCoronaUpdate)} - No users to notify");
                return;
            }

            logger.LogInformation("Sending notifications to subscribed users");

            // download latest data first
            var task = CoronaApi.DownloadCoronaCaseDistributionRecordsAsync(true);
            task.Wait();

            foreach (var user in users)
            {
                Task.Run(async () => await ExecuteCoronaCommand(user.ChatId));
            }
        }

        private void NotifyAdministrators(string notificationMessage, bool notifySilently = false)
        {
            var users = db.Select_TelegramUsersIsAdministrator();

            if (users.Count == 0)
            {
                logger.LogInformation($"{nameof(NotifyAdministrators)} - No users to notify");
                return;
            }

            logger.LogInformation("Sending app info to admin users");

            foreach (var user in users)
            {
                Task.Run(async () => await SendTextMessageNoReplyAsync(user.ChatId, notificationMessage, notifySilently));
            }
        }

        private void OnMessageEvent(object? sender, MessageEventArgs e)
        {
            logger.LogInformation($"Received message from user: '{e.Message.From.Username}'  id: {e.Message.Chat.Id}  type: {e.Message.Type}");

            Task.Run(async () => await ProcessCommand(e));
        }

        private void OnMessageEditedEvent(object? sender, MessageEventArgs e)
        {
            logger.LogDebug($"{nameof(OnMessageEditedEvent)} method called");
            OnMessageEvent(sender, e);
        }

        private void OnUpdateEvent(object? sender, UpdateEventArgs e)
        {
            logger.LogDebug($"{nameof(OnUpdateEvent)} method called");
        }

        private void OnReceiveErrorEvent(object? sender, ReceiveErrorEventArgs e)
        {
            logger.LogDebug($"{nameof(OnReceiveErrorEvent)} method called");
        }

        private async Task ProcessCommand(MessageEventArgs e)
        {
            try
            {
                var user = db.Select_TelegramUsers(e.Message.Chat.Id);

                // special case: access denied message
                if (user is null && AppSettings.PermissionDeniedForNewUsers)
                {
                    string message = $"Permission denied for user id {e.Message.Chat.Id}";
                    logger.LogInformation(message);
                    NotifyAdministrators(message);
                    await SendTextMessageNoReplyAsync(e.Message.Chat.Id, GetPermissionDeniedMessage());
                    return;
                }

                if (e.Message.Type == MessageType.Text)
                {
                    // extract only first argument from message text
                    string command = e.Message.Text.Split(' ')[0].ToLower();

                    // main init command
                    if (command.Equals("/start"))
                    {
                        await ExecuteStartCommand(e, user);
                    }
                    else if (user is null)
                    {
                        // special case - non existing users can only call /start command
                        await SendTextMessageNoReplyAsync(e.Message.Chat.Id, "New user(s) should call /start command first");
                    }
                    else if (command.Equals("/help"))
                    {
                        await SendTextMessageNoReplyAsync(e.Message.Chat.Id, GetBotInfo());
                    }
                    else if (command.Equals("/stop"))
                    {
                        await ExecuteStopCommand(e.Message.Chat.Id, user);
                    }
                    else if (command.Equals("/uptime"))
                    {
                        await SendTextMessageNoReplyAsync(e.Message.Chat.Id, GetBotUptime());
                    }
                    else if (command.Equals("/date"))
                    {
                        await SendTextMessageNoReplyAsync(e.Message.Chat.Id, DateTime.UtcNow.ToString("u"));
                    }
                    else if (command.Equals("/pic"))
                    {
                        await ExecutePictureCommand(e.Message.Chat.Id);
                    }
                    else if (command.Equals("/corona"))
                    {
                        await SendTextMessageNoReplyAsync(e.Message.Chat.Id, "Working on it...");
                        await ExecuteCoronaCommand(e.Message.Chat.Id, e.Message.Text);
                    }
                    else if (command.Equals("/fuelcost"))
                    {
                        await ExecuteFuelcostCommand(e.Message.Chat.Id, e.Message.Text);
                    }
                    else if (command.Equals("/joke"))
                    {
                        await ExecuteJokeCommand(e.Message.Chat.Id);
                    }
                    else
                    {
                        await SendTextMessageAsync(
                                e.Message.Chat.Id,
                                e.Message.MessageId,
                                "Unknown command detected.\nType in /help to display help info");
                    }
                }
                else if (e.Message.Type == MessageType.Location)
                {
                    if (user is null)
                    {
                        await SendTextMessageNoReplyAsync(e.Message.Chat.Id, "New user(s) should call /start command first");
                        return;
                    }

                    user.UserLocationLatitude = e.Message.Location.Latitude;
                    user.UserLocationLongitude = e.Message.Location.Longitude;
                    db.Update_TelegramUsers(user);

                    await SendTextMessageNoReplyAsync(
                        e.Message.Chat.Id,
                        $"Your current location has been updated!\nLatitude: <b>{e.Message.Location.Latitude}</b>  Longitude: <b>{e.Message.Location.Longitude}</b>");
                }
                else
                {
                    logger.LogInformation($"Unsupported message type: {e.Message.Type}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        private async Task ExecuteStartCommand(MessageEventArgs e, DB_TelegramUsers? user)
        {
            await SendTextMessageNoReplyAsync(
                                    e.Message.Chat.Id,
                                    $"Hi, {e.Message.From.FirstName} {e.Message.From.LastName} (user: '{e.Message.From.Username}').");

            // create new user
            if (user is null)
            {
                var newUser = new DB_TelegramUsers
                {
                    ChatId = e.Message.Chat.Id,
                    FirstName = e.Message.Chat.FirstName,
                    LastName = e.Message.Chat.LastName,
                    UserName = e.Message.Chat.Username,
                    UserIsSubscribedToCoronaUpdates = true,

                    // check if new user must have admin option set to true
                    UserIsAdmin = db.LastIndex_TelegramUsers() is null && AppSettings.FirstUserGetsAdminRights,
                };

                db.Insert_TelegramUsers(newUser);
                logger.LogInformation($"User {newUser.ChatId} added to the db");
                await SendTextMessageNoReplyAsync(e.Message.Chat.Id, "You have successfully subscribed!");
            }

            await SendTextMessageNoReplyAsync(e.Message.Chat.Id, GetBotInfo());
        }

        private async Task ExecuteStopCommand(long chatId, DB_TelegramUsers? user)
        {
            if (user is null)
            {
                return;
            }

            db.Delete_TelegramUsers(user);
            logger.LogInformation($"{user.ChatId} user removed from the db");
            await SendTextMessageNoReplyAsync(chatId, "You have successfully unsubscribed!");
        }

        private async Task ExecuteFuelcostCommand(long chatId, string args)
        {
            logger.LogDebug($"{nameof(ExecuteFuelcostCommand)} method called");

            string[] command = args.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            try
            {
                double tripDistance = (command.Length >= 2) ? double.Parse(command[1]) : 100.0D;
                double fuelEfficiency = (command.Length >= 3) ? double.Parse(command[2]) : 6.0D;
                decimal fuelPriceLiter = (command.Length >= 4) ? decimal.Parse(command[3]) : 1.249M;

                var fuelCalculator = new FuelcostCalculator(tripDistance, fuelEfficiency, fuelPriceLiter);
                await SendTextMessageNoReplyAsync(chatId, fuelCalculator.TripCostFormatted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await SendTextMessageNoReplyAsync(chatId, ex.Message);
            }
        }

        private async Task ExecuteJokeCommand(long chatId)
        {
            logger.LogDebug($"{nameof(ExecuteJokeCommand)} method called");

            try
            {
                var xmlObj = await RzhunemoguApi.DownloadRandomJoke();
                var sb = new StringBuilder();
                sb.AppendLine("<b>Рандомный анекдот от РжуНеМогу.ру</b>");
                sb.AppendLine(xmlObj?.Content);
                await SendTextMessageNoReplyAsync(chatId, sb.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await SendTextMessageNoReplyAsync(chatId, ex.Message);
            }
        }

        private async Task ExecuteCoronaCommand(long chatId, string? args = null)
        {
            logger.LogDebug($"{nameof(ExecuteCoronaCommand)} method called");

            bool overrideCachedData = false;

            if (args is not null)
            {
                string[] command = args.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                overrideCachedData = (command.Length >= 2) && command[1].Equals("-override");
            }

            try
            {
                var sw = Stopwatch.StartNew();
                await CoronaApi.DownloadCoronaCaseDistributionRecordsAsync(overrideCachedData);
                sw.Stop();

                // generate output message
                var sb = new StringBuilder();
                sb.AppendLine($"<b>14-day notification rate of new COVID-19 cases and deaths</b>");
                sb.AppendLine("Position | YearWeek | Country | Rate14Day | WeeklyCount | CumulativeCount");
                int position = 0;

                foreach (var record in CoronaApi.CashedRecords)
                {
                    position++;
                    string line = $"{position:00}. | {record.YearWeek} | {record.Country.Replace("_", " ")} | {Math.Round(record.Rate14Day, 2)} | {record.WeeklyCount} | {record.CumulativeCount}";

                    // highlight current record
                    if (AppSettings.CoronaOutputHighlightCountries.Contains(record.Country))
                    {
                        line = $"<b>{line}</b>";
                    }

                    sb.AppendLine(line);
                }

                sb.AppendLine();
                sb.AppendLine($"{CoronaApi.CashedRecords.Count} record(s) in total.");
                sb.AppendLine($"Data collected on {CoronaApi.RecordsCachedDateUtc:u} ({sw.ElapsedMilliseconds / 1000D:0.00} sec.)");

                await SendTextMessageNoReplyAsync(chatId, sb.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await SendTextMessageNoReplyAsync(chatId, ex.Message);
            }
        }

        private async Task ExecutePictureCommand(long chatId)
        {
            logger.LogDebug($"{nameof(ExecutePictureCommand)} method called");

            try
            {
                var filePath = await LoremPicsumApi.DownloadRandomImage();
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                await bot.SendDocumentAsync(chatId, new InputOnlineFile(fs, filePath));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await SendTextMessageNoReplyAsync(chatId, ex.Message);
            }
        }

        private async Task SendTextMessageAsync(long chatId, int messageId, string message)
        {
            var msg = await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    disableNotification: false,
                    replyToMessageId: messageId);

            logger.LogInformation($"{msg.From.FirstName} sent message {msg.MessageId} " +
                $"to chat {msg.Chat.Id} at {msg.Date.ToUniversalTime():u}. " +
                $"It is a reply to message {msg.ReplyToMessage.MessageId}.");
        }

        private async Task SendTextMessageNoReplyAsync(long chatId, string message, bool sendMessageSilently = false)
        {
            var msg = await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    disableNotification: sendMessageSilently);

            logger.LogInformation($"{msg.From.FirstName} sent message {msg.MessageId} " +
                $"to chat {msg.Chat.Id} at {msg.Date.ToUniversalTime():u}.");
        }

        private async Task SendFileAsync(long chatId, string filePath, bool sendAsPhoto = false)
        {
            string fileName = Path.GetFileName(filePath);
            using var sr = File.Open(filePath, FileMode.Open);
            var doc = new InputOnlineFile(sr, fileName);
            var task = sendAsPhoto ? await bot.SendPhotoAsync(chatId, doc, fileName)
                                   : await bot.SendDocumentAsync(chatId, doc);

            logger.LogInformation($"'{fileName}' file sent.");
        }

        private string GetBotUptime()
        {
            var uptime = DateTime.UtcNow - clientStartedDateUtc;
            var proc = Process.GetCurrentProcess();

            return $"TelegramBot <pre>v{GitVersionInformation.InformationalVersion}</pre>\n" +
                   $"<b>Working set:</b> {proc.WorkingSet64 / 1024 / 1024D:0.00} Mbytes\n" +
                   $"<b>Peak working set:</b> {proc.PeakWorkingSet64 / 1024 / 1024D:0.00} Mbytes\n" +
                   $"<b>Total CPU time:</b> {proc.TotalProcessorTime.TotalSeconds:0.00} sec\n" +
                   $"<b>Uptime:</b> {uptime.Days} day(s) {uptime.Hours:00}h:{uptime.Minutes:00}m:{uptime.Seconds:00}s";
        }
    }
}

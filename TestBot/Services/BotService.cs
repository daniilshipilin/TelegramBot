namespace TelegramBot.TestBot.Service
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Timers;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Telegram.Bot;
    using Telegram.Bot.Args;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types.InputFiles;
    using TelegramBot.TestBot.Helpers;
    using TelegramBot.TestBot.Models;

    public class BotService
    {
        private const int Min60Msec = 3600000;

        private static readonly HttpClient Client = new HttpClient() { Timeout = new TimeSpan(0, 0, 60) };
        private static readonly Random Rnd = new Random();

        private readonly DateTime botStartedDateUtc;
        private readonly ITelegramBotClient botClient;
        private readonly Timer gcTimer;
        private readonly Timer subscriptionTimer;
        private readonly Timer maintenanceTimer;
        private readonly object locker = new object();
        private readonly DatabaseAccess sqlite;
        private readonly ILogger<HostedService> logger;

        private bool commandIsExecuting;
        private bool overrideCachedData;

        public BotService(ILogger<HostedService> logger)
        {
            this.logger = logger;
            botStartedDateUtc = DateTime.UtcNow;

            botClient = new TelegramBotClient(AppSettings.TelegramBotToken)
            {
                Timeout = new TimeSpan(0, 0, 60),
            };

            sqlite = new DatabaseAccess(AppSettings.DatabaseConnectionString);

            gcTimer = new Timer(Min60Msec);
            gcTimer.Elapsed += GarbageCollectEvent;
            gcTimer.AutoReset = true;
            gcTimer.Enabled = true;

            subscriptionTimer = new Timer(CalculateTimerInterval(AppSettings.SubscriptionTimerTriggeredAt));
            subscriptionTimer.Elapsed += SubscribedUsersNotifyEvent;
            subscriptionTimer.AutoReset = false;
            subscriptionTimer.Enabled = true;

            maintenanceTimer = new Timer(CalculateTimerInterval(AppSettings.MaintenanceTimerTriggeredAt));
            maintenanceTimer.Elapsed += MaintenanceEvent;
            maintenanceTimer.AutoReset = false;
            maintenanceTimer.Enabled = true;
        }

        public void PrintBotInfo()
        {
            var botInfo = botClient.GetMeAsync().Result;
            logger.LogInformation($"TelegramBot v{GitVersionInformation.InformationalVersion}");
            logger.LogInformation($"Id: {botInfo.Id}\tName: '{botInfo.FirstName}'\tUsername: '{botInfo.Username}'");
            int users = sqlite.Count_TelegramUsers();
            logger.LogInformation($"{users} user(s) found in db");
        }

        public void StartReceiving()
        {
            logger.LogDebug($"{nameof(StartReceiving)} method called");

            SubscribeEvents();
            botClient.StartReceiving();
            NotifyAdministrators($"'{botClient.GetMeAsync().Result.FirstName}' started receiving at {DateTime.UtcNow:u}");
        }

        public void StopReceiving()
        {
            logger.LogDebug($"{nameof(StopReceiving)} method called");

            NotifyAdministrators($"'{botClient.GetMeAsync().Result.FirstName}' stopped receiving at {DateTime.UtcNow:u}");
            UnSubscribeEvents();
            botClient.StopReceiving();
        }

        private double CalculateTimerInterval(DateTime triggerAtTime)
        {
            var now = DateTime.UtcNow;

            if (now > triggerAtTime)
            {
                triggerAtTime = triggerAtTime.AddDays(1);
            }

            return (triggerAtTime - now).TotalMilliseconds;
        }

        private void SubscribeEvents()
        {
            botClient.OnMessage += OnMessageEvent;
            botClient.OnMessageEdited += OnMessageEditedEvent;
            botClient.OnUpdate += OnUpdateEvent;
            botClient.OnReceiveError += OnReceiveErrorEvent;
        }

        private void UnSubscribeEvents()
        {
            botClient.OnMessage -= OnMessageEvent;
            botClient.OnMessageEdited -= OnMessageEditedEvent;
            botClient.OnUpdate -= OnUpdateEvent;
            botClient.OnReceiveError -= OnReceiveErrorEvent;
        }

        private void GarbageCollectEvent(object sender, ElapsedEventArgs e)
        {
            logger.LogDebug($"{nameof(GarbageCollectEvent)} method called");

            GC.Collect();
        }

        private void SubscribedUsersNotifyEvent(object sender, ElapsedEventArgs e)
        {
            logger.LogDebug($"{nameof(SubscribedUsersNotifyEvent)} method called");

            NotifySubscribedUsers();

            subscriptionTimer.Interval = CalculateTimerInterval(AppSettings.SubscriptionTimerTriggeredAt);
            subscriptionTimer.Enabled = true;
        }

        private void MaintenanceEvent(object sender, ElapsedEventArgs e)
        {
            logger.LogDebug($"{nameof(MaintenanceEvent)} method called");

            logger.LogInformation("Compacting db");
            sqlite.DbCompact();

            NotifyAdministrators(GetBotUptime(), true);

            maintenanceTimer.Interval = CalculateTimerInterval(AppSettings.MaintenanceTimerTriggeredAt);
            maintenanceTimer.Enabled = true;
        }

        private void NotifySubscribedUsers()
        {
            var users = sqlite.Select_TelegramUsersIsSubscribed();

            if (users.Count == 0)
            {
                logger.LogInformation($"{nameof(NotifySubscribedUsers)} - No users to notify");
                return;
            }

            logger.LogInformation("Sending notifications to subscribed users");

            // make sure, that new data will be fetched during next ExecuteCoronaCommand call
            overrideCachedData = true;

            foreach (var user in users)
            {
                Task.Run(async () => await ExecuteCoronaCommand(user.ChatId));
            }
        }

        private void NotifyAdministrators(string notificationMessage, bool notifySilently = false)
        {
            var users = sqlite.Select_TelegramUsersIsAdministrator();

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

        private async void OnMessageEvent(object? sender, MessageEventArgs e)
        {
            logger.LogDebug($"{nameof(OnMessageEvent)} method called");
            logger.LogInformation($"Received a text message from user '{e.Message.From.Username}'  type: {e.Message.Type}  message: '{e.Message.Text}'");

            long chatId = e.Message.Chat.Id;

            try
            {
                // extract only first argument from message text
                switch (e.Message.Text.ToLower().Split(' ')[0])
                {
                    case "/start":
                        {
                            await SendTextMessageNoReplyAsync(chatId, $"Hi, {e.Message.From.FirstName} {e.Message.From.LastName} (user: '{e.Message.From.Username}').");
                            var user = sqlite.Select_TelegramUsers(chatId);

                            if (user is null)
                            {
                                var newUser = new DB_TelegramUsers(chatId, e.Message.Chat.FirstName, e.Message.Chat.LastName, e.Message.Chat.Username);

                                // check if new user must have admin option set to true
                                if (sqlite.LastIndex_TelegramUsers() is null &&
                                    AppSettings.FirstUserGetsAdminRights)
                                {
                                    newUser.UserIsAdmin = true;
                                }

                                sqlite.Insert_TelegramUsers(newUser);
                                logger.LogInformation($"User {newUser.ChatId} added to the db");
                                await SendTextMessageNoReplyAsync(chatId, "You have successfully subscribed");
                            }

                            await SendTextMessageNoReplyAsync(chatId, GetBotInfo());
                        }

                        break;

                    case "/stop":
                        {
                            var user = sqlite.Select_TelegramUsers(chatId);

                            if (user is object)
                            {
                                sqlite.Delete_TelegramUsers(user);
                                logger.LogInformation($"{user.ChatId} user removed from the db");
                                await SendTextMessageNoReplyAsync(chatId, "You have successfully unsubscribed");
                            }
                        }

                        break;

                    case "/help":
                        await SendTextMessageNoReplyAsync(chatId, GetBotInfo());
                        break;

                    case "/uptime":
                        await SendTextMessageNoReplyAsync(chatId, GetBotUptime());
                        break;

                    case "/date":
                        await SendTextMessageNoReplyAsync(chatId, DateTime.UtcNow.ToString("u"));
                        break;

                    case "/pic":
                        await ExecutePictureCommand(chatId);
                        break;

                    case "/corona":
                        await SendTextMessageNoReplyAsync(chatId, "Working on it...");
                        await ExecuteCoronaCommand(chatId);
                        break;

                    case "/fuelcost":
                        await ExecuteFuelcostCommand(chatId, e.Message.Text);
                        break;

                    case "/joke":
                        await SendTextMessageNoReplyAsync(chatId, "<b>Рандомный анекдот от РжуНеМогу.ру</b>");
                        await ExecuteJokeCommand(chatId);
                        break;

                    default:
                        await SendTextMessageAsync(
                            chatId,
                            e.Message.MessageId,
                            "Unknown command detected.\nType in /help to display help info");
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
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

        private async Task ExecuteFuelcostCommand(long chatId, string args)
        {
            logger.LogDebug($"{nameof(ExecuteFuelcostCommand)} method called");

            string[] command = args.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            try
            {
                double tripDistance = (command.Length >= 2) ? double.Parse(command[1]) : 100;
                double fuelEfficiency = (command.Length >= 3) ? double.Parse(command[2]) : 6.0;
                decimal fuelPriceLiter = (command.Length >= 4) ? decimal.Parse(command[3]) : 1.249M;

                var fuelCalculator = new FuelcostCalculator(tripDistance, fuelEfficiency, fuelPriceLiter);
                await SendTextMessageNoReplyAsync(chatId, fuelCalculator.TripCostFormatted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        private async Task ExecuteJokeCommand(long chatId)
        {
            logger.LogDebug($"{nameof(ExecuteJokeCommand)} method called");

            try
            {
                RzhunemoguXml xmlObj;
                int argument = AppSettings.RzhunemoguApiArgumentsList[Rnd.Next(AppSettings.RzhunemoguApiArgumentsList.Count)];
                string requestUri = AppSettings.RzhunemoguApiBaseUrl + argument;
                using var response = await Client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    // register extended encodings
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    var encoding = Encoding.GetEncoding("windows-1251");

                    using var sr = new StreamReader(await response.Content.ReadAsStreamAsync(), encoding);
                    string xml = sr.ReadToEnd();

                    // deserialize received xml
                    xmlObj = Utils.XmlDeserializeFromString<RzhunemoguXml>(xml);
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }

                await SendTextMessageNoReplyAsync(chatId, xmlObj.Content);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

        private async Task ExecuteCoronaCommand(long chatId)
        {
            logger.LogDebug($"{nameof(ExecuteCoronaCommand)} method called");

            lock (locker)
            {
                // wait till previous method call is executed
                while (commandIsExecuting)
                {
                    Task.Delay(100).Wait();
                }

                commandIsExecuting = true;
            }

            try
            {
                var sw = new Stopwatch();
                DB_CoronaCaseDistributionRecords? dbRecord = null;
                string timestamp = sqlite.Select_CoronaCaseDistributionRecordsLastTimestamp();
                var lastRecordDateUtc = (timestamp is object) ? DateTime.ParseExact(timestamp, "u", CultureInfo.InvariantCulture) : new DateTime(1, 1, 1);

                // download data, if last download operation was done more than hour ago
                if ((DateTime.UtcNow - lastRecordDateUtc).Hours >= 1 || overrideCachedData)
                {
                    sw.Start();
                    var jsonObj = new CaseDistributionJson();
                    using var response = await Client.GetAsync(AppSettings.CoronaApiBaseUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        // read response in json format
                        string json = await response.Content.ReadAsStringAsync();

                        // create string list for possible errors during json processing
                        var errors = new List<string>();
                        var settings = new JsonSerializerSettings()
                        {
                            Error = (sender, args) =>
                            {
                                // put registered errors in created string list
                                errors.Add(args.ErrorContext.Error.Message);
                                args.ErrorContext.Handled = true;
                            },
                        };

                        // deserialize received json
                        jsonObj = JsonConvert.DeserializeObject<CaseDistributionJson>(json, settings);

                        if (errors.Count > 0)
                        {
                            throw new Exception($"JSON deserialization failed{Environment.NewLine}" +
                                                $"{string.Join(Environment.NewLine, errors)}");
                        }
                    }
                    else
                    {
                        throw new Exception(response.ReasonPhrase);
                    }

                    // select the records from specific region
                    var records = jsonObj.Records.FindAll(i => i.ContinentExp.Equals("Europe", StringComparison.InvariantCultureIgnoreCase));
                    var caseDistributionRecords = new List<CaseDistributionJson.Record>();

                    foreach (var record in records)
                    {
                        // skip already added country record
                        if (!caseDistributionRecords.Exists(x => x.CountriesAndTerritories.Equals(record.CountriesAndTerritories, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            caseDistributionRecords.Add(record);
                        }
                    }

                    // sort list records
                    caseDistributionRecords = caseDistributionRecords.OrderByDescending(i => i.CumulativeNumber).ToList();

                    sw.Stop();

                    // generate output message
                    var sb = new StringBuilder();
                    sb.AppendLine($"<b>COVID-19 situation update</b>");
                    sb.AppendLine("Timestamp\tCountry\tCumulativeNumber");
                    sb.Append("<pre>");

                    caseDistributionRecords.ForEach(i => sb.AppendLine($"{i.TimeStamp:yyyy-MM-dd}\t{i.CountriesAndTerritories,-12}\t{i.CumulativeNumber:0.00}"));

                    sb.AppendLine("</pre>");
                    sb.AppendLine($"{caseDistributionRecords.Count} record(s) in total.");

                    dbRecord = new DB_CoronaCaseDistributionRecords(sb.ToString());
                    sqlite.Insert_CoronaCaseDistributionRecords(dbRecord);
                }
                else
                {
                    dbRecord = sqlite.Select_CoronaCaseDistributionRecords();
                }

                await SendTextMessageNoReplyAsync(chatId, dbRecord.CaseDistributionRecords + $"Data collected on {dbRecord.DateCollectedUtc} ({sw.ElapsedMilliseconds / 1000D:0.00} sec.)");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
            finally
            {
                overrideCachedData = false;
                commandIsExecuting = false;
            }
        }

        private async Task ExecutePictureCommand(long chatId)
        {
            logger.LogDebug($"{nameof(ExecutePictureCommand)} method called");

            string picsDir = Path.GetFullPath(AppSettings.PicsDirectory);
            var pics = GetFiles(picsDir, @"\.jpg|\.jpeg|\.png|\.bmp", SearchOption.AllDirectories).ToList();

            if (pics.Count == 0)
            {
                throw new Exception($"'{picsDir}' directory has no images");
            }

            int index = Rnd.Next(pics.Count);
            await SendFileAsync(chatId, pics[index]);
        }

        private IEnumerable<string> GetFiles(string path, string searchPatternExpression, SearchOption searchOption)
        {
            var reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);

            return Directory.EnumerateFiles(path, "*", searchOption).Where(file => reSearchPattern.IsMatch(Path.GetExtension(file)));
        }

        private async Task SendTextMessageAsync(long chatId, int messageId, string message)
        {
            var msg = await botClient.SendTextMessageAsync(
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
            var msg = await botClient.SendTextMessageAsync(
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
            var task = sendAsPhoto ? await botClient.SendPhotoAsync(chatId, doc, fileName)
                                   : await botClient.SendDocumentAsync(chatId, doc);
            logger.LogInformation($"'{fileName}' file sent.");
        }

        private double GetTotalAllocatedMemoryInMBytes()
        {
            using var p = Process.GetCurrentProcess();

            return p.PrivateMemorySize64 / 1048576D;
        }

        private string GetBotUptime()
        {
            var uptime = DateTime.UtcNow - botStartedDateUtc;
            var proc = Process.GetCurrentProcess();

            return $"TelegramBot <pre>v{GitVersionInformation.InformationalVersion}</pre>\n" +
                   $"<b>Working set:</b> {proc.WorkingSet64 / 1024 / 1024D:0.00} Mbytes\n" +
                   $"<b>Peak working set:</b> {proc.PeakWorkingSet64 / 1024 / 1024D:0.00} Mbytes\n" +
                   $"<b>Total CPU time:</b> {proc.TotalProcessorTime.TotalSeconds:0.00} sec\n" +
                   $"<b>Uptime:</b> {uptime.Days} day(s) {uptime.Hours:00}h:{uptime.Minutes:00}m:{uptime.Seconds:00}s";
        }

        private string GetBotInfo()
        {
            return $"TelegramBot v{GitVersionInformation.SemVer} made by @daniilshipilin.\n" +
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
        }
    }
}

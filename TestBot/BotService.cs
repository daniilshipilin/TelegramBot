using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using TelegramBot.Models;

namespace TelegramBot
{
    public class BotService
    {
        const int MIN_60_MSEC = 3600000;

        readonly DateTime _botStartedDateUtc;
        readonly Random _rnd;
        readonly ITelegramBotClient _botClient;
        readonly HttpClient _httpClient;
        readonly Timer _gcTimer;
        readonly Timer _subscriptionTimer;
        readonly Timer _maintenanceTimer;
        readonly object _locker = new object();
        bool _commandIsExecuting;
        readonly SQLiteDBAccess _sqlite;

        readonly IConfiguration _configuration;
        readonly ILogger<HostedService> _logger;

        public BotService(IConfiguration configuration, ILogger<HostedService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _botStartedDateUtc = DateTime.UtcNow;
            _rnd = new Random();

            _botClient = new TelegramBotClient(_configuration.GetValue<string>("ApplicationSettings:TelegramBotToken"))
            {
                Timeout = new TimeSpan(0, 0, 60)
            };

            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_configuration.GetValue<string>("ApplicationSettings:CoronaApiBaseUrl")),
                Timeout = new TimeSpan(0, 0, 60)
            };

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _sqlite = new SQLiteDBAccess(_configuration.GetSection("DatabaseSettings:ConnectionStrings")["Default"]);

            _gcTimer = new Timer(MIN_60_MSEC);
            _gcTimer.Elapsed += GarbageCollectEvent;
            _gcTimer.AutoReset = true;
            _gcTimer.Enabled = true;

            _subscriptionTimer = new Timer(CalculateTimerInterval(_configuration.GetValue<string>("ApplicationSettings:SubscriptionTimerTriggeredAt")));
            _subscriptionTimer.Elapsed += SubscribedUsersNotifyEvent;
            _subscriptionTimer.AutoReset = false;
            _subscriptionTimer.Enabled = true;

            _maintenanceTimer = new Timer(CalculateTimerInterval(_configuration.GetValue<string>("ApplicationSettings:MaintenanceTimerTriggeredAt")));
            _maintenanceTimer.Elapsed += MaintenanceEvent;
            _maintenanceTimer.AutoReset = false;
            _maintenanceTimer.Enabled = true;
        }

        private double CalculateTimerInterval(string triggerAtTime)
        {
            var parsed = DateTime.ParseExact(triggerAtTime, "HH:mm:ss", CultureInfo.InvariantCulture);
            var now = DateTime.UtcNow;

            if (now > parsed)
            {
                parsed = parsed.AddDays(1);
            }

            return (parsed - now).TotalMilliseconds;
        }

        private void SubscribeEvents()
        {
            _botClient.OnMessage += OnMessageEvent;
            _botClient.OnMessageEdited += OnMessageEditedEvent;
            _botClient.OnUpdate += OnUpdateEvent;
            _botClient.OnReceiveError += OnReceiveErrorEvent;
        }

        private void UnSubscribeEvents()
        {
            _botClient.OnMessage -= OnMessageEvent;
            _botClient.OnMessageEdited -= OnMessageEditedEvent;
            _botClient.OnUpdate -= OnUpdateEvent;
            _botClient.OnReceiveError -= OnReceiveErrorEvent;
        }

        private void GarbageCollectEvent(object sender, ElapsedEventArgs e)
        {
            _logger.LogDebug($"{nameof(GarbageCollectEvent)} method called");

            GC.Collect();
        }

        private void SubscribedUsersNotifyEvent(object sender, ElapsedEventArgs e)
        {
            _logger.LogDebug($"{nameof(SubscribedUsersNotifyEvent)} method called");

            NotifySubscribedUsers();

            _subscriptionTimer.Interval = CalculateTimerInterval(_configuration.GetValue<string>("ApplicationSettings:SubscriptionTimerTriggeredAt"));
            _subscriptionTimer.Enabled = true;
        }

        private void MaintenanceEvent(object sender, ElapsedEventArgs e)
        {
            _logger.LogDebug($"{nameof(MaintenanceEvent)} method called");

            _logger.LogInformation("Compacting db");
            _sqlite.DbCompact();

            NotifyAdministrators();

            _maintenanceTimer.Interval = CalculateTimerInterval(_configuration.GetValue<string>("ApplicationSettings:MaintenanceTimerTriggeredAt"));
            _maintenanceTimer.Enabled = true;
        }

        private void NotifySubscribedUsers()
        {
            var users = _sqlite.Select_TelegramUsersIsSubscribed();

            if (users.Count == 0)
            {
                _logger.LogInformation($"{nameof(NotifySubscribedUsers)} - No users to notify");
                return;
            }

            _logger.LogInformation("Sending notifications to subscribed users");

            foreach (var user in users)
            {
                Task.Run(async () => await ExecuteCoronaCommand(user.ChatId)).ConfigureAwait(false);
            }
        }

        private void NotifyAdministrators()
        {
            var users = _sqlite.Select_TelegramUsersIsAdministrator();

            if (users.Count == 0)
            {
                _logger.LogInformation($"{nameof(NotifyAdministrators)} - No users to notify");
                return;
            }

            _logger.LogInformation("Sending app info to admin users");

            foreach (var user in users)
            {
                Task.Run(async () => await SendTextMessageNoReplyAsync(user.ChatId, GetBotUptime(), true)).ConfigureAwait(false);
            }
        }

        public void PrintBotInfo()
        {
            var botInfo = _botClient.GetMeAsync().Result;
            _logger.LogInformation($"TelegramBot v{GitVersionInformation.InformationalVersion}");
            _logger.LogInformation($"Id: {botInfo.Id}\tName: '{botInfo.FirstName}'\tUsername: '{botInfo.Username}'");
            int users = _sqlite.Count_TelegramUsers();
            _logger.LogInformation($"{users} user(s) found in db");
        }

        public void StartReceiving()
        {
            _logger.LogDebug($"{nameof(StartReceiving)} method called");
            SubscribeEvents();
            _botClient.StartReceiving();
        }

        public void StopReceiving()
        {
            _logger.LogDebug($"{nameof(StopReceiving)} method called");
            UnSubscribeEvents();
            _botClient.StopReceiving();
        }

        private async void OnMessageEvent(object? sender, MessageEventArgs e)
        {
            _logger.LogDebug($"{nameof(OnMessageEvent)} method called");
            _logger.LogInformation($"Received a text message from user '{e.Message.From.Username}'  type: {e.Message.Type}  message: '{e.Message.Text}'");

            long chatId = e.Message.Chat.Id;

            try
            {
                // extract only first argument from message text
                switch (e.Message.Text.ToLower().Split(' ')[0])
                {
                    case "/start":
                        {
                            await SendTextMessageNoReplyAsync(chatId, $"Hi, {e.Message.From.FirstName} {e.Message.From.LastName} (user: '{e.Message.From.Username}').");
                            var user = _sqlite.Select_TelegramUsers(chatId);

                            if (user is null)
                            {
                                var newUser = new DB_TelegramUsers(chatId, e.Message.Chat.FirstName, e.Message.Chat.LastName, e.Message.Chat.Username);
                                _sqlite.Insert_TelegramUsers(newUser);
                                _logger.LogInformation($"User {newUser.ChatId} added to the db");
                            }
                            else
                            {
                                await SendTextMessageNoReplyAsync(chatId, GetBotInfo());
                            }
                        }
                        break;

                    case "/stop":
                        {
                            var existingUser = _sqlite.Select_TelegramUsers(chatId);

                            if (existingUser is object)
                            {
                                existingUser.UserIsSubscribed = false.ToString();
                                _sqlite.Update_TelegramUsers(existingUser);
                                _logger.LogInformation($"{existingUser.ChatId} user removed from the db");
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

                    default:
                        await SendTextMessageAsync(chatId, e.Message.MessageId, "Unknown command detected.\n" +
                                                      "Type in /help to display help info");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void OnMessageEditedEvent(object? sender, MessageEventArgs e)
        {
            _logger.LogDebug($"{nameof(OnMessageEditedEvent)} method called");
            OnMessageEvent(sender, e);
        }

        private void OnUpdateEvent(object? sender, UpdateEventArgs e)
        {
            _logger.LogDebug($"{nameof(OnUpdateEvent)} method called");
        }

        private void OnReceiveErrorEvent(object? sender, ReceiveErrorEventArgs e)
        {
            _logger.LogDebug($"{nameof(OnReceiveErrorEvent)} method called");
        }

        private async Task ExecuteFuelcostCommand(long chatId, string args)
        {
            _logger.LogDebug($"{nameof(ExecuteFuelcostCommand)} method called");

            string[] command = args.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double tripDistance;
            double fuelEfficiency;
            decimal fuelPriceLiter;

            // parameters parsing
            try
            {
                tripDistance = (command.Length >= 2) ? double.Parse(command[1]) : 100;
                fuelEfficiency = (command.Length >= 3) ? double.Parse(command[2]) : 6.0;
                fuelPriceLiter = (command.Length >= 4) ? decimal.Parse(command[3]) : 1.249M;
            }
            catch (Exception)
            {
                await SendTextMessageNoReplyAsync(chatId, "Incorrect arguments provided");
                return;
            }

            // calculation part
            double tripFuelUsed = (tripDistance / 100) * fuelEfficiency;
            decimal tripCost = (decimal)tripFuelUsed * fuelPriceLiter;

            await SendTextMessageNoReplyAsync(chatId,
                $"<b>Distance:</b> {tripDistance:0.00} km\n" +
                $"<b>Avg. fuel consumption:</b> {fuelEfficiency:0.00} l/100km\n" +
                $"<b>Fuel cost:</b> {fuelPriceLiter:0.00} EUR/l\n" +
                $"This trip will require <b>{tripFuelUsed:0.00}</b> liter(s) of fuel, " +
                $"which amounts to a fuel cost of <b>{tripCost:0.00}</b> EUR");
        }

        private async Task ExecuteCoronaCommand(long chatId)
        {
            _logger.LogDebug($"{nameof(ExecuteCoronaCommand)} method called");

            lock (_locker)
            {
                // wait till previous method call is executed
                while (_commandIsExecuting)
                {
                    Task.Delay(100).Wait();
                }

                _commandIsExecuting = true;
            }

            try
            {
                var sw = new Stopwatch();
                DB_CoronaCaseDistributionRecords? dbRecord = null;
                string timestamp = _sqlite.Select_CoronaCaseDistributionRecordsLastTimestamp();
                var lastRecordDateUtc = (timestamp is object) ? DateTime.ParseExact(timestamp, "u", CultureInfo.InvariantCulture) : new DateTime();

                // download date only if last download operation was done more than hour ago
                if ((DateTime.UtcNow - lastRecordDateUtc).Hours >= 1)
                {
                    sw.Start();
                    var jsonObj = new CaseDistributionJson();
                    using var response = await _httpClient.GetAsync("json");

                    if (response.IsSuccessStatusCode)
                    {
                        // read response in json format
                        string json = await response.Content.ReadAsStringAsync();

                        // create string list for possible errors during json processing
                        var errors = new List<string>();
                        var settings = new JsonSerializerSettings()
                        {
                            Error = delegate (object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                            {
                                // put registered errors in created string list
                                errors.Add(args.ErrorContext.Error.Message);
                                args.ErrorContext.Handled = true;
                            }
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
                    _sqlite.Insert_CoronaCaseDistributionRecords(dbRecord);
                }
                else
                {
                    dbRecord = _sqlite.Select_CoronaCaseDistributionRecords();
                }

                await SendTextMessageNoReplyAsync(chatId, dbRecord.CaseDistributionRecords + $"Data collected on {dbRecord.DateCollectedUtc} ({(sw.ElapsedMilliseconds / (double)1000):0.00} sec.)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                _commandIsExecuting = false;
            }
        }

        private async Task ExecutePictureCommand(long chatId)
        {
            _logger.LogDebug($"{nameof(ExecutePictureCommand)} method called");

            string picsDir = Path.GetFullPath(_configuration.GetValue<string>("ApplicationSettings:PicsDirectory"));
            var pics = GetFiles(picsDir, @"\.jpg|\.jpeg|\.png|\.bmp", SearchOption.AllDirectories).ToList();

            if (pics.Count == 0)
            {
                throw new Exception($"'{picsDir}' directory has no images");
            }

            int index = _rnd.Next(pics.Count);
            await SendFileAsync(chatId, pics[index]);
        }

        private IEnumerable<string> GetFiles(string path, string searchPatternExpression, SearchOption searchOption)
        {
            var reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);

            return Directory.EnumerateFiles(path, "*", searchOption).Where(file => reSearchPattern.IsMatch(Path.GetExtension(file)));
        }

        private async Task SendTextMessageAsync(long chatId, int messageId, string message)
        {
            var msg = await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    disableNotification: false,
                    replyToMessageId: messageId);

            _logger.LogInformation($"{msg.From.FirstName} sent message {msg.MessageId} " +
                $"to chat {msg.Chat.Id} at {msg.Date.ToUniversalTime():u}. " +
                $"It is a reply to message {msg.ReplyToMessage.MessageId}.");
        }

        private async Task SendTextMessageNoReplyAsync(long chatId, string message, bool sendMessageSilently = false)
        {
            var msg = await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    disableNotification: sendMessageSilently);

            _logger.LogInformation($"{msg.From.FirstName} sent message {msg.MessageId} " +
                $"to chat {msg.Chat.Id} at {msg.Date.ToUniversalTime():u}.");
        }

        private async Task SendFileAsync(long chatId, string filePath, bool sendAsPhoto = false)
        {
            string fileName = Path.GetFileName(filePath);
            using var sr = File.Open(filePath, FileMode.Open);
            var doc = new InputOnlineFile(sr, fileName);
            var task = (sendAsPhoto) ? await _botClient.SendPhotoAsync(chatId, doc, fileName)
                                     : await _botClient.SendDocumentAsync(chatId, doc);
            _logger.LogInformation($"'{fileName}' file sent.");
        }

        private double GetTotalAllocatedMemoryInMBytes()
        {
            using var p = Process.GetCurrentProcess();

            return p.PrivateMemorySize64 / (double)1048576;
        }

        private string GetBotUptime()
        {
            var uptime = DateTime.UtcNow - _botStartedDateUtc;
            var proc = Process.GetCurrentProcess();

            return $"TelegramBot v{GitVersionInformation.InformationalVersion}\n" +
                   $"Working set: {proc.WorkingSet64 / 1024 / (double)1024:0.00} Mbytes\n" +
                   $"Peak working set: {proc.PeakWorkingSet64 / 1024 / (double)1024:0.00} Mbytes\n" +
                   $"Total CPU time: {proc.TotalProcessorTime.TotalSeconds:0.00} sec\n" +
                   $"Uptime: {uptime.Days} day(s) {uptime.Hours:00}h:{uptime.Minutes:00}m:{uptime.Seconds:00}s";
        }

        private string GetBotInfo()
        {
            return $"TelegramBot v{GitVersionInformation.SemVer} made by @daniilshipilin.\n" +
                    "This bot supports following commands:\n" +
                    "  /start - subscribe to receive messages from the bot;\n" +
                    "  /stop - stop receiving messages from the bot;\n" +
                    "  /help - display help info;\n" +
                    "  /uptime - display service uptime info;\n" +
                    "  /date - show current date in UTC format;\n" +
                    "  /pic - receive random picture;\n" +
                    "  /corona - get current corona situation update;\n" +
                    "  /fuelcost - fuel consumption calculator.";
        }
    }
}

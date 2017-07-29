using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Emzi0767.Devi.Services;
using Emzi0767.Devi.Services.Data;
using Newtonsoft.Json;

// DEvI: Dynamic Evaluation Implement

namespace Emzi0767.Devi
{
    internal static class Program
    {
        #region Discord Client
        private static DiscordClient DeviClient { get; set; }
        private static CommandsNextModule DeviCommands { get; set; }
        #endregion

        #region Settings and configuration
        private static DeviSettingStore Settings { get; set; }
        private static DeviEmojiMap EmojiMap { get; set; }
        private static DeviDongerMap Dongers { get; set; }
        private static DeviGuildEmojiMap GuildEmoji { get; set; }
        private static DeviDatabaseClient DatabaseClient { get; set; }
        private static DeviUtilities Utilities { get; set; }
        private static HttpClient Http { get; set; }
        #endregion

        #region Tracking and temporary storage
        private static List<DiscordMessage> DeviMessageTracker { get; set; }
        #endregion

        internal static void Main(string[] args)
        {
            Order66().GetAwaiter().GetResult();
        }

        private static async Task Order66()
        {
            var l = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            GuildEmoji = new DeviGuildEmojiMap(new Dictionary<string, string>());

            var stx = Path.Combine(l, "devi.json");
            var emx = Path.Combine(l, "emoji.json");
            var dgx = Path.Combine(l, "donger.json");

            if (File.Exists(stx) && File.Exists(emx) && File.Exists(dgx))
            {
                stx = File.ReadAllText(stx, new UTF8Encoding(false));
                Settings = JsonConvert.DeserializeObject<DeviSettingStore>(stx);
            }
            else
            {
                throw new FileNotFoundException("Unable to load configuration file (devi.json)!");
            }

            DatabaseClient = new DeviDatabaseClient(Settings.DatabaseSettings);
            await DatabaseClient.PreconfigureAsync();

            if (File.Exists(emx))
            {
                emx = File.ReadAllText(emx, new UTF8Encoding(false));
                var edict = JsonConvert.DeserializeObject<Dictionary<string, string>>(emx);
                EmojiMap = new DeviEmojiMap(edict);
            }
            else
            {
                EmojiMap = new DeviEmojiMap(new Dictionary<string, string>());
            }

            if (File.Exists(dgx))
            {
                dgx = File.ReadAllText(dgx, new UTF8Encoding(false));
                Dongers = JsonConvert.DeserializeObject<DeviDongerMap>(dgx);
                Dongers.HookAliases();
            }
            else
            {
                Dongers = new DeviDongerMap()
                {
                    Dongers = new Dictionary<string, string>(),
                    Aliases = new Dictionary<string, List<string>>()
                };
            }

            Utilities = new DeviUtilities();
            Http = new HttpClient();

            var discord = new DiscordClient(new DiscordConfig() 
            { 
                LogLevel = LogLevel.Debug, 
                Token = Settings.Token, 
                TokenType = TokenType.User, 
                MessageCacheSize = Settings.CacheSize, 
                AutomaticGuildSync = false 
            });
            DeviClient = discord;

            var depb = new DependencyCollectionBuilder();
            var deps = depb.AddInstance(Settings)
                .AddInstance(EmojiMap)
                .AddInstance(Dongers)
                .AddInstance(GuildEmoji)
                .AddInstance(DatabaseClient)
                .AddInstance(Utilities)
                .AddInstance(Http)
                .AddInstance(Settings.CryptoSettings)
                .Add<CryptonatorApiClient>()
                .Add<NanopoolApiClient>()
                .Build();

            CommandsNextUtilities.RegisterConverter<ICurrency>(new CryptoCurrencyCodeConverter());

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration 
            { 
                StringPrefix = Settings.Prefix, 
                SelfBot = true, 
                EnableDefaultHelp = false, 
                EnableMentionPrefix = false,
                Dependencies = deps 
            });
            DeviCommands = commands;
            DeviCommands.CommandErrored += DeviCommands_CommandErrored;
            DeviCommands.RegisterCommands<DeviCommandModule>();
            DeviCommands.RegisterCommands<DeviLogManagementModule>();
            DeviCommands.RegisterCommands<DeviCryptomarketCommands>();

            DeviMessageTracker = new List<DiscordMessage>();

            discord.GuildAvailable += Discord_GuildAvailable;
            discord.MessageCreated += Discord_MessageReceived;
            discord.MessageUpdate += Discord_MessageUpdate;
            discord.MessageDelete += Discord_MessageDelete;
            discord.MessageBulkDelete += Discord_MessageBulkDelete;
            discord.Ready += Discord_Ready;
            discord.DebugLogger.LogMessageReceived += Discord_Log;
            discord.MessageReactionAdd += Discord_ReactionAdded;
            discord.MessageReactionRemove += Discord_ReactionRemoved;
            discord.ClientError += Discord_ClientError;
            
            await discord.ConnectAsync();

            await Task.Delay(-1);
        }

        private static Task DeviCommands_CommandErrored(CommandErrorEventArgs ea)
        {
            ea.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "DEvI CMD", string.Concat("'", ea.Command.QualifiedName, "' threw ", ea.Exception.GetType().ToString(), ": ", ea.Exception.Message), DateTime.Now);

            return Task.Delay(0);
        }

        private static Task Discord_ClientError(ClientErrorEventArgs ea)
        {
            ea.Client.DebugLogger.LogMessage(LogLevel.Error, "DEvI DSP", string.Concat(ea.Exception.GetType(), ": ", ea.Exception.Message), DateTime.Now);

            return Task.Delay(0);
        }

        private static Task Discord_GuildAvailable(GuildCreateEventArgs ea)
        {
            ea.Client.DebugLogger.LogMessage(LogLevel.Info, "DEvI", string.Concat("Guild available: ", ea.Guild.Name), DateTime.Now);

            var arg = ea.Guild;
            var emoji = arg.Emojis;
            if (emoji == null) return Task.CompletedTask;
            foreach (var e in emoji)
            {
                var es = string.Concat("<:X:", e.Id, ">");
                GuildEmoji.Mapping[e.Name.ToLower()] = es;
            }
            return Task.CompletedTask;
        }

        private static async Task Discord_ReactionAdded(MessageReactionAddEventArgs ea)
        {
            var arg1 = ea.Message.Id;

            var chn = ea.Channel;
            if (chn == null)
                return;
            
            if (ea.Channel.Guild == null || (ea.Channel.Guild != null && !DatabaseClient.Ignored.Contains(ea.Channel.Guild.Id)))
                await DatabaseClient.LogReactionAsync(ea.Emoji, ea.User, ea.Message, ea.Channel, true);

            var msg = DeviMessageTracker.FirstOrDefault(xmsg => xmsg.Id == arg1);
            if (msg == null)
                return;

            if (msg.Author.Id != DeviClient.CurrentUser.Id)
                return;

            if (ea.User.Id != msg.Author.Id)
                return;

            if (ea.Emoji.Name == EmojiMap.Mapping["x"])
                await msg.DeleteAsync();
        }

        private static Task Discord_ReactionRemoved(MessageReactionRemoveEventArgs ea)
        {
            if (ea.Channel.Guild == null || (ea.Channel.Guild != null && !DatabaseClient.Ignored.Contains(ea.Channel.Guild.Id)))
                return DatabaseClient.LogReactionAsync(ea.Emoji, ea.User, ea.Message, ea.Channel, false);
            return Task.CompletedTask;
        }

        private static void Discord_Log(object sender, DebugLogMessageEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[{0:yyyy-MM-dd HH:mm:ss zzz}] ", e.Timestamp.ToLocalTime());

            var tag = e.Application;
            if (tag.Length > 12)
                tag = tag.Substring(0, 12);
            if (tag.Length < 12)
                tag = tag.PadLeft(12, ' ');
            Console.Write("[{0}] ", tag);

            switch (e.Level)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
            }
            Console.Write("[{0}] ", e.Level.ToString().PadLeft(8));

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(e.Message);
        }

        private static Task Discord_Ready(ReadyEventArgs ea)
        {
            var timer1 = new Timer(new TimerCallback(DeviTimerCallback), null, 5000, 300000);
            var timer2 = new Timer(new TimerCallback(DeviSettingsCallback), null, 5000, 1800000);
            return Task.CompletedTask;
        }

        private static async Task Discord_MessageReceived(MessageCreateEventArgs ea)
        {
            var msg = ea.Message;
            if (msg == null)
                return;

            var chn = msg.Channel;
            if (chn == null)
                return;
            
            if (ea.Channel.Guild == null || (ea.Channel.Guild != null && !DatabaseClient.Ignored.Contains(ea.Channel.Guild.Id)))
                await DatabaseClient.LogMessageCreateAsync(msg);
            
            var gld = chn.Guild;
            if (gld == null)
                return;
            
            if (msg.Author.Id != DeviClient.CurrentUser.Id)
                return;

            if (msg.Author.Id == DeviClient.CurrentUser.Id)
                DeviMessageTracker.Add(msg);

            DeviMessageTracker.Add(msg);
        }

        private static Task Discord_MessageDelete(MessageDeleteEventArgs ea)
        {
            var msg = ea.Message;
            if (msg == null)
                return Task.CompletedTask;

            var chn = msg.Channel;
            if (chn == null)
                return Task.CompletedTask;
            
            if (ea.Channel.Guild == null || (ea.Channel.Guild != null && !DatabaseClient.Ignored.Contains(ea.Channel.Guild.Id)))
                return DatabaseClient.LogMessageDeleteAsync(msg);
            return Task.CompletedTask;
        }

        private static async Task Discord_MessageBulkDelete(MessageBulkDeleteEventArgs ea)
        {
            foreach (var msg in ea.Messages)
            {
                if (msg == null)
                    continue;

                var chn = msg.Channel;
                if (chn == null)
                    continue;
                
                if (ea.Channel.Guild == null || (ea.Channel.Guild != null && !DatabaseClient.Ignored.Contains(ea.Channel.Guild.Id)))
                    await DatabaseClient.LogMessageDeleteAsync(msg);
            }
        }

        private static Task Discord_MessageUpdate(MessageUpdateEventArgs ea)
        {
            var msg = ea.Message;
            if (msg == null)
                return Task.CompletedTask;

            var chn = msg.Channel;
            if (chn == null)
                return Task.CompletedTask;
            
            if (ea.Channel.Guild == null || (ea.Channel.Guild != null && !DatabaseClient.Ignored.Contains(ea.Channel.Guild.Id)))
                return DatabaseClient.LogMessageEditAsync(msg);
            return Task.CompletedTask;
        }

        private static void DeviTimerCallback(object _)
        {
            var delmg = new List<DiscordMessage>();
            foreach (var msg in DeviMessageTracker)
                if (msg.CreationDate.AddMinutes(30).ToLocalTime() < DateTimeOffset.Now)
                    delmg.Add(msg);
            foreach (var msg in delmg)
                DeviMessageTracker.Remove(msg);
        }

        private static void DeviSettingsCallback(object _)
        {
            var json = JsonConvert.SerializeObject(Settings, Formatting.None);
            var l = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            l = Path.Combine(l, "devi.json");
            File.WriteAllText(l, json, new UTF8Encoding(false));
        }
    }
}

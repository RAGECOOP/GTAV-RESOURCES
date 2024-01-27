using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using RageCoop.Server.Scripting;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace RageCoop.Resources.Discord
{
    public class Main : ServerScript
    {
        public DiscordBot DiscordBot;

        public override void OnStart()
        {
            DiscordBot = new DiscordBot(API, CurrentResource.DataFolder);
            API.Events.OnPlayerConnected += (s, c) =>
            {
                DiscordBot.SendToDiscord($"{c.Username} connected").GetAwaiter().GetResult();
                DiscordBot.SetGame(API.GetAllClients().Count).GetAwaiter().GetResult();
            };
            API.Events.OnPlayerDisconnected += (s, c) =>
            {
                DiscordBot.SendToDiscord($"{c.Username} disconnected").GetAwaiter().GetResult();
                DiscordBot.SetGame(API.GetAllClients().Count).GetAwaiter().GetResult();
            };
            API.Events.OnChatMessage += (s, m) =>
            {
                DiscordBot.SendToDiscord(m.Message, m.Client?.Username ?? m.ClaimedSender ?? "Server").GetAwaiter().GetResult();
            };
            CurrentResource.Logger.Info("Discord resource started");
        }

        public override void OnStop()
        {
            CurrentResource.Logger.Info($"Discord resource stopped");
        }
    }

    public class DiscordSettings
    {
        public string Token { get; set; }
        public string Webhook { get; set; }
        public ulong Channel { get; set; }

        public DiscordSettings()
        {
            Token = "token";
            Webhook = "webhook URL";
            Channel = 0;
        }

        public static DiscordSettings ReadSettings(string path)
        {
            var ser = new XmlSerializer(typeof(DiscordSettings));
            var xmlSettings = new XmlWriterSettings()
            {
                Indent = true,
            };
            DiscordSettings settings = null;
            if (File.Exists(path))
                using (var stream = XmlReader.Create(path)) settings = (DiscordSettings)ser.Deserialize(stream);
            else
                using (var stream = XmlWriter.Create(path, xmlSettings)) ser.Serialize(stream, settings = new DiscordSettings());
            return settings;
        }
    }

    public class DiscordBot
    {
        private readonly DiscordSettings Settings;
        private DiscordSocketClient Client;
        private readonly DiscordWebhookClient Webhook;
        private IMessageChannel Channel;
        private readonly bool Enabled = false;
        private readonly API API;

        public DiscordBot(API api, string datafolder)
        {
            Settings = DiscordSettings.ReadSettings(Path.Combine(AppContext.BaseDirectory, datafolder, "DiscordSettings.xml"));
            if (Settings.Token != "token" && Settings.Webhook != "webhook URL" && Settings.Channel != 0)
            {
                MainAsync().GetAwaiter().GetResult();
                Webhook = new DiscordWebhookClient(Settings.Webhook);
                Enabled = true;
            }
            API = api;
        }

        private async Task MainAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent
            };
            Client = new DiscordSocketClient(config);
            Client.Log += LogAsync;
            Client.Ready += ReadyAsync;
            Client.MessageReceived += MessageReceivedAsync;
            await Client.LoginAsync(TokenType.Bot, Settings.Token);
            await Client.StartAsync();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Channel = (IMessageChannel)Client.GetChannel(Settings.Channel);
            Console.WriteLine($"{Client.CurrentUser} is connected");
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == Client.CurrentUser.Id)
                return;

            if (message.Content == "!ping")
                await message.Channel.SendMessageAsync("pong");
            else if (message.Channel == Channel && !message.Author.IsBot)
            {
                string usr = Regex.Replace(message.Author.Username, @"\p{Cs}", "");
                string msg = Regex.Replace(message.Content, @"\p{Cs}", "");
                if (msg.Length > 0)
                    API.SendChatMessage(msg, null, usr + " [Discord]", false);
            }
        }

        public async Task SendToDiscord(string message, string name = "Server")
        {
            if (Enabled)
                await Webhook.SendMessageAsync(text: message, username: name);
        }

        public async Task SetGame(int n)
        {
            if (Enabled)
                await Client.SetGameAsync(n > 0 ? $"{n} player{(n > 1 ? "s" : "")} online" : null);
        }
    }
}

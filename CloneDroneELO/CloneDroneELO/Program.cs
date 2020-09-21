using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloneDroneELO
{
    class Program
    {
        static DiscordSocketClient _client;
        static CommandHandler _commandHandler;

        static void Main(string[] args)
        {
            MainLoop().GetAwaiter().GetResult();
        }

        static async Task MainLoop()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            string configPath = Path.TrimEndingDirectorySeparator(Directory.GetCurrentDirectory()) + "/config.json";
            BotConfig config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(configPath));

            await _client.LoginAsync(TokenType.Bot, config.Token);
            await _client.StartAsync();

            _commandHandler = new CommandHandler(_client, new CommandService(new CommandServiceConfig { DefaultRunMode = RunMode.Async }));
            await _commandHandler.InstallCommandsAsync();

            await Task.Delay(-1);
        }

        static Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }

    [Group("sensei")]
    public class CommandModule : ModuleBase<SocketCommandContext>
    {
        [Command("test")]
        public async Task TestAsync()
        {
            await Context.Channel.SendMessageAsync("test");
        }

        [Command("nickname")]
        public async Task SetNicknameAsync(string nickname)
        {
            IGuildUser guildUser = (IGuildUser)Context.User;
            await guildUser.ModifyAsync((GuildUserProperties properties) => properties.Nickname = new Optional<string>(nickname));
        }
    }
}

using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloneDroneELO
{
    class Program
    {
        public const uint DEFAULT_ELO = 1000;

        static DiscordSocketClient _client;
        static CommandHandler _commandHandler;
        static UserHandler _userHandler;

        static string _workingDirectory;

        static void Main(string[] args)
        {
            try
            {
                _workingDirectory = Path.TrimEndingDirectorySeparator(Directory.GetCurrentDirectory()) + "/";
                MainLoop().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static string GetUserDataPath()
        {
            return _workingDirectory + "UserData";
        }

        public static async Task<Dictionary<ulong, UserData>> GetOrCreateUserDictionaryAsync()
        {
            string filePath = GetUserDataPath();
            
            FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            byte[] bytes = new byte[fileStream.Length];

            int bytesRead = await fileStream.ReadAsync(bytes, 0, bytes.Length);
            await fileStream.DisposeAsync();

            if (bytesRead <= 0)
                return new Dictionary<ulong, UserData>();

            return await Task.Run(delegate
            {
                MemoryStream memoryStream = new MemoryStream(bytes);
                BinaryReader reader = new BinaryReader(memoryStream);

                Dictionary<ulong, UserData> userDataDictionary = new Dictionary<ulong, UserData>();

                uint dictionaryLength = reader.ReadUInt32();
                for (int i = 0; i < dictionaryLength; i++)
                {
                    UserData userData = UserData.DeserializeFrom(reader);
                    userDataDictionary.Add(userData.UserID, userData);
                }

                reader.Dispose();
                memoryStream.Dispose();

                return userDataDictionary;
            });
        }

        public static async Task WriteUserDataToFileAsync(Dictionary<ulong, UserData> usersDictionary)
        {
            string filePath = GetUserDataPath();

            byte[] bytes = await Task.Run(delegate
            {
                MemoryStream memoryStream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(memoryStream);

                writer.Write((uint)usersDictionary.Count);
                foreach (UserData user in usersDictionary.Values)
                {
                    user.SerializeInto(writer);
                }

                byte[] resultingBytes = memoryStream.ToArray();

                writer.Dispose();
                memoryStream.Dispose();

                return resultingBytes;
            });

            await File.WriteAllBytesAsync(filePath, bytes);
        }

        public static async Task TryChangeUserNicknameTo(ulong userID, string newNickname)
        {
            await _userHandler.SetUserNickname(userID, newNickname);
        }

        static async Task MainLoop()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            string configPath = _workingDirectory + "config.json";
            if (!File.Exists(configPath))
            {
                Console.WriteLine("ERROR: No config file found, excpecting one at \"" + configPath + "\"");
                Console.ReadLine();
                return;
            }

            BotConfig config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(configPath));
            
            await _client.LoginAsync(TokenType.Bot, config.Token);
            await _client.StartAsync();
            await _client.SetStatusAsync(UserStatus.Invisible);

            _commandHandler = new CommandHandler(_client, new CommandService(new CommandServiceConfig { DefaultRunMode = RunMode.Async }));
            await _commandHandler.InstallCommandsAsync();

            await hookUserHandler();

            await Task.Delay(-1);
        }

        static async Task hookUserHandler()
        {
            _userHandler = new UserHandler(_client);
            await _userHandler.HookUserHandler();
        }

        static Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}

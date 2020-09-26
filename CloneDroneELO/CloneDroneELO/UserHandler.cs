using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloneDroneELO
{
    public abstract class BaseUserHandler : IAsyncSerializable
    {
        protected readonly DiscordSocketClient _client;
        protected static Dictionary<ulong, UserData> _userDictionary;

        public async Task InitializeAsync()
        {
            _userDictionary = await Program.GetOrCreateUserDictionaryAsync();

            _client.UserUpdated += onUserUpdatedAsync;

            await HookAsync();
        }

        async Task onUserUpdatedAsync(SocketUser oldUser, SocketUser newUser)
        {

        }

        public BaseUserHandler(DiscordSocketClient discordSocketClient)
        {
            _client = discordSocketClient;
        }

        public abstract Task HookAsync();

        public abstract Task DeserializeAsync(BinaryReader binaryReader);
        public abstract Task SerializeAsync(BinaryWriter binaryWriter);

        protected abstract Task onUserAdded();
    }

    public class TestUserHandler : BaseUserHandler
    {
        public async override Task HookAsync()
        {

        }
    }

    public class UserHandler
    {
        readonly DiscordSocketClient _client;

        Dictionary<ulong, UserData> _userDictionary;

        public UserHandler(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task HookUserHandler()
        {
            _client.UserUpdated += userUpdatedAsync;
            _client.UserJoined += userJoinedAsync;
            _client.Ready += ready;

            await populateELODictionaryAsync();
        }

        async Task<List<IUser>> GetAllUsersAsync()
        {
            List<ulong> userIDs = new List<ulong>();
            List<IUser> users = new List<IUser>();

            foreach (SocketGuild guild in _client.Guilds)
            {
                if (!guild.HasAllMembers)
                    await guild.DownloadUsersAsync();

                foreach (SocketGuildUser user in guild.Users)
                {
                    if (!userIDs.Contains(user.Id))
                    {
                        userIDs.Add(user.Id);
                        users.Add(user);
                    }
                }
            }

            return users;
        }

        async Task ready()
        {
            List<IUser> users = await GetAllUsersAsync();
            foreach (IUser user in users)
            {
                await addOrRefreshUserAsync(user);
            }

            await saveUserDataToFileAsync();
            await _client.SetStatusAsync(UserStatus.Online);
        }

        async Task userUpdatedAsync(SocketUser oldUserData, SocketUser newUserData)
        {
            await addOrRefreshUserAsync(oldUserData);
        }

        async Task userJoinedAsync(SocketGuildUser user)
        {
            await addOrRefreshUserAsync(user);
        }

        async Task saveUserDataToFileAsync()
        {
            if (_userDictionary != null)
                await Program.WriteUserDataToFileAsync(_userDictionary);
        }

        public async Task SetUserNickname(ulong userID, string newNickname)
        {
            if (_userDictionary.TryGetValue(userID, out UserData userData))
            {
                if (userData.NicknameOverride == newNickname || (!userData.HasNicknameOverride && string.IsNullOrWhiteSpace(newNickname)))
                    return;

                userData.NicknameOverride = newNickname;
                await saveUserDataToFileAsync();
                await refreshUserNicknameInAllGuilds(userData);
            }
        }

        public async Task SetUserRegion(ulong userID, RegionType newRegion)
        {
            if (_userDictionary.TryGetValue(userID, out UserData userData))
            {
                if (userData.Region == newRegion)
                    return;

                userData.Region = newRegion;
                await saveUserDataToFileAsync();
                await refreshUserRegionInAllGuilds(userData);
            }
        }

        async Task addOrRefreshUserAsync(IUser user)
        {
            if (user.IsBot || user.IsWebhook)
                return;

            if (!_userDictionary.TryGetValue(user.Id, out UserData userData))
            {
                userData = new UserData(user);
                _userDictionary.Add(user.Id, userData);
            }

            await refreshUserDataInAllGuilds(userData);
        }

        async Task refreshUserDataInAllGuilds(UserData userData)
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                if (!guild.HasAllMembers)
                    await guild.DownloadUsersAsync();

                SocketGuildUser guildUser = guild.GetUser(userData.UserID);
                if (guildUser == null) // The user is not in this guild
                    continue;

                await tryUpdateUserNickname(guildUser, userData);
                await tryUpdateUserRegion(guildUser, userData);
            }
        }

        async Task populateELODictionaryAsync()
        {
            _userDictionary = await Program.GetOrCreateUserDictionaryAsync();
        }

        async Task refreshUserRegionInAllGuilds(UserData userData)
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                if (!guild.HasAllMembers)
                    await guild.DownloadUsersAsync();

                SocketGuildUser guildUser = guild.GetUser(userData.UserID);
                if (guildUser == null) // The user is not in this guild
                    continue;

                await tryUpdateUserRegion(guildUser, userData);
            }
        }

        async Task tryUpdateUserRegion(SocketGuildUser guildUser, UserData userData)
        {
            if (userData.Region == RegionType.None)
                return;

            foreach (SocketRole existingRole in guildUser.Roles)
            {
                if (existingRole.Name.StartsWith("Region: "))
                {
                    if (existingRole.Name.Equals("Region: " + userData.Region, StringComparison.OrdinalIgnoreCase)) // If the user already has the correct role
                        return;

                    await guildUser.RemoveRoleAsync(existingRole);
                    break;
                }
            }

            foreach (SocketRole role in guildUser.Guild.Roles)
            {
                if (role.Name.Equals("Region: " + userData.Region, StringComparison.OrdinalIgnoreCase))
                {
                    await guildUser.AddRoleAsync(role);
                    break;
                }
            }
        }

        async Task refreshUserNicknameInAllGuilds(UserData userData)
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                if (!guild.HasAllMembers)
                    await guild.DownloadUsersAsync();

                SocketGuildUser guildUser = guild.GetUser(userData.UserID);
                if (guildUser == null) // The user is not in this guild
                    continue;

                await tryUpdateUserNickname(guildUser, userData);
            }
        }

        async Task tryUpdateUserNickname(SocketGuildUser guildUser, UserData userData)
        {
            if (guildUser.Guild.GetUser(_client.CurrentUser.Id).Hierarchy <= guildUser.Hierarchy)
            {
                Console.WriteLine("Could not change nickname of user " + guildUser.Username + "#" + guildUser.Discriminator + ". Reason: User's server hierarchy is higher than the bot's (" + guildUser.Guild.Name + ")");
                return;
            }

            string username = guildUser.Username;
            if (userData.HasNicknameOverride)
                username = userData.NicknameOverride;

            string nickname = "[ELO " + userData.ELO + "] " + username;
            if (guildUser.Nickname != nickname)
                await guildUser.ModifyAsync((GuildUserProperties properties) => properties.Nickname = nickname);
        }
    }
}

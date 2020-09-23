using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CloneDroneELO
{
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

        async Task ready()
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                if (!guild.HasAllMembers)
                    await guild.DownloadUsersAsync();

                foreach (SocketGuildUser user in guild.Users)
                {
                    await addOrRefreshUserAsync(user);
                }
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
                userData.NicknameOverride = newNickname;
                await saveUserDataToFileAsync();
                await refreshUserNicknameInAllGuilds(userData);
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

            await refreshUserNicknameInAllGuilds(userData);
        }

        async Task populateELODictionaryAsync()
        {
            _userDictionary = await Program.GetOrCreateUserDictionaryAsync();
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

            string name = guildUser.Username;
            if (userData.HasNicknameOverride)
                name = userData.NicknameOverride;

            await guildUser.ModifyAsync((GuildUserProperties properties) => properties.Nickname = "[ELO " + userData.ELO + "] " + name);
        }
    }
}

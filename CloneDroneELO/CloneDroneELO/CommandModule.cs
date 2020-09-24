using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CloneDroneELO
{
    [Group("sensei")]
    public class CommandModule : ModuleBase<SocketCommandContext>
    {
        [Group("nickname")]
        public class NicknameModule : ModuleBase<SocketCommandContext>
        {
            [Command("set")]
            public async Task SetUserNicknameAsync([Remainder] string nickname)
            {
                nickname = nickname.Trim();
                if (string.IsNullOrWhiteSpace(nickname))
                    return;

                await Program.TryChangeUserNicknameTo(Context.User.Id, nickname);
            }

            [Command("set")]
            [RequireContext(ContextType.Guild)]
            public async Task SetUserNicknameAsync(IGuildUser user, [Remainder] string nickname)
            {
                if (user.Id != Context.User.Id && !user.GuildPermissions.ManageNicknames)
                    return;

                nickname = nickname.Trim();
                if (string.IsNullOrWhiteSpace(nickname))
                    return;

                await Program.TryChangeUserNicknameTo(user.Id, nickname);
            }

            [Command("reset")]
            public async Task ResetUserNicknameAsync()
            {
                await Program.TryChangeUserNicknameTo(Context.User.Id, string.Empty);
            }

            [Command("reset")]
            [RequireContext(ContextType.Guild)]
            public async Task ResetUserNicknameAsync(IGuildUser user)
            {
                if (user.Id != Context.User.Id && !user.GuildPermissions.ManageNicknames)
                    return;

                await Program.TryChangeUserNicknameTo(user.Id, string.Empty);
            }
        }
        
        [Group("setregion")]
        public class RegionModule : ModuleBase<SocketCommandContext>
        {
            [Command]
            public async Task SetUserRegionAsync([Remainder] string regionString)
            {
                regionString = Regex.Replace(regionString, @"\s+", "");
                
                if (!Enum.TryParse(regionString, true, out RegionType region) || region == RegionType.None || region == RegionType.USCentral)
                {
                    await Context.Channel.SendMessageAsync("Invalid region '" + regionString + "'!");
                    return;
                }

                await Program.TrySetPreferredRegion(Context.User.Id, region);
            }

            [Command]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(ChannelPermission.ManageRoles)]
            public async Task SetUserRegionAsync(IGuildUser user, [Remainder] string regionString)
            {
                regionString = Regex.Replace(regionString, @"\s+", "");

                if (!Enum.TryParse(regionString, true, out RegionType region) || region == RegionType.None || region == RegionType.USCentral)
                {
                    await Context.Channel.SendMessageAsync("Invalid region '" + regionString + "'!");
                    return;
                }

                await Program.TrySetPreferredRegion(user.Id, region);
            }
        }
    }
}

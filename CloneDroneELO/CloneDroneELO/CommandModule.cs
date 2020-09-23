using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CloneDroneELO
{
    [Group("sensei")]
    public class CommandModule : ModuleBase<SocketCommandContext>
    {
        [Group("nickname")]
        [Name("Nickname")]
        public class NicknameModule : ModuleBase<SocketCommandContext>
        {
            [Command("set")]
            [Name("Set")]
            [RequireContext(ContextType.Guild)]
            public async Task SetUserNicknameAsync([Name("")] [Remainder] string nickname)
            {
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

                if (string.IsNullOrWhiteSpace(nickname))
                    return;

                await Program.TryChangeUserNicknameTo(user.Id, nickname);
            }

            [Command("reset")]
            [RequireContext(ContextType.Guild)]
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
        
        [Group("region")]
        public class RegionModule : ModuleBase<SocketCommandContext>
        {
            [Command("setpreferred")]
            [RequireContext(ContextType.Guild)]
            public async Task SetUserRegionAsync([Remainder] string region)
            {
                region = Regex.Replace(region, @"\s+", "");
                
                SocketGuildUser user = Context.Guild.GetUser(Context.User.Id);
                
            }
        }
    }
}

using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloneDroneELO
{
    public static class ExtensionMethods
    {
        public static SocketGuildUser GetGuildUser(this SocketCommandContext context)
        {
            return context.Guild.GetUser(context.User.Id);
        }
    }
}

using Bot.Abstractions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RoleReactions.Data;

namespace RoleReactions.Services;

public class RoleJoinService(DiscordSocketClient client, IServiceProvider serviceProvider) : IEvent
{
    public void RegisterEvents() => client.UserJoined += AddRoles;

    private async Task AddRoles(SocketGuildUser user)
    {
        using var scope = serviceProvider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<RoleReactionsDatabase>();

        var userInfos = database.UserRoles.Where(x => x.GuildId == user.Guild.Id && x.UserId == user.Id);

        foreach (var userInfo in userInfos)
        {
            foreach (var roleId in userInfo.RoleIds)
            {
                var role = user.Guild.GetRole(roleId);

                if (role == null) continue;

                await user.AddRoleAsync(role);
            }
        }
    }
}

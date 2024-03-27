using Bot.Enums;
using Bot.Exceptions;
using Bot.Models;
using Bot.Services;
using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Abstractions;

public abstract class Identity(string token, IServiceProvider serviceProvider)
{
    protected readonly DiscordRest DiscordRest = serviceProvider.GetRequiredService<DiscordRest>();
    public readonly IServiceProvider ServiceProvider = serviceProvider;

    public readonly string Token = token;

    protected IUser CurrentUser;
    protected List<UserGuild> CurrentUserGuilds;

    public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddMinutes(15);

    public abstract bool IsOnGuild(ulong guildId);

    public abstract Task<bool> HasAdminRoleOnGuild(ulong guildId);

    public abstract Task<bool> HasModRoleOrHigherOnGuild(ulong guildId);

    public bool HasPermission(GuildPermission permission, ulong guildId)
    {
        var guild = DiscordRest.FetchGuildInfo(guildId, CacheBehavior.Default);

        if (CurrentUser is null || guild is null)
            return false;

        var user = DiscordRest.FetchGuildUserInfo(guildId, CurrentUser.Id, CacheBehavior.Default);

        return user is not null && (user.Guild.OwnerId == user.Id || user.GuildPermissions.Has(permission));
    }

    public async Task<bool> HasPermission(DiscordPermission permission, ulong guildId)
    {
        using var scope = ServiceProvider.CreateScope();

        if (await IsSiteAdmin())
            return true;

        try
        {
            return permission switch
            {
                DiscordPermission.User => IsOnGuild(guildId),
                DiscordPermission.Moderator => await HasModRoleOrHigherOnGuild(guildId),
                DiscordPermission.Admin => await HasAdminRoleOnGuild(guildId),
                _ => false
            };
        }
        catch (UnregisteredGuildException)
        {
            var guild = DiscordRest.FetchGuildInfo(guildId, CacheBehavior.Default);
            var user = await guild.GetUserAsync(CurrentUser.Id);

            return permission switch
            {
                DiscordPermission.User => user != null,
                DiscordPermission.Moderator => user.GuildPermissions.Administrator,
                DiscordPermission.Admin => user.GuildPermissions.Administrator,
                _ => false
            };
        }
    }

    public async Task RequireSiteAdmin()
    {
        if (!await IsSiteAdmin())
            throw new UnauthorizedException();
    }

    public async Task RequirePermission(DiscordPermission permission, ulong guildId)
    {
        if (!await HasPermission(permission, guildId))
            throw new UnauthorizedException();
    }

    public abstract Task<bool> IsSiteAdmin();

    public IUser GetCurrentUser() => CurrentUser ?? throw new InvalidIdentityException(Token);

    public List<UserGuild> GetCurrentUserGuilds() => CurrentUserGuilds ?? throw new InvalidIdentityException(Token);

    public virtual void RemoveGuildMembership(ulong guildId) => CurrentUserGuilds.RemoveAll(x => x.Id == guildId);

    public virtual void AddGuildMembership(IGuildUser user)
    {
        if (CurrentUserGuilds.All(x => x.Id != user.Guild.Id))
            CurrentUserGuilds.Add(UserGuild.GetUserGuild(user));
    }

    public virtual void UpdateGuildMembership(IGuildUser user)
    {
        if (CurrentUserGuilds.All(x => x.Id != user.Guild.Id))
            CurrentUserGuilds.Add(UserGuild.GetUserGuild(user));
    }
}

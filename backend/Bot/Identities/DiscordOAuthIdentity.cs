using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Exceptions;
using Bot.Models;
using Bot.Services;
using Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bot.Identities;

public class DiscordOAuthIdentity : Identity
{
    private readonly Dictionary<ulong, IGuildUser> _guildMemberships;

    public DiscordOAuthIdentity(string token, IServiceProvider serviceProvider, IUser currentUser,
        List<UserGuild> userGuilds) : base(token, serviceProvider)
    {
        CurrentUser = currentUser;
        CurrentUserGuilds = userGuilds;

        _guildMemberships = [];
    }

    public static string TryGetKey(HttpContext context) =>
        context is not null ? context.Request.Cookies["dexter_access_token"] : string.Empty;

    public static async Task<Identity> TryMakeIdentity(HttpContext httpContext, IServiceProvider services)
    {
        var token = await httpContext.GetTokenAsync("Cookies", "access_token");

        if (!string.IsNullOrEmpty(token))
        {
            services.GetRequiredService<ILogger<DiscordOAuthIdentity>>()
                .LogInformation("Registering new DiscordIdentity.");

            var api = services.GetRequiredService<DiscordRest>();
            var user = await api.FetchCurrentUserInfo(token, CacheBehavior.IgnoreButCacheOnError);
            var guilds = await api.FetchGuildsOfCurrentUser(token, CacheBehavior.IgnoreButCacheOnError);

            return new DiscordOAuthIdentity(token, services, user, guilds);
        }

        return null;
    }

    public override bool IsOnGuild(ulong guildId) => CurrentUser != null && CurrentUserGuilds.Any(x => x.Id == guildId);

    public IGuildUser GetGuildMembership(ulong guildId)
    {
        if (_guildMemberships.TryGetValue(guildId, out var value))
            return value;
        if (CurrentUser is null)
            return null;

        var guildUser = DiscordRest.FetchGuildUserInfo(guildId, CurrentUser.Id, CacheBehavior.Default);

        if (guildUser is null)
            return null;

        _guildMemberships[guildId] = guildUser;

        return guildUser;
    }

    public override async Task<bool> HasAdminRoleOnGuild(ulong guildId)
    {
        if (!IsOnGuild(guildId))
            return false;

        try
        {
            using var scope = ServiceProvider.CreateScope();

            var guildConfigRepo = scope.ServiceProvider.GetRequiredService<GuildConfigRepository>();

            var guildConfig = await guildConfigRepo.GetGuildConfig(guildId);

            var guildUser = GetGuildMembership(guildId);

            return guildUser is not null
&& (guildUser.Guild.OwnerId == guildUser.Id ||
                   guildUser.RoleIds.Any(x => guildConfig.AdminRoles.Contains(x)));
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
    }

    public override async Task<bool> HasModRoleOrHigherOnGuild(ulong guildId)
    {
        if (!IsOnGuild(guildId))
            return false;

        try
        {
            using var scope = ServiceProvider.CreateScope();

            var guildConfigRepo = scope.ServiceProvider.GetRequiredService<GuildConfigRepository>();

            var guildConfig = await guildConfigRepo.GetGuildConfig(guildId);

            var guildUser = GetGuildMembership(guildId);

            return guildUser is not null
&& (guildUser.Guild.OwnerId == guildUser.Id
|| guildUser.RoleIds.Any(x => guildConfig.AdminRoles.Contains(x) ||
                                              guildConfig.ModRoles.Contains(x)));
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
    }

    public override async Task<bool> IsSiteAdmin()
    {
        if (CurrentUser is null)
            return false;

        using var scope = ServiceProvider.CreateScope();

        var config = await scope.ServiceProvider
            .GetRequiredService<SettingsRepository>().GetAppSettings();

        return config.SiteAdmins.Contains(CurrentUser.Id);
    }

    public override void RemoveGuildMembership(ulong guildId)
    {
        CurrentUserGuilds.RemoveAll(x => x.Id == guildId);
        _guildMemberships.Remove(guildId);
    }

    public override void AddGuildMembership(IGuildUser user)
    {
        if (CurrentUserGuilds.All(x => x.Id != user.Guild.Id))
            CurrentUserGuilds.Add(UserGuild.GetUserGuild(user));

        _guildMemberships[user.Guild.Id] = user;
    }

    public override void UpdateGuildMembership(IGuildUser user)
    {
        if (CurrentUserGuilds.All(x => x.Id != user.Guild.Id))
            CurrentUserGuilds.Add(UserGuild.GetUserGuild(user));

        _guildMemberships[user.Guild.Id] = user;
    }
}

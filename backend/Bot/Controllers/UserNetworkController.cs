using Bot.Abstractions;
using Bot.Data;
using Bot.Dynamics;
using Bot.Enums;
using Bot.Exceptions;
using Bot.Models;
using Bot.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;

namespace Bot.Controllers;

[Route("api/v1/network")]
public class UserNetworkController(GuildConfigRepository guildConfigRepository, DiscordRest discordRest,
    IdentityManager identityManager, CachedServices cachedServices, IServiceProvider serviceProvider) : AuthenticatedController(identityManager, guildConfigRepository)
{
    private readonly CachedServices _cachedServices = cachedServices;
    private readonly DiscordRest _discordRest = discordRest;
    private readonly GuildConfigRepository _guildConfigRepository = guildConfigRepository;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    [HttpGet("user")]
    public async Task<IActionResult> GetUserNetwork([FromQuery][Required] ulong userId)
    {
        var identity = await SetupAuthentication();

        List<string> modGuilds = [];
        List<DiscordGuild> guildViews = [];

        var guildConfigs = await _guildConfigRepository.GetAllGuildConfigs();

        if (guildConfigs.Count == 0)
            throw new NoGuildsRegisteredException();

        foreach (var guildConfig in guildConfigs)
        {
            if (await identity.HasPermission(DiscordPermission.Moderator, guildConfig.GuildId))
            {
                modGuilds.Add(guildConfig.GuildId.ToString());
                guildViews.Add(
                    DiscordGuild.GetDiscordGuild(
                        _discordRest.FetchGuildInfo(guildConfig.GuildId, CacheBehavior.Default)));
            }
        }

        if (modGuilds.Count == 0)
            return Unauthorized();

        var searchedUser = DiscordUser.GetDiscordUser(await _discordRest.FetchUserInfo(userId, false));

        dynamic data = new ExpandoObject();

        data.guilds = guildViews;
        data.user = searchedUser;

        foreach (var repo in
                 _cachedServices.GetInitializedAuthenticatedClasses<IAddNetworks>(_serviceProvider, identity))
            await repo.AddNetworkData(data, modGuilds, userId);

        return Ok(data);
    }
}

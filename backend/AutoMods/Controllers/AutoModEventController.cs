using AutoMods.Data;
using AutoMods.Models;
using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AutoMods.Controllers;

[Route("api/v1/guilds/{guildId}/automods")]
public class AutoModEventController(IdentityManager identityManager, AutoModEventRepository autoModEventRepository,
    GuildConfigRepository guildConfigRepo) : AuthenticatedController(identityManager, autoModEventRepository, guildConfigRepo)
{
    private readonly AutoModEventRepository _autoModEventRepository = autoModEventRepository;
    private readonly GuildConfigRepository _guildConfigRepo = guildConfigRepo;

    [HttpGet]
    public async Task<IActionResult> GetAllItems([FromRoute] ulong guildId,
        [FromQuery][Range(0, int.MaxValue)] int startPage = 0)
    {
        var identity = await SetupAuthentication();

        await _guildConfigRepo.RequireGuildRegistered(guildId);

        ulong userOnly = 0;

        if (!await identity.HasPermission(DiscordPermission.Moderator, guildId))
            userOnly = identity.GetCurrentUser().Id;

        List<AutoModEvent> events;

        int eventsCount;

        if (userOnly == 0)
        {
            events = await _autoModEventRepository.GetPagination(guildId, startPage);
            eventsCount = await _autoModEventRepository.CountEventsByGuild(guildId);
        }
        else
        {
            events = await _autoModEventRepository.GetPaginationFilteredForUser(guildId, userOnly, startPage);
            eventsCount = await _autoModEventRepository.CountEventsByGuildAndUser(guildId, userOnly);
        }

        return Ok(new
        {
            events,
            count = eventsCount
        });
    }
}

using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Exceptions;
using Bot.Services;
using Messaging.Data;
using Messaging.DTOs;
using Messaging.Enums;
using Messaging.Exceptions;
using Messaging.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Messaging.Controllers;

[Route("api/v1/guilds/{guildId}/scheduledmessages")]
public class ScheduledMessageController(MessagingRepository messagingRepo, GuildConfigRepository guildConfigRepo,
    DiscordRest discordRest, IdentityManager identityManager) : AuthenticatedController(identityManager, messagingRepo, guildConfigRepo)
{
    private readonly DiscordRest _discordRest = discordRest;
    private readonly GuildConfigRepository _guildConfigRepo = guildConfigRepo;
    private readonly MessagingRepository _messagingRepo = messagingRepo;

    [HttpGet]
    public async Task<IActionResult> GetMessages([FromRoute] ulong guildId,
        [FromQuery][Range(0, int.MaxValue)] int page = 0)
    {
        var identity = await SetupAuthentication();

        await identity.RequirePermission(DiscordPermission.Moderator, guildId);
        await _guildConfigRepo.RequireGuildRegistered(guildId);


        var messages = await _messagingRepo.GetAllMessages(guildId, page);

        var results = new List<ScheduledMessageExtended>();

        foreach (var message in messages)
        {
            results.Add(new ScheduledMessageExtended(
                message,
                await _discordRest.FetchUserInfo(message.CreatorId, true),
                await _discordRest.FetchUserInfo(message.LastEditedById, true),
                _discordRest.FetchGuildChannels(guildId, CacheBehavior.OnlyCache)
                    .FirstOrDefault(x => x.Id == message.ChannelId)));
        }

        return Ok(results);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromRoute] ulong guildId,
        [FromBody] ScheduledMessageForCreateDto dto)
    {
        var identity = await SetupAuthentication();

        await identity.RequirePermission(DiscordPermission.Moderator, guildId);
        await _guildConfigRepo.RequireGuildRegistered(guildId);

        var message = new ScheduledMessage
        {
            Name = dto.Name,
            Content = dto.Content,
            ScheduledFor = dto.ScheduledFor,
            ChannelId = dto.ChannelId,
            GuildId = guildId
        };

        if (message.ScheduledFor < DateTime.UtcNow.AddMinutes(1))
            throw new InvalidDateForScheduledMessageException();

        message = await _messagingRepo.CreateMessage(message);

        return Ok(new ScheduledMessageExtended(
            message,
            await _discordRest.FetchUserInfo(message.CreatorId, false),
            await _discordRest.FetchUserInfo(message.LastEditedById, false),
            _discordRest.FetchGuildChannels(guildId, CacheBehavior.OnlyCache)
                .FirstOrDefault(x => x.Id == message.ChannelId)
        ));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditMessage([FromRoute] ulong guildId, [FromRoute] int id,
        [FromBody] ScheduledMessageForPutDto dto)
    {
        var identity = await SetupAuthentication();

        await identity.RequirePermission(DiscordPermission.Moderator, guildId);
        await _guildConfigRepo.RequireGuildRegistered(guildId);

        var message = await _messagingRepo.GetMessage(id);

        if (message.GuildId != guildId)
            throw new UnauthorizedException();

        if (message.Status != ScheduledMessageStatus.Pending)
            throw new ProtectedScheduledMessageException();

        if (dto.ScheduledFor < DateTime.UtcNow.AddMinutes(1))
            throw new InvalidDateForScheduledMessageException();

        message.Name = dto.Name;
        message.Content = dto.Content;
        message.ScheduledFor = dto.ScheduledFor;
        message.ChannelId = dto.ChannelId;

        message = await _messagingRepo.UpdateMessage(message);

        return Ok(new ScheduledMessageExtended(
            message,
            await _discordRest.FetchUserInfo(message.CreatorId, false),
            await _discordRest.FetchUserInfo(message.LastEditedById, false),
            _discordRest.FetchGuildChannels(guildId, CacheBehavior.OnlyCache)
                .FirstOrDefault(x => x.Id == message.ChannelId)
        ));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage([FromRoute] ulong guildId, [FromRoute] int id)
    {
        var identity = await SetupAuthentication();

        await identity.RequirePermission(DiscordPermission.Moderator, guildId);
        await _guildConfigRepo.RequireGuildRegistered(guildId);

        var message = await _messagingRepo.GetMessage(id);

        if (message.GuildId != guildId)
            throw new UnauthorizedException();

        if (message.Status != ScheduledMessageStatus.Pending && !await identity.HasAdminRoleOnGuild(guildId))
            throw new ProtectedScheduledMessageException();

        await _messagingRepo.DeleteMessage(message.Id);
        return Ok();
    }
}

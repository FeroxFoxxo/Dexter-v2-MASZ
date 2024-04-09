using Bot.Abstractions;
using Bot.Enums;
using Bot.Models;
using Bot.Services;
using Invites.Data;
using Invites.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Invites.Controllers;

[Route("api/v1/network")]
public class UserNetworkController(IdentityManager identityManager, DiscordRest discordRest,
    InviteRepository inviteRepository) : AuthenticatedController(identityManager, inviteRepository)
{
    private readonly DiscordRest _discordRest = discordRest;
    private readonly InviteRepository _inviteRepository = inviteRepository;

    [HttpGet("invite")]
    public async Task<IActionResult> GetInviteNetwork([FromQuery][Required] string inviteUrl)
    {
        var identity = await SetupAuthentication();

        var invites = await _inviteRepository.GetInvitesByCode(inviteUrl);

        if (invites == null || invites.Count == 0)
            return NotFound();

        await identity.RequirePermission(DiscordPermission.Moderator, invites.First().GuildId);

        var guild = DiscordGuild.GetDiscordGuild(_discordRest.FetchGuildInfo(invites.First().GuildId,
            CacheBehavior.Default));

        List<UserInviteExpanded> inviteViews = [];

        foreach (var invite in invites)
        {
            inviteViews.Add(new UserInviteExpanded(
                invite,
                await _discordRest.FetchUserInfo(invite.JoinedUserId, true),
                await _discordRest.FetchUserInfo(invite.InviteIssuerId, true)
            ));
        }

        return Ok(new
        {
            invites = inviteViews,
            guild
        });
    }
}

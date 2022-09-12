using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Exceptions;
using Bot.Models;
using Bot.Services;
using Microsoft.AspNetCore.Mvc;
using Punishments.Data;
using Punishments.Extensions;
using Punishments.Models;
using UserNotes.Data;
using UserNotes.Models;

namespace Punishments.Controllers;

[Route("api/v1/guilds/{guildId}/cases/{caseId}/view")]
public class ModCaseViewController : AuthenticatedController
{
	private readonly DiscordRest _discordRest;
	private readonly GuildConfigRepository _guildConfigRepository;
	private readonly ModCaseRepository _modCaseRepository;
	private readonly UserNoteRepository _userNoteRepository;

	public ModCaseViewController(IdentityManager identityManager, GuildConfigRepository guildConfigRepository,
		UserNoteRepository userNoteRepository, ModCaseRepository modCaseRepository, DiscordRest discordRest) :
		base(identityManager, guildConfigRepository, userNoteRepository, modCaseRepository)
	{
		_guildConfigRepository = guildConfigRepository;
		_userNoteRepository = userNoteRepository;
		_modCaseRepository = modCaseRepository;
		_discordRest = discordRest;
	}

	[HttpGet]
	public async Task<IActionResult> GetModCaseView([FromRoute] ulong guildId, [FromRoute] int caseId)
	{
		var identity = await SetupAuthentication();

		var guildConfig = await _guildConfigRepository.GetGuildConfig(guildId);

		var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

		await identity.RequirePermission(ApiActionPermission.View, modCase);

		var suspect = await _discordRest.FetchUserInfo(modCase.UserId, CacheBehavior.OnlyCache);

		var comments = new List<ModCaseCommentExpanded>();

		foreach (var comment in modCase.Comments)
			comments.Add(new ModCaseCommentExpanded(
				comment,
				await _discordRest.FetchUserInfo(comment.UserId, CacheBehavior.OnlyCache)
			));

		UserNoteExpanded userNote = null;

		if (await identity.HasPermission(DiscordPermission.Moderator, guildId))
			try
			{
				var note = await _userNoteRepository.GetUserNote(guildId, modCase.UserId);
				userNote = new UserNoteExpanded(
					note,
					suspect,
					await _discordRest.FetchUserInfo(note.CreatorId, CacheBehavior.OnlyCache)
				);
			}
			catch (ResourceNotFoundException)
			{
			}

		var caseView = new ModCaseExpanded(
			modCase,
			await _discordRest.FetchUserInfo(modCase.ModId, CacheBehavior.OnlyCache),
			await _discordRest.FetchUserInfo(modCase.LastEditedByModId, CacheBehavior.OnlyCache),
			suspect,
			comments,
			userNote
		);

		if (modCase.LockedByUserId != 0)
			caseView.LockedBy =
				DiscordUser.FromUser(await _discordRest.FetchUserInfo(modCase.LockedByUserId, CacheBehavior.OnlyCache));

		if (modCase.DeletedByUserId != 0)
			caseView.DeletedBy =
				DiscordUser.FromUser(await _discordRest.FetchUserInfo(modCase.DeletedByUserId, CacheBehavior.OnlyCache));

		if (!(await identity.HasPermission(DiscordPermission.Moderator, guildId) || guildConfig.PublishModeratorInfo))
			caseView.RemoveModeratorInfo();

		return Ok(caseView);
	}
}
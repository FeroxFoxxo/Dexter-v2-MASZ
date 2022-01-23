using MASZ.Bot.Abstractions;
using MASZ.Bot.Data;
using MASZ.Bot.Enums;
using MASZ.Bot.Extensions;
using MASZ.Bot.Services;
using MASZ.Punishments.Data;
using MASZ.Punishments.DTOs;
using MASZ.Punishments.Enums;
using MASZ.Punishments.Models;
using MASZ.Punishments.Translators;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MASZ.Punishments.Controllers;

[Route("api/v1/guilds/{guildId}")]
public class ModCaseTableController : AuthenticatedController
{
	private readonly DiscordRest _discordRest;
	private readonly GuildConfigRepository _guildConfigRepository;
	private readonly ModCaseRepository _modCaseRepository;
	private readonly Translation _translator;

	public ModCaseTableController(GuildConfigRepository guildConfigRepository, ModCaseRepository modCaseRepository,
		Translation translator, DiscordRest discordRest, IdentityManager identityManager) :
		base(identityManager, guildConfigRepository, modCaseRepository)
	{
		_guildConfigRepository = guildConfigRepository;
		_modCaseRepository = modCaseRepository;
		_translator = translator;
		_discordRest = discordRest;
	}

	[HttpPost("modcasetable")]
	public async Task<IActionResult> GetAllModCases([FromRoute] ulong guildId,
		[FromQuery] [Range(0, int.MaxValue)] int startPage = 0, [FromBody] ModCaseTableFilterDto search = null)
	{
		return Ok(await GenerateTable(guildId, ModCaseTableType.Default, startPage, search));
	}

	[HttpPost("expiringpunishment")]
	public async Task<IActionResult> GetExpiringPunishments([FromRoute] ulong guildId,
		[FromQuery] [Range(0, int.MaxValue)] int startPage = 0, [FromBody] ModCaseTableFilterDto search = null)
	{
		return Ok(await GenerateTable(guildId, ModCaseTableType.OnlyPunishments, startPage, search,
			ModCaseTableSortType.SortByExpiring));
	}

	[HttpPost("casebin")]
	public async Task<IActionResult> GetDeletedModCases([FromRoute] ulong guildId,
		[FromQuery] [Range(0, int.MaxValue)] int startPage = 0, [FromBody] ModCaseTableFilterDto search = null)
	{
		return Ok(await GenerateTable(guildId, ModCaseTableType.OnlyBin, startPage, search,
			ModCaseTableSortType.SortByDeleting));
	}

	private async Task<CaseTable> GenerateTable(ulong guildId, ModCaseTableType tableType, int startPage = 0,
		ModCaseTableFilterDto search = null, ModCaseTableSortType sortBy = ModCaseTableSortType.Default)
	{
		var identity = await SetupAuthentication();

		var guildConfig = await _guildConfigRepository.GetGuildConfig(guildId);

		ulong userOnly = 0;

		if (!await identity.HasPermission(DiscordPermission.Moderator, guildId))
			userOnly = identity.GetCurrentUser().Id;

		var modCases = await _modCaseRepository.GetCasesForGuild(guildId);

		modCases = sortBy switch
		{
			ModCaseTableSortType.SortByExpiring => modCases.Where(x => x.PunishedUntil != null)
				.OrderBy(x => x.PunishedUntil)
				.ToList(),
			ModCaseTableSortType.SortByDeleting => modCases.OrderBy(x => x.MarkedToDeleteAt).ToList(),
			_ => modCases
		};

		if (userOnly != 0)
			modCases = modCases.Where(x => x.UserId == userOnly).ToList();

		modCases = tableType switch
		{
			ModCaseTableType.OnlyPunishments => modCases.Where(x => x.PunishmentActive).ToList(),
			ModCaseTableType.OnlyBin => modCases.Where(x => x.MarkedToDeleteAt != null).ToList(),
			_ => modCases
		};

		var publishMod = guildConfig.PublishModeratorInfo ||
		                 await identity.HasPermission(DiscordPermission.Moderator, guildId);
		List<ModCaseTableEntry> tmp = new();
		foreach (var c in modCases)
		{
			var entry = new ModCaseTableEntry(
				c,
				await _discordRest.FetchUserInfo(c.ModId, CacheBehavior.OnlyCache),
				await _discordRest.FetchUserInfo(c.UserId, CacheBehavior.OnlyCache)
			);

			if (!publishMod)
				entry.RemoveModeratorInfo();

			tmp.Add(entry);
		}

		var table = tmp.AsEnumerable();

		if (!string.IsNullOrWhiteSpace(search?.CustomTextFilter))
			table = table.Where(t =>
				search.CustomTextFilter.Search(t.ModCase.Title) ||
				search.CustomTextFilter.Search(t.ModCase.Description) ||
				search.CustomTextFilter.Search(_translator.Get<PunishmentEnumTranslator>()
					.Enum(t.ModCase.PunishmentType)) ||
				search.CustomTextFilter.Search(t.ModCase.Username) ||
				search.CustomTextFilter.Search(t.ModCase.Discriminator) ||
				search.CustomTextFilter.Search(t.ModCase.Nickname) ||
				search.CustomTextFilter.Search(t.ModCase.UserId) ||
				search.CustomTextFilter.Search(t.ModCase.ModId) ||
				search.CustomTextFilter.Search(t.ModCase.LastEditedByModId) ||
				search.CustomTextFilter.Search(t.ModCase.CreatedAt) ||
				search.CustomTextFilter.Search(t.ModCase.OccurredAt) ||
				search.CustomTextFilter.Search(t.ModCase.LastEditedAt) ||
				search.CustomTextFilter.Search(t.ModCase.Labels) ||
				search.CustomTextFilter.Search(t.ModCase.CaseId.ToString()) ||
				search.CustomTextFilter.Search("#" + t.ModCase.CaseId) ||
				search.CustomTextFilter.Search(t.Moderator) ||
				search.CustomTextFilter.Search(t.Suspect));

		if (search?.UserIds != null && search.UserIds.Count > 0)
			table = table.Where(x => search.UserIds.Contains(x.ModCase.UserId));

		if (search?.ModeratorIds != null && search.ModeratorIds.Count > 0)
			table = table.Where(x =>
				search.ModeratorIds.Contains(x.ModCase.ModId) ||
				search.ModeratorIds.Contains(x.ModCase.LastEditedByModId));

		if (search?.Since != null && search.Since != DateTime.MinValue)
			table = table.Where(x => x.ModCase.CreatedAt >= search.Since);

		if (search?.Before != null && search.Before != DateTime.MinValue)
			table = table.Where(x => x.ModCase.CreatedAt <= search.Before);

		if (search?.PunishedUntilMin != null && search.PunishedUntilMin != DateTime.MinValue)
			table = table.Where(x => x.ModCase.PunishedUntil >= search.PunishedUntilMin);

		if (search?.PunishedUntilMax != null && search.PunishedUntilMax != DateTime.MinValue)
			table = table.Where(x => x.ModCase.PunishedUntil <= search.PunishedUntilMax);

		if (search?.Edited != null)
			table = table.Where(x => x.ModCase.LastEditedAt == x.ModCase.CreatedAt != search.Edited.Value);

		if (search?.CreationTypes != null && search.CreationTypes.Count > 0)
			table = table.Where(x => search.CreationTypes.Contains(x.ModCase.CreationType));

		if (search?.PunishmentTypes != null && search.PunishmentTypes.Count > 0)
			table = table.Where(x => search.PunishmentTypes.Contains(x.ModCase.PunishmentType));

		if (search?.PunishmentActive != null)
			table = table.Where(x => search.PunishmentActive != null && x.ModCase.PunishmentActive == search.PunishmentActive.Value);

		if (search?.LockedComments != null)
			table = table.Where(x => search.LockedComments != null && x.ModCase.AllowComments != search.LockedComments.Value);

		if (search?.MarkedToDelete != null)
			table = table.Where(x => search.MarkedToDelete != null && x.ModCase.MarkedToDeleteAt.HasValue == search.MarkedToDelete.Value);

		var modCaseTableEntries = table as ModCaseTableEntry[] ?? table.ToArray();

		return new CaseTable(modCaseTableEntries.Skip(startPage * 20).Take(20).ToList(),
			modCaseTableEntries.Length);
	}
}
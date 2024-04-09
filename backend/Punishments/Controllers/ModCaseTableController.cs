using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Extensions;
using Bot.Models;
using Bot.Services;
using Microsoft.AspNetCore.Mvc;
using Punishments.Data;
using Punishments.DTOs;
using Punishments.Enums;
using Punishments.Models;
using Punishments.Translators;
using System.ComponentModel.DataAnnotations;

namespace Punishments.Controllers;

[Route("api/v1/guilds/{guildId}")]
public class ModCaseTableController(GuildConfigRepository guildConfigRepository, ModCaseRepository modCaseRepository,
    Translation translator, DiscordRest discordRest, IdentityManager identityManager) : AuthenticatedController(identityManager, guildConfigRepository, modCaseRepository)
{
    private readonly DiscordRest _discordRest = discordRest;
    private readonly GuildConfigRepository _guildConfigRepository = guildConfigRepository;
    private readonly ModCaseRepository _modCaseRepository = modCaseRepository;
    private readonly Translation _translator = translator;

    [HttpPost("modcasetable")]
    public async Task<IActionResult> GetAllModCases([FromRoute] ulong guildId,
        [FromQuery][Range(0, int.MaxValue)] int startPage = 0, [FromBody] ModCaseTableFilterDto search = null) =>
        Ok(await GenerateTable(guildId, ModCaseTableType.Default, startPage, search));

    [HttpPost("expiringpunishment")]
    public async Task<IActionResult> GetExpiringPunishments([FromRoute] ulong guildId,
        [FromQuery][Range(0, int.MaxValue)] int startPage = 0, [FromBody] ModCaseTableFilterDto search = null) =>
        Ok(await GenerateTable(guildId, ModCaseTableType.OnlyPunishments, startPage, search,
            ModCaseTableSortType.SortByExpiring));

    [HttpPost("casebin")]
    public async Task<IActionResult> GetDeletedModCases([FromRoute] ulong guildId,
        [FromQuery][Range(0, int.MaxValue)] int startPage = 0, [FromBody] ModCaseTableFilterDto search = null) =>
        Ok(await GenerateTable(guildId, ModCaseTableType.OnlyBin, startPage, search,
            ModCaseTableSortType.SortByDeleting));

    private async Task<ModCaseTable> GenerateTable(ulong guildId, ModCaseTableType tableType, int startPage = 0,
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
            ModCaseTableSortType.SortByExpiring =>
            [
                .. modCases.Where(x => x.PunishedUntil != null)
                                .OrderBy(x => x.PunishedUntil)
,
            ],
            ModCaseTableSortType.SortByDeleting => [.. modCases.OrderBy(x => x.MarkedToDeleteAt)],
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

        var table = modCases.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search?.CustomTextFilter))
            table = table.Where(t =>
                search.CustomTextFilter.Search(t.Title) ||
                search.CustomTextFilter.Search(t.Description) ||
                search.CustomTextFilter.Search(_translator.Get<PunishmentEnumTranslator>()
                    .Enum(t.PunishmentType)) ||
                search.CustomTextFilter.Search(t.Username) ||
                search.CustomTextFilter.Search(t.Nickname) ||
                search.CustomTextFilter.Search(t.UserId) ||
                search.CustomTextFilter.Search(t.ModId) ||
                search.CustomTextFilter.Search(t.LastEditedByModId) ||
                search.CustomTextFilter.Search(t.CreatedAt) ||
                search.CustomTextFilter.Search(t.OccurredAt) ||
                search.CustomTextFilter.Search(t.LastEditedAt) ||
                search.CustomTextFilter.Search(t.Labels) ||
                search.CustomTextFilter.Search(t.CaseId.ToString()) ||
                search.CustomTextFilter.Search("#" + t.CaseId) ||
                search.CustomTextFilter.Search(
                    DiscordUser.GetDiscordUser(_discordRest.FetchMemCachedUserInfo(t.ModId))
                ) ||
                search.CustomTextFilter.Search(_translator.Get<PunishmentEnumTranslator>()
                    .Enum(t.Severity)) ||
                search.CustomTextFilter.Search(
                    DiscordUser.GetDiscordUser(_discordRest.FetchMemCachedUserInfo(t.UserId)
                    )));

        if (search?.UserIds != null && search.UserIds.Count > 0)
            table = table.Where(x => search.UserIds.Contains(x.UserId.ToString()));

        if (search?.ModeratorIds != null && search.ModeratorIds.Count > 0)
            table = table.Where(x =>
                search.ModeratorIds.Contains(x.ModId.ToString()) ||
                search.ModeratorIds.Contains(x.LastEditedByModId.ToString()));

        if (search?.Since != null && search.Since != DateTime.MinValue)
            table = table.Where(x => x.CreatedAt >= search.Since);

        if (search?.Before != null && search.Before != DateTime.MinValue)
            table = table.Where(x => x.CreatedAt <= search.Before);

        if (search?.PunishedUntilMin != null && search.PunishedUntilMin != DateTime.MinValue)
            table = table.Where(x => x.PunishedUntil >= search.PunishedUntilMin);

        if (search?.PunishedUntilMax != null && search.PunishedUntilMax != DateTime.MinValue)
            table = table.Where(x => x.PunishedUntil <= search.PunishedUntilMax);

        if (search?.Edited != null)
            table = table.Where(x => x.LastEditedAt == x.CreatedAt != search.Edited.Value);

        if (search?.CreationTypes != null && search.CreationTypes.Count > 0)
            table = table.Where(x => search.CreationTypes.Contains(x.CreationType));

        if (search?.SeverityTypes != null && search.SeverityTypes.Count > 0)
            table = table.Where(x => search.SeverityTypes.Contains(x.Severity));

        if (search?.PunishmentTypes != null && search.PunishmentTypes.Count > 0)
            table = table.Where(x => search.PunishmentTypes.Contains(x.PunishmentType));

        if (search?.PunishmentActive != null)
            table = table.Where(x =>
                search.PunishmentActive != null && x.PunishmentActive == search.PunishmentActive.Value);

        if (search?.LockedComments != null)
            table = table.Where(x => search.LockedComments != null && x.AllowComments != search.LockedComments.Value);

        if (search?.MarkedToDelete != null)
            table = table.Where(x =>
                search.MarkedToDelete != null && x.MarkedToDeleteAt.HasValue == search.MarkedToDelete.Value);

        List<ModCaseTableEntry> tmp = [];

        foreach (var c in table.Skip(startPage * 20).Take(20))
        {
            var entry = new ModCaseTableEntry(
                c,
                publishMod ? await _discordRest.FetchUserInfo(c.ModId, true) : null,
                await _discordRest.FetchUserInfo(c.UserId, true)
            );

            if (!publishMod)
                entry.RemoveModeratorInfo();

            tmp.Add(entry);
        }

        return new ModCaseTable(tmp, table.Count());
    }
}

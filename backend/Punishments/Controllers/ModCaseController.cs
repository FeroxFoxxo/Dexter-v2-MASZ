using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Services;
using Microsoft.AspNetCore.Mvc;
using Punishments.Data;
using Punishments.DTOs;
using Punishments.Enums;
using Punishments.Exceptions;
using Punishments.Extensions;
using Punishments.Models;
using System.ComponentModel.DataAnnotations;

namespace Punishments.Controllers;

[Route("api/v1/guilds/{guildId}/cases/")]
public class ModCaseController(ModCaseRepository modCaseRepository, GuildConfigRepository guildConfigRepository,
    PunishmentConfigRepository punishmentConfigRepository, IdentityManager identityManager) : AuthenticatedController(identityManager, modCaseRepository, guildConfigRepository, punishmentConfigRepository)
{
    private readonly GuildConfigRepository _guildConfigRepository = guildConfigRepository;
    private readonly ModCaseRepository _modCaseRepository = modCaseRepository;
    private readonly PunishmentConfigRepository _punishmentConfigRepository = punishmentConfigRepository;

    [HttpGet("labels")]
    public async Task<IActionResult> Get([FromRoute] ulong guildId)
    {
        var identity = await SetupAuthentication();

        await identity.RequirePermission(DiscordPermission.Moderator, guildId);

        return Ok(await _modCaseRepository.GetLabelUsages(guildId));
    }

    [HttpGet("{caseId}")]
    public async Task<IActionResult> GetSpecificItem([FromRoute] ulong guildId, [FromRoute] int caseId)
    {
        var identity = await SetupAuthentication();

        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        await identity.RequirePermission(ApiActionPermission.View, modCase);

        var caseView = await _modCaseRepository.GetModCase(guildId, caseId);

        if ((await _guildConfigRepository.GetGuildConfig(guildId)).PublishModeratorInfo)
            return Ok(caseView);

        if (!await identity.HasPermission(DiscordPermission.Moderator, guildId))
            caseView.RemoveModeratorInfo();

        return Ok(caseView);
    }

    [HttpDelete("{caseId}")]
    public async Task<IActionResult> DeleteSpecificItem([FromRoute] ulong guildId, [FromRoute] int caseId,
        [FromQuery] bool handlePunishment = true, [FromQuery] bool forceDelete = false)
    {
        var identity = await SetupAuthentication();

        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        await identity.RequirePermission(forceDelete ? ApiActionPermission.ForceDelete : ApiActionPermission.Delete,
            modCase);

        var deletedCase =
            await _modCaseRepository.DeleteModCase(guildId, caseId, forceDelete, handlePunishment);

        return Ok(deletedCase);
    }

    [HttpPut("{caseId}")]
    public async Task<IActionResult> PutSpecificItem([FromRoute] ulong guildId, [FromRoute] int caseId,
        [FromBody] ModCaseForPutDto newValue, [FromQuery] bool handlePunishment = true)
    {
        var identity = await SetupAuthentication();

        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        await identity.RequirePermission(ApiActionPermission.Edit, modCase);

        modCase.Title = newValue.Title;
        modCase.Description = newValue.Description;
        modCase.UserId = newValue.UserId;

        if (newValue.OccurredAt.HasValue)
            modCase.OccurredAt = newValue.OccurredAt.Value;

        modCase.Labels = newValue.Labels.Distinct().ToArray();
        modCase.Others = newValue.Others;
        modCase.PunishmentType = newValue.PunishmentType;
        modCase.PunishedUntil = newValue.PunishedUntil;
        modCase.LastEditedByModId = identity.GetCurrentUser().Id;
        modCase.Severity = newValue.SeverityType;

        modCase = await _modCaseRepository.UpdateModCase(modCase, handlePunishment);

        return Ok(modCase);
    }

    [HttpGet("{userId}/finalWarn")]
    public async Task<int> GetFinalWarn([FromRoute] ulong guildId, [FromRoute] ulong userId)
    {
        await SetupAuthentication();
        var finalWarn = await _modCaseRepository.GetFinalWarn(userId, guildId);

        return finalWarn?.CaseId ?? -1;
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem([FromRoute] ulong guildId, [FromBody] ModCaseForCreateDto modCaseDto)
    {
        var identity = await SetupAuthentication();

        if (await _modCaseRepository.GetFinalWarn(modCaseDto.UserId, guildId) != null)
            throw new AlreadyFinalWarnedException();

        var modCase = new ModCase
        {
            Title = modCaseDto.Title,
            Description = modCaseDto.Description,
            GuildId = guildId,
            ModId = identity.GetCurrentUser().Id,
            UserId = modCaseDto.UserId,
            Labels = modCaseDto.Labels.Distinct().ToArray(),
            Others = modCaseDto.Others,
            Severity = modCaseDto.SeverityType
        };

        if (modCaseDto.OccurredAt.HasValue)
            modCase.OccurredAt = modCaseDto.OccurredAt.Value;

        modCase.CreationType = CaseCreationType.Default;
        modCase.PunishmentType = modCaseDto.PunishmentType;
        modCase.PunishedUntil = modCaseDto.PunishedUntil;

        if (modCaseDto.PunishmentType == PunishmentType.FinalWarn)
        {
            _punishmentConfigRepository.AsUser(identity);
            var punishmentConfig = await _punishmentConfigRepository.GetGuildPunishmentConfig(guildId);
            modCase.PunishedUntil = punishmentConfig.FinalWarnMuteTime == default
                ? null
                : DateTime.UtcNow + punishmentConfig.FinalWarnMuteTime;
        }

        await identity.RequirePermission(ApiActionPermission.Edit, modCase);

        modCase = (await _modCaseRepository.CreateModCase(modCase)).Item1;

        return StatusCode(201, modCase);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllItems([FromRoute] ulong guildId,
        [FromQuery][Range(0, int.MaxValue)] int startPage = 0)
    {
        var identity = await SetupAuthentication();

        ulong userOnly = 0;

        if (!await identity.HasPermission(DiscordPermission.Moderator, guildId))
            userOnly = identity.GetCurrentUser().Id;

        var modCases = userOnly == 0
            ? (await _modCaseRepository.GetCasePagination(guildId, startPage)).ToList()
            : [.. (await _modCaseRepository.GetCasePaginationFilteredForUser(guildId, userOnly, startPage))];
        if ((await _guildConfigRepository.GetGuildConfig(guildId)).PublishModeratorInfo)
            return Ok(modCases);

        if (await identity.HasPermission(DiscordPermission.Moderator, guildId))
            return Ok(modCases);

        foreach (var modCase in modCases)
            modCase.RemoveModeratorInfo();

        return Ok(modCases);
    }

    [HttpPost("{caseId}/lock")]
    public async Task<IActionResult> LockComments([FromRoute] ulong guildId, [FromRoute] int caseId)
    {
        var identity = await SetupAuthentication();

        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        await identity.RequirePermission(ApiActionPermission.Edit, modCase);

        modCase = await _modCaseRepository.LockCaseComments(guildId, caseId);

        return Ok(modCase);
    }

    [HttpDelete("{caseId}/lock")]
    public async Task<IActionResult> UnlockComments([FromRoute] ulong guildId, [FromRoute] int caseId)
    {
        var identity = await SetupAuthentication();

        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        await identity.RequirePermission(ApiActionPermission.Edit, modCase);

        modCase = await _modCaseRepository.UnlockCaseComments(guildId, caseId);

        return Ok(modCase);
    }

    [HttpPost("{caseId}/active")]
    public async Task<IActionResult> ActivateCase([FromRoute] ulong guildId, [FromRoute] int caseId)
    {
        var identity = await SetupAuthentication();

        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        await identity.RequirePermission(ApiActionPermission.Edit, modCase);

        modCase = await _modCaseRepository.ActivateModCase(guildId, caseId);

        return Ok(modCase);
    }

    [HttpDelete("{caseId}/active")]
    public async Task<IActionResult> DeactivateCase([FromRoute] ulong guildId, [FromRoute] int caseId)
    {
        var identity = await SetupAuthentication();

        var modCase = await _modCaseRepository.GetModCase(guildId, caseId);

        await identity.RequirePermission(ApiActionPermission.Edit, modCase);

        modCase = await _modCaseRepository.DeactivateModCase(guildId, caseId);

        return Ok(modCase);
    }
}

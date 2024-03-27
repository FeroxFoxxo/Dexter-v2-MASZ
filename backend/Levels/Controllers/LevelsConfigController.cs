using Bot.Abstractions;
using Bot.Data;
using Bot.Exceptions;
using Bot.Services;
using Discord;
using Levels.Data;
using Levels.DTOs;
using Levels.Models;
using Microsoft.AspNetCore.Mvc;

namespace Levels.Controllers;

[Route("api/v1/guilds/{guildId}/levels")]
public class LevelsConfigController(IdentityManager identityManager, GuildLevelConfigRepository levelsConfigRepository,
    GuildConfigRepository guildConfigRepository) : AuthenticatedController(identityManager, levelsConfigRepository)
{
    private readonly GuildConfigRepository _guildConfigRepository = guildConfigRepository;
    private readonly GuildLevelConfigRepository _levelsConfigRepository = levelsConfigRepository;

    [HttpGet]
    public async Task<IActionResult> GetConfig([FromRoute] ulong guildId)
    {
        try
        {
            await _guildConfigRepository.GetGuildConfig(guildId);
        }
        catch (UnregisteredGuildException e)
        {
            return NotFound(e);
        }

        var config = await _levelsConfigRepository.GetOrCreateConfig(guildId);

        var dto = new GuildLevelConfigDto
        {
            Id = guildId,
            
            Coefficients = config.Coefficients,
            XpInterval = config.XpInterval,

            MaximumTextXpGiven = config.Experience.MaximumTextXpGiven,
            MaximumVoiceXpGiven = config.Experience.MaximumVoiceXpGiven,
            MinimumTextXpGiven = config.Experience.MinimumTextXpGiven,
            MinimumVoiceXpGiven = config.Experience.MinimumVoiceXpGiven,

            VoiceXpRequiredMembers = config.VoiceXpRequiredMembers,
            VoiceXpCountMutedMembers = config.VoiceXpCountMutedMembers,

            HandleRoles = config.HandleRoles,
            NicknameDisabledRole = config.NicknameDisabledRole,
            NicknameDisabledReplacement = config.NicknameDisabledReplacement,
            Levels = config.Levels,
            LevelUpMessageOverrides = config.LevelUpMessageOverrides,
            DisabledXpChannels = config.DisabledXpChannels,

            LevelUpTemplate = config.LevelUpTemplate,
            SendTextLevelUps = config.SendTextLevelUps,
            SendVoiceLevelUps = config.SendVoiceLevelUps,
            TextLevelUpChannel = config.TextLevelUpChannel,
            VoiceLevelUpChannel = config.VoiceLevelUpChannel
        };

        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> PutConfig([FromRoute] ulong guildId, [FromBody] GuildLevelConfigDto config)
    {
        var identity = await SetupAuthentication();

        if (!identity.HasPermission(GuildPermission.Administrator, guildId) && !await identity.IsSiteAdmin())
            return Forbid();

        try
        {
            await _guildConfigRepository.GetGuildConfig(guildId);
        }
        catch (UnregisteredGuildException e)
        {
            return NotFound(e);
        }

        if (config.Id != guildId)
            return BadRequest(
                "The IDs supplied in the request route and in the object in the request body do not match.");

        var validationErrors = ValidateConfig(config);

        if (validationErrors is not null)
            return validationErrors;

        var existing = await _levelsConfigRepository.GetOrCreateConfig(guildId);

        existing.Coefficients = config.Coefficients;
        existing.XpInterval = config.XpInterval;

        existing.Experience = new ExperienceConfig() {
            MaximumTextXpGiven = config.MaximumTextXpGiven,
            MaximumVoiceXpGiven = config.MaximumVoiceXpGiven,
            MinimumTextXpGiven = config.MinimumTextXpGiven,
            MinimumVoiceXpGiven = config.MinimumVoiceXpGiven
        };
        
        existing.VoiceXpRequiredMembers = config.VoiceXpRequiredMembers;
        existing.VoiceXpCountMutedMembers = config.VoiceXpCountMutedMembers;

        existing.HandleRoles = config.HandleRoles;
        existing.NicknameDisabledRole = config.NicknameDisabledRole;
        existing.NicknameDisabledReplacement = config.NicknameDisabledReplacement;
        existing.Levels = config.Levels;
        existing.LevelUpMessageOverrides = config.LevelUpMessageOverrides;
        existing.DisabledXpChannels = config.DisabledXpChannels;

        existing.LevelUpTemplate = config.LevelUpTemplate;
        existing.SendTextLevelUps = config.SendTextLevelUps;
        existing.SendVoiceLevelUps = config.SendVoiceLevelUps;
        existing.TextLevelUpChannel = config.TextLevelUpChannel;
        existing.VoiceLevelUpChannel = config.VoiceLevelUpChannel;

        await _levelsConfigRepository.UpdateConfig(existing);
        return Ok();
    }

    private BadRequestObjectResult ValidateConfig(GuildLevelConfigDto config)
    {
        if (config.Coefficients.Length is < 2 or > 10)
            return BadRequest("Leveling coefficients must have between 2 and 10 elements!");

        config.Coefficients[0] = 0;
        for (var i = 1; i < config.Coefficients.Length; i++)
        {
            var n = config.Coefficients[i];
            if (n <= 0) return BadRequest("Found non-positive coefficients in configuration.");
        }

        if (config.XpInterval < 10)
            return BadRequest("Experience Interval cannot be lower than 10 seconds");

        if (config.MaximumTextXpGiven < config.MinimumTextXpGiven)
            return BadRequest("Invalid range for Text Experience, minimum exceeds maximum.");
        if (config.MaximumVoiceXpGiven < config.MinimumVoiceXpGiven)
            return BadRequest("Invalid range for Voice Experience, minimum exceeds maximum.");

        if (config.NicknameDisabledReplacement != default)
        {
            var found = false;
            foreach (var roleIds in config.Levels.Values)
                if (roleIds.Contains(config.NicknameDisabledReplacement))
                {
                    found = true;
                    break;
                }

            if (!found) return BadRequest("Nickname Disabled Replacement must be a level role.");
        }

        return string.IsNullOrWhiteSpace(config.LevelUpTemplate)
            ? BadRequest("Level Up Template must not be empty.")
            : config.LevelUpTemplate.Length > 250 ? BadRequest("The Length of Level Up Template may not exceed 200 characters.") : null;
    }
}

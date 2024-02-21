using Bot.Abstractions;
using Bot.Data;
using Bot.Dynamics;
using Bot.Enums;
using Bot.Exceptions;
using Bot.Extensions;
using Bot.Models;
using Bot.Services;
using Bot.Translators;
using Discord;
using Humanizer;
using Microsoft.Extensions.Logging;
using Punishments.Enums;
using Punishments.Events;
using Punishments.Exceptions;
using Punishments.Extensions;
using Punishments.Models;
using Punishments.Services;
using Punishments.Translators;
using System.Text;
using Utilities.Dynamics;

namespace Punishments.Data;

public class ModCaseRepository : Repository,
    IAddAdminStats, IMportGuildInfo, ILoopCaches, IAddChart, IAddGuildStats, IAddQuickEntrySearch,
    IAddNetworks, IWhoIsResults, IDeleteGuildData
{
    private readonly DiscordRest _discordRest;
    private readonly PunishmentEventHandler _eventHandler;
    private readonly GuildConfigRepository _guildConfigRepository;
    private readonly ILogger<ModCaseRepository> _logger;
    private readonly PunishmentDatabase _punishmentDatabase;
    private readonly PunishmentHandler _punishmentHandler;
    private readonly IServiceProvider _serviceProvider;
    private readonly SettingsRepository _settingsRepository;
    private readonly Translation _translator;

    public ModCaseRepository(DiscordRest discordRest, SettingsRepository settingsRepository,
        GuildConfigRepository guildConfigRepository, PunishmentDatabase punishmentDatabase,
        PunishmentEventHandler eventHandler, PunishmentHandler punishmentHandler,
        Translation translator, ILogger<ModCaseRepository> logger, IServiceProvider serviceProvider) : base(discordRest)
    {
        _discordRest = discordRest;
        _settingsRepository = settingsRepository;
        _guildConfigRepository = guildConfigRepository;
        _punishmentDatabase = punishmentDatabase;
        _eventHandler = eventHandler;
        _punishmentHandler = punishmentHandler;
        _translator = translator;
        _logger = logger;

        _settingsRepository.AsUser(Identity);
        _guildConfigRepository.AsUser(Identity);
        _serviceProvider = serviceProvider;
    }

    public async Task AddAdminStatistics(dynamic adminStats) => adminStats.modCases = await CountAllCases();

    public async Task AddChartData(dynamic chart, ulong guildId, DateTime since)
    {
        chart.modCases = await GetCounts(guildId, since);
        chart.punishments = await GetPunishmentCounts(guildId, since);
    }

    public async Task AddGuildStatistics(dynamic stats, ulong guildId)
    {
        stats.caseCount = await CountAllCasesForGuild(guildId);
        stats.activeCount = await CountAllPunishmentsForGuild(guildId);
        stats.activeBanCount = await CountAllActiveBansForGuild(guildId);
        stats.activeMuteCount = await CountAllActiveMutesForGuild(guildId);
    }

    public async Task AddNetworkData(dynamic network, List<string> modGuilds, ulong userId) => network.modCases =
        (await GetCasesForUser(userId)).Where(x => modGuilds.Contains(x.GuildId.ToString())).ToList();

    public async Task AddQuickSearchResults(List<IQuickSearchEntry> entries, ulong guildId, string search)
    {
        foreach (var item in await SearchCases(guildId, search))
        {
            entries.Add(new QuickSearchEntry<ModCaseExpanded>
            {
                Entry = new ModCaseExpanded(
                    item,
                    await _discordRest.FetchUserInfo(item.ModId, true),
                    await _discordRest.FetchUserInfo(item.LastEditedByModId, true),
                    await _discordRest.FetchUserInfo(item.UserId, true),
                    [],
                    null
                ),
                CreatedAt = item.CreatedAt,
                QuickSearchEntryType = QuickSearchEntryType.ModCase
            });
        }
    }

    public async Task DeleteGuildData(ulong guildId) => await _punishmentDatabase.DeleteAllModCasesForGuild(guildId);

    public async Task LoopCaches()
    {
        _logger.LogInformation("Case bin | Checking case bin and delete old cases.");

        var config = await _settingsRepository.GetAppSettings();

        foreach (var modCase in await _punishmentDatabase.SelectAllModCasesMarkedAsDeleted())
        {
            try
            {
                FilesHandler.DeleteDirectory(Path.Combine(config.AbsolutePathToFileUpload, modCase.GuildId.ToString(),
                    modCase.CaseId.ToString()));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete files directory for mod case.");
            }

            await _punishmentDatabase.DeleteSpecificModCase(modCase);
        }

        _logger.LogInformation("Case bin | Done.");
    }

    public async Task ImportGuildInfo(GuildConfig guildConfig)
    {
        _translator.SetLanguage(guildConfig);

        foreach (var modCase in (await _discordRest.GetGuildBans(guildConfig.GuildId, CacheBehavior.OnlyCache)).Select(
                     ban => new ModCase
                     {
                         Title = string.IsNullOrEmpty(ban.Reason)
                             ? _translator.Get<PunishmentTranslator>().ImportedFromExistingBans()
                             : ban.Reason,
                         Description = string.IsNullOrEmpty(ban.Reason)
                             ? _translator.Get<PunishmentTranslator>().ImportedFromExistingBans()
                             : ban.Reason,
                         GuildId = guildConfig.GuildId,
                         UserId = ban.User.Id,
                         Username = ban.User.Username,
                         Labels = [_translator.Get<PunishmentTranslator>().Imported()],
                         CreationType = CaseCreationType.Imported,
                         PunishmentType = PunishmentType.Ban,
                         PunishedUntil = null
                     }))
            await ImportModCase(modCase);
    }

    public async Task AddWhoIsInformation(EmbedBuilder embed, IGuildUser user, IInteractionContext context,
        Translation translator)
    {
        var cases = await GetCasesForGuildAndUser(context.Guild.Id, user.Id);
        var activeCases = cases.FindAll(c => c.PunishmentActive);

        if (cases.Count > 0)
        {
            StringBuilder caseInfo = new();

            var config = await _settingsRepository.GetAppSettings();

            foreach (var modCase in cases.Take(5))
            {
                caseInfo.Append($"[{modCase.CaseId} - {modCase.Title.Truncate(50)}]");
                caseInfo.Append($"({config.GetServiceUrl()}/guilds/{modCase.GuildId}/cases/{modCase.CaseId})\n");
            }

            if (cases.Count > 5)
                caseInfo.Append("...");

            embed.AddField($"{translator.Get<PunishmentTranslator>().Cases()} [{cases.Count}]", caseInfo.ToString());

            if (activeCases.Count > 0)
            {
                StringBuilder activeInfo = new();

                foreach (var modCase in activeCases.Take(5))
                {
                    activeInfo.Append($"{translator.Get<PunishmentEnumTranslator>().Enum(modCase.PunishmentType)} ");

                    if (modCase.PunishedUntil != null)
                        activeInfo.Append(
                            $"({translator.Get<BotTranslator>().Until()} {modCase.PunishedUntil.Value.ToDiscordTs()}) ");

                    activeInfo.Append($"[{modCase.CaseId} - {modCase.Title.Truncate(50)}]");
                    activeInfo.Append($"({config.GetServiceUrl()}/guilds/{modCase.GuildId}/cases/{modCase.CaseId})\n");
                }

                if (activeCases.Count > 5)
                    activeInfo.Append("...");

                embed.AddField($"{translator.Get<PunishmentTranslator>().ActivePunishments()} ({activeCases.Count})",
                    activeInfo.ToString());
            }
        }
        else
        {
            embed.AddField($"{translator.Get<PunishmentTranslator>().Cases()}",
                translator.Get<PunishmentTranslator>().NoCases());
        }
    }

    public async Task<ModCase> GetFinalWarn(ulong userId, ulong guildId) =>
        await _punishmentDatabase.GetFinalWarn(userId, guildId);

    public async Task<ModCase> ImportModCase(ModCase modCase)
    {
        try
        {
            modCase.CreationType = CaseCreationType.Imported;
            modCase.CaseId = await _punishmentDatabase.GetHighestCaseIdForGuild(modCase.GuildId) + 1;
            modCase.CreatedAt = DateTime.UtcNow;

            if (modCase.OccurredAt == default || modCase.OccurredAt == DateTime.MinValue)
                modCase.OccurredAt = modCase.CreatedAt;

            modCase.ModId = Identity.Id;
            modCase.LastEditedAt = modCase.CreatedAt;
            modCase.LastEditedByModId = Identity.Id;

            modCase.Labels = modCase.Labels != null ? modCase.Labels.Distinct().ToArray() : [];

            modCase.Valid = true;

            if (modCase.PunishmentType is PunishmentType.Warn or PunishmentType.Kick)
            {
                modCase.PunishedUntil = null;
                modCase.PunishmentActive = false;
            }
            else
            {
                modCase.PunishmentActive = modCase.PunishedUntil == null || modCase.PunishedUntil > DateTime.UtcNow;
            }

            await _punishmentDatabase.SaveModCase(modCase);

            return modCase;
        }
        catch (ResourceNotFoundException)
        {
            throw new UnregisteredGuildException(modCase.GuildId);
        }
    }

    public async Task<List<ModCaseLabel>> GetLabelUsages(ulong guildId)
    {
        var labels = await _punishmentDatabase.GetAllLabels(guildId);

        var countMap = new Dictionary<string, int>();

        foreach (var label in labels)
        {
            countMap[label] = countMap.TryGetValue(label, out var value) ? ++value : 1;
        }

        List<ModCaseLabel> result = [];
        foreach (var label in countMap.Keys)
        {
            result.Add(new ModCaseLabel
            {
                Label = label,
                Count = countMap[label]
            });
        }

        return [.. result.OrderByDescending(x => x.Count)];
    }

    public async Task<Tuple<ModCase, AnnouncementResult, bool>> CreateModCase(ModCase modCase)
    {
        var currentReportedUser = await _discordRest.FetchUserInfo(modCase.UserId, false);

        try
        {
            var guildConfig = await _guildConfigRepository.GetGuildConfig(modCase.GuildId);

            if (currentReportedUser == null)
            {
                _logger.LogError("Failed to fetch mod case suspect.");

                throw new InvalidIUserException(modCase.ModId);
            }

            if (currentReportedUser.IsBot)
            {
                _logger.LogError("Cannot create cases for bots.");

                throw new ProtectedModCaseSuspectException("Cannot create cases for bots.", modCase)
                    .WithError(ApiError.ProtectedModCaseSuspectIsBot);
            }

            var config = await _settingsRepository.GetAppSettings();

            if (config.SiteAdmins.Contains(currentReportedUser.Id))
            {
                _logger.LogInformation("Cannot create cases for site admins.");

                throw new ProtectedModCaseSuspectException("Cannot create cases for site admins.", modCase)
                    .WithError(ApiError.ProtectedModCaseSuspectIsSiteAdmin);
            }

            modCase.Username = currentReportedUser.Username;

            var reportedGuildUser =
                _discordRest.FetchGuildUserInfo(modCase.GuildId, modCase.UserId, CacheBehavior.IgnoreButCacheOnError);

            if (reportedGuildUser != null)
            {
                if (reportedGuildUser.RoleIds.Any(x => guildConfig.ModRoles.Contains(x)) ||
                    reportedGuildUser.RoleIds.Any(x => guildConfig.AdminRoles.Contains(x)))
                {
                    _logger.LogInformation("Cannot create cases for team members.");

                    throw new ProtectedModCaseSuspectException("Cannot create cases for team members.", modCase)
                        .WithError(ApiError.ProtectedModCaseSuspectIsTeam);
                }

                modCase.Nickname = reportedGuildUser.Nickname;
            }

            modCase.CaseId = await _punishmentDatabase.GetHighestCaseIdForGuild(modCase.GuildId) + 1;
            modCase.CreatedAt = DateTime.UtcNow;

            if (modCase.OccurredAt == default || modCase.OccurredAt == DateTime.MinValue)
                modCase.OccurredAt = modCase.CreatedAt;

            modCase.ModId = Identity.Id;
            modCase.LastEditedAt = modCase.CreatedAt;
            modCase.LastEditedByModId = Identity.Id;

            modCase.Labels = modCase.Labels != null ? modCase.Labels.Distinct().ToArray() : [];

            modCase.Valid = true;

            if (modCase.PunishmentType is PunishmentType.Warn or PunishmentType.Kick)
            {
                modCase.PunishedUntil = null;
                modCase.PunishmentActive = false;
            }
            else
            {
                modCase.PunishmentActive = modCase.PunishedUntil == null || modCase.PunishedUntil > DateTime.UtcNow;
            }

            await _punishmentDatabase.SaveModCase(modCase);

            AnnouncementResult announceResult;

            _logger.LogInformation(
                $"Sending dm notification to {modCase.UserId} for case {modCase.GuildId}/{modCase.CaseId}");

            var guild = _discordRest.FetchGuildInfo(modCase.GuildId, CacheBehavior.Default);

            try
            {
                var message = string.Empty;

                var modCaseUrl = $"{config.GetServiceUrl()}/guilds/{modCase.GuildId}/cases/{modCase.CaseId}";
                var reason = $"**{modCase.Title}**";

                if (modCase.Title != modCase.Description)
                    reason = $"**{modCase.Title}:** {modCase.Description}";

                _translator.SetLanguage(guildConfig);

                switch (modCase.PunishmentType)
                {
                    case PunishmentType.Mute:
                        message = modCase.PunishedUntil.HasValue
                            ? _translator.Get<PunishmentNotificationTranslator>()
                                .NotificationModCaseDmMuteTemp(modCase, guild, reason, modCaseUrl)
                            : _translator.Get<PunishmentNotificationTranslator>()
                                .NotificationModCaseDmMutePerm(guild, reason, modCaseUrl);
                        break;
                    case PunishmentType.Kick:
                        message = _translator.Get<PunishmentNotificationTranslator>()
                            .NotificationModCaseDmKick(guild, reason, modCaseUrl);
                        break;
                    case PunishmentType.Ban:
                        message = modCase.PunishedUntil.HasValue
                            ? _translator.Get<PunishmentNotificationTranslator>()
                                .NotificationModCaseDmBanTemp(modCase, guild, reason, modCaseUrl)
                            : _translator.Get<PunishmentNotificationTranslator>()
                                .NotificationModCaseDmBanPerm(guild, reason, modCaseUrl);
                        break;
                    case PunishmentType.Warn:
                        message = _translator.Get<PunishmentNotificationTranslator>()
                            .NotificationModCaseDmWarn(guild, reason, modCaseUrl);
                        break;
                    case PunishmentType.FinalWarn:
                        message = _translator.Get<PunishmentNotificationTranslator>()
                            .NotificationModCaseDmFinalWarn(guild, reason, modCaseUrl);
                        break;
                }

                await _discordRest.SendDmMessage(modCase.UserId, message);

                announceResult = AnnouncementResult.Announced;
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"Error while announcing mod case {modCase.GuildId}/{modCase.CaseId} in DMs to {modCase.UserId}.");

                announceResult = AnnouncementResult.Failed;
            }

            _eventHandler.ModCaseCreatedEvent.Invoke(modCase, Identity, announceResult);

            var finalWarning = await GetFinalWarn(modCase.UserId, modCase.GuildId);

            var isFinalWarned = finalWarning != null;

            if (isFinalWarned)
            {
                var (embed, buttons) = await modCase.CreateNewModCaseEmbed(guildConfig, config, announceResult,
                    _discordRest, _serviceProvider);

                var textChannel = await guild.GetTextChannelAsync(guildConfig.StaffAnnouncements);

                var title = embed.Title;

                embed.WithTitle("Final warned user has triggered an infraction")
                    .WithColor(Color.Red);

                if (title != embed.Description)
                    embed.AddField("Title", title);

                buttons.WithButton("View Final Warn", style: ButtonStyle.Link,
                    url: $"{config.GetServiceUrl()}/guilds/{finalWarning.GuildId}/cases/{finalWarning.CaseId}");

                await textChannel.SendMessageAsync(embed: embed.Build(), components: buttons.Build());
            }

            if (!modCase.PunishmentActive && modCase.PunishmentType != PunishmentType.Kick)
                return new Tuple<ModCase, AnnouncementResult, bool>(modCase, announceResult, isFinalWarned);

            if (modCase.PunishedUntil == null || modCase.PunishedUntil > DateTime.UtcNow)
                await _punishmentHandler.ModifyPunishment(modCase, RestAction.Created);

            return new Tuple<ModCase, AnnouncementResult, bool>(modCase, announceResult, isFinalWarned);
        }
        catch (ResourceNotFoundException)
        {
            throw new UnregisteredGuildException(modCase.GuildId);
        }
    }

    public async Task<ModCase> GetModCase(ulong guildId, int caseId)
    {
        var modCase = await _punishmentDatabase.SelectSpecificModCase(guildId, caseId);

        return modCase ?? throw new ResourceNotFoundException($"Mod case with id {caseId} does not exist.");
    }

    public async Task<ModCase> DeleteModCase(ulong guildId, int caseId, bool forceDelete = false,
        bool handlePunishment = true)
    {
        var modCase = await GetModCase(guildId, caseId);

        if (forceDelete)
        {
            try
            {
                var config = await _settingsRepository.GetAppSettings();
                FilesHandler.DeleteDirectory(Path.Combine(config.AbsolutePathToFileUpload, guildId.ToString(),
                    caseId.ToString()));
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to delete files directory for mod case {guildId}/{caseId}.");
            }

            _logger.LogInformation($"Force deleting modCase {guildId}/{caseId}.");
            await _punishmentDatabase.DeleteSpecificModCase(modCase);

            _eventHandler.ModCaseDeletedEvent.Invoke(modCase, Identity);
        }
        else
        {
            modCase.MarkedToDeleteAt = DateTime.UtcNow.AddDays(7);
            modCase.DeletedByUserId = Identity.Id;
            modCase.PunishmentActive = false;

            _logger.LogInformation($"Marking mod case {guildId}/{caseId} as deleted.");
            await _punishmentDatabase.UpdateModCase(modCase);

            _eventHandler.ModCaseMarkedToBeDeletedEvent.Invoke(modCase, Identity);
        }

        if (!handlePunishment) return modCase;

        try
        {
            _logger.LogInformation($"Handling punishment for case {guildId}/{caseId}.");
            await _punishmentHandler.ModifyPunishment(modCase, RestAction.Deleted);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to handle punishment for modcase {guildId}/{caseId}.");
        }

        return modCase;
    }

    public async Task<ModCase> UpdateModCase(ModCase modCase, bool handlePunishment)
    {
        var currentReportedUser = await _discordRest.FetchUserInfo(modCase.UserId, false);

        try
        {
            var guildConfig = await _guildConfigRepository.GetGuildConfig(modCase.GuildId);

            if (currentReportedUser == null)
            {
                _logger.LogError("Failed to fetch mod case suspect.");

                throw new InvalidIUserException(modCase.ModId);
            }

            if (currentReportedUser.IsBot)
            {
                _logger.LogError("Cannot edit cases for bots.");

                throw new ProtectedModCaseSuspectException("Cannot edit cases for bots.", modCase).WithError(
                    ApiError.ProtectedModCaseSuspectIsBot);
            }

            var config = await _settingsRepository.GetAppSettings();

            if (config.SiteAdmins.Contains(currentReportedUser.Id))
            {
                _logger.LogInformation("Cannot edit cases for site admins.");

                throw new ProtectedModCaseSuspectException("Cannot edit cases for site admins.", modCase).WithError(
                    ApiError.ProtectedModCaseSuspectIsSiteAdmin);
            }

            modCase.Username = currentReportedUser.Username;

            var reportedGuildUser =
                _discordRest.FetchGuildUserInfo(modCase.GuildId, modCase.UserId, CacheBehavior.IgnoreButCacheOnError);

            if (reportedGuildUser != null)
            {
                if (reportedGuildUser.RoleIds.Any(x => guildConfig.ModRoles.Contains(x)) ||
                    reportedGuildUser.RoleIds.Any(x => guildConfig.AdminRoles.Contains(x)))
                {
                    _logger.LogInformation("Cannot create cases for team Users.");

                    throw new ProtectedModCaseSuspectException("Cannot create cases for team Users.", modCase)
                        .WithError(ApiError.ProtectedModCaseSuspectIsTeam);
                }

                modCase.Nickname = reportedGuildUser.Nickname;
            }

            modCase.LastEditedAt = DateTime.UtcNow;
            modCase.LastEditedByModId = Identity.Id;
            modCase.Valid = true;

            if (modCase.PunishmentType is PunishmentType.Warn or PunishmentType.Kick)
            {
                modCase.PunishedUntil = null;
                modCase.PunishmentActive = false;
            }
            else
            {
                modCase.PunishmentActive = modCase.PunishedUntil == null || modCase.PunishedUntil > DateTime.UtcNow;
            }

            await _punishmentDatabase.UpdateModCase(modCase);

            _eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity);

            if (!handlePunishment || !modCase.PunishmentActive && modCase.PunishmentType != PunishmentType.Kick)
                return modCase;

            if (modCase.PunishedUntil == null || modCase.PunishedUntil > DateTime.UtcNow)
                await _punishmentHandler.ModifyPunishment(modCase, RestAction.Created);

            return modCase;
        }
        catch (ResourceNotFoundException)
        {
            throw new UnregisteredGuildException(modCase.GuildId);
        }
    }

    public async Task<List<ModCase>> GetCasePagination(ulong guildId, int startPage = 1, int pageSize = 20) =>
        await _punishmentDatabase.SelectAllModCasesForGuild(guildId, startPage, pageSize);

    public async Task<List<ModCase>> GetCasePaginationFilteredForUser(ulong guildId, ulong userId, int startPage = 1,
        int pageSize = 20) =>
        await _punishmentDatabase.SelectAllModCasesForSpecificUserOnGuild(guildId, userId, startPage,
            pageSize);

    public async Task<List<ModCase>> GetCasesForUser(ulong userId) =>
        await _punishmentDatabase.SelectAllModCasesForSpecificUser(userId);

    public async Task<List<ModCase>> GetCasesForGuild(ulong guildId) =>
        await _punishmentDatabase.SelectAllModCasesForGuild(guildId);

    public async Task<List<ModCase>> GetCasesForGuildAndUser(ulong guildId, ulong userId) =>
        await _punishmentDatabase.SelectAllModCasesForSpecificUserOnGuild(guildId, userId);

    public async Task<int> CountAllCases() => await _punishmentDatabase.CountAllModCases();

    public async Task<int> CountAllCasesForGuild(ulong guildId) =>
        await _punishmentDatabase.CountAllModCasesForGuild(guildId);

    public async Task<int> CountAllPunishmentsForGuild(ulong guildId) =>
        await _punishmentDatabase.CountAllActivePunishmentsForGuild(guildId);

    public async Task<int> CountAllActiveMutesForGuild(ulong guildId) =>
        await _punishmentDatabase.CountAllActivePunishmentsForGuild(guildId, PunishmentType.Mute);

    public async Task<int> CountAllActiveBansForGuild(ulong guildId) =>
        await _punishmentDatabase.CountAllActivePunishmentsForGuild(guildId, PunishmentType.Ban);

    public async Task<List<ModCase>> SearchCases(ulong guildId, string searchString)
    {
        var modCases = await _punishmentDatabase.SelectAllModCasesForGuild(guildId);
        var filteredModCases = new List<ModCase>();

        foreach (var c in modCases)
        {
            var entry = new ModCaseTableEntry(
                c,
                await _discordRest.FetchUserInfo(c.ModId, true),
                await _discordRest.FetchUserInfo(c.UserId, true)
            );

            if (searchString.Search(entry, _translator))
                filteredModCases.Add(c);
        }

        return filteredModCases;
    }

    public async Task<ModCase> LockCaseComments(ulong guildId, int caseId)
    {
        var modCase = await GetModCase(guildId, caseId);
        modCase.AllowComments = false;
        modCase.LockedAt = DateTime.UtcNow;
        modCase.LockedByUserId = Identity.Id;

        await _punishmentDatabase.UpdateModCase(modCase);

        _eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity);

        return modCase;
    }

    public async Task<ModCase> UnlockCaseComments(ulong guildId, int caseId)
    {
        var modCase = await GetModCase(guildId, caseId);
        modCase.AllowComments = true;
        modCase.LockedAt = null;
        modCase.LockedByUserId = 0;

        await _punishmentDatabase.UpdateModCase(modCase);

        _eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity);

        return modCase;
    }

    public async Task<ModCase> RestoreCase(ulong guildId, int caseId)
    {
        var modCase = await GetModCase(guildId, caseId);
        modCase.MarkedToDeleteAt = null;
        modCase.DeletedByUserId = 0;

        await _punishmentDatabase.UpdateModCase(modCase);

        _eventHandler.ModCaseRestoredEvent.Invoke(modCase);

        try
        {
            _logger.LogInformation($"Handling punishment for case {guildId}/{caseId}.");

            await _punishmentHandler.ModifyPunishment(modCase, RestAction.Created);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to handle punishment for mod case {guildId}/{caseId}.");
        }

        return modCase;
    }

    public async Task<List<DbCount>> GetCounts(ulong guildId, DateTime since) =>
        await _punishmentDatabase.GetCaseCountGraph(guildId, since);

    public async Task<List<DbCount>> GetPunishmentCounts(ulong guildId, DateTime since) =>
        await _punishmentDatabase.GetPunishmentCountGraph(guildId, since);

    public async Task<ModCase> ActivateModCase(ulong guildId, int caseId)
    {
        var modCase = await GetModCase(guildId, caseId);
        modCase.PunishmentActive = true;
        modCase.LastEditedAt = DateTime.UtcNow;
        modCase.LastEditedByModId = Identity.Id;

        await _punishmentDatabase.UpdateModCase(modCase);

        _eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity);

        try
        {
            _logger.LogInformation($"Handling punishment for case {guildId}/{caseId}.");

            await _punishmentHandler.ModifyPunishment(modCase, RestAction.Created);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to handle punishment for mod case {guildId}/{caseId}.");
        }

        return modCase;
    }

    public async Task<ModCase> DeactivateModCase(ulong guildId, int caseId)
    {
        var modCase = await GetModCase(guildId, caseId);
        modCase.PunishmentActive = false;
        modCase.LastEditedAt = DateTime.UtcNow;
        modCase.LastEditedByModId = Identity.Id;

        await _punishmentDatabase.UpdateModCase(modCase);

        _eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity);

        try
        {
            _logger.LogInformation($"Handling punishment for case {guildId}/{caseId}.");
            await _punishmentHandler.ModifyPunishment(modCase, RestAction.Deleted);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to handle punishment for mod case {guildId}/{caseId}.");
        }

        return modCase;
    }

    public async Task DeactivateModCase(params ModCase[] modCases)
    {
        foreach (var modCase in modCases)
        {
            modCase.PunishmentActive = false;
            modCase.LastEditedAt = DateTime.UtcNow;
            modCase.LastEditedByModId = Identity.Id;

            await _punishmentDatabase.UpdateModCase(modCase);

            _eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity);

            try
            {
                _logger.LogInformation($"Handling punishment for case {modCase.GuildId}/{modCase.CaseId}.");
                await _punishmentHandler.ModifyPunishment(modCase, RestAction.Deleted);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to handle punishment for mod case {modCase.GuildId}/{modCase.CaseId}.");
            }
        }
    }
}

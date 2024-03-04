using AutoMods.Enums;
using AutoMods.Events;
using AutoMods.Extensions;
using AutoMods.Models;
using AutoMods.Translators;
using Bot.Abstractions;
using Bot.Data;
using Bot.Dynamics;
using Bot.Enums;
using Bot.Extensions;
using Bot.Models;
using Bot.Services;
using Bot.Translators;
using Discord;
using Microsoft.Extensions.Logging;
using Punishments.Data;
using Punishments.Enums;
using Punishments.Models;
using System.Text;

namespace AutoMods.Data;

public class AutoModEventRepository : Repository,
    IAddAdminStats, IAddChart, IAddGuildStats, IAddQuickEntrySearch, IAddNetworks, IDeleteGuildData
{
    private readonly AutoModConfigRepository _autoModConfigRepo;
    private readonly AutoModDatabase _autoModDatabase;
    private readonly DiscordRest _discordRest;
    private readonly AutoModEventHandler _eventHandler;
    private readonly GuildConfigRepository _guildConfigRepo;
    private readonly ILogger<AutoModConfigRepository> _logger;
    private readonly ModCaseRepository _modCaseRepo;
    private readonly Translation _translator;

    public AutoModEventRepository(AutoModDatabase autoModDatabase, AutoModEventHandler eventHandler,
        GuildConfigRepository guildConfigRepo, AutoModConfigRepository autoModConfigRepo, Translation translator,
        DiscordRest discordRest, ILogger<AutoModConfigRepository> logger, ModCaseRepository modCaseRepo) : base(
        discordRest)
    {
        _autoModDatabase = autoModDatabase;
        _eventHandler = eventHandler;
        _guildConfigRepo = guildConfigRepo;
        _autoModConfigRepo = autoModConfigRepo;
        _translator = translator;
        _discordRest = discordRest;
        _logger = logger;
        _modCaseRepo = modCaseRepo;

        _guildConfigRepo.AsUser(Identity);
        _autoModConfigRepo.AsUser(Identity);
        _modCaseRepo.AsUser(Identity);
    }

    public async Task AddAdminStatistics(dynamic adminStats) => adminStats.autoModEvents = await CountEvents();

    public async Task AddChartData(dynamic chart, ulong guildId, DateTime since) =>
        chart.autoMods = await GetCounts(guildId, since);

    public async Task AddGuildStatistics(dynamic stats, ulong guildId) =>
        stats.autoModCount = await CountEventsByGuild(guildId);

    public async Task AddNetworkData(dynamic network, List<string> modGuilds, ulong userId) => network.modEvents =
        (await GetAllEventsForUser(userId)).Where(x => modGuilds.Contains(x.GuildId.ToString())).ToList();

    public async Task AddQuickSearchResults(List<IQuickSearchEntry> entries, ulong guildId, string search)
    {
        foreach (var item in await SearchInGuild(guildId, search))
        {
            entries.Add(new QuickSearchEntry<AutoModEventExpanded>
            {
                Entry = new AutoModEventExpanded(
                    item,
                    await _discordRest.FetchUserInfo(item.UserId, true)
                ),
                CreatedAt = item.CreatedAt,
                QuickSearchEntryType = QuickSearchEntryType.AutoMod
            });
        }
    }


    public async Task DeleteGuildData(ulong guildId) =>
        await _autoModDatabase.DeleteAllPunishmentsEventsForGuild(guildId);

    public async Task<int> CountEvents() => await _autoModDatabase.CountAllPunishmentsEvents();

    public async Task<int> CountEventsByGuild(ulong guildId) =>
        await _autoModDatabase.CountAllPunishmentsEventsForGuild(guildId);

    public async Task<int> CountEventsByGuildAndUser(ulong guildId, ulong userId) =>
        await _autoModDatabase.CountAllPunishmentsEventsForSpecificUserOnGuild(guildId, userId);

    public async Task<AutoModEvent> RegisterEvent(AutoModEvent modEvent, ITextChannel channel, IUser author)
    {
        var guildConfig = await _guildConfigRepo.GetGuildConfig(modEvent.GuildId);

        _translator.SetLanguage(guildConfig);

        var modConfig = await _autoModConfigRepo.GetConfigsByGuildAndType(modEvent.GuildId, modEvent.AutoModType);

        var user = await _discordRest.FetchUserInfo(modEvent.UserId, false);

        if (user != null)
            modEvent.Username = user.Username;

        modEvent.CreatedAt = DateTime.UtcNow;

        if (modConfig.AutoModAction is AutoModAction.CaseCreated or AutoModAction.ContentDeletedAndCaseCreated)
        {
            ModCase modCase = new()
            {
                Title = $"{_translator.Get<AutoModTranslator>().AutoMod()}: " +
                        _translator.Get<AutoModEnumTranslator>().Enum(modEvent.AutoModType)
            };

            StringBuilder description = new();

            description.AppendLine(_translator.Get<AutoModNotificationTranslator>().NotificationAutoModCase(user));

            description.AppendLine($"{_translator.Get<BotTranslator>().Type()}: " +
                                   _translator.Get<AutoModEnumTranslator>().Enum(modEvent.AutoModType));

            description.AppendLine($"{_translator.Get<BotTranslator>().Action()}: " +
                                   _translator.Get<AutoModEnumTranslator>().Enum(modEvent.AutoModAction));

            description.AppendLine($"{_translator.Get<BotTranslator>().Message()}: " +
                                   modEvent.MessageId);

            description.AppendLine($"{_translator.Get<BotTranslator>().MessageContent()}: " +
                                   modEvent.MessageContent);

            modCase.Description = description.ToString();

            modCase.Labels = ["Automod", modEvent.AutoModType.ToString()];
            modCase.CreationType = CaseCreationType.AutoMod;
            modCase.PunishmentType = PunishmentType.Warn;
            modCase.PunishedUntil = null;

            if (modConfig.PunishmentType != null)
                modCase.PunishmentType = modConfig.PunishmentType.Value;

            if (modConfig.PunishmentDurationMinutes != null)
                modCase.PunishedUntil = DateTime.UtcNow.AddMinutes(modConfig.PunishmentDurationMinutes.Value);

            modCase.UserId = modEvent.UserId;
            modCase.GuildId = modEvent.GuildId;

            try
            {
                modCase = (await _modCaseRepo.CreateModCase(modCase)).Item1;

                modEvent.AssociatedCaseId = modCase.CaseId;
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    $"Failed to create mod-case for mod event {modEvent.GuildId}/{modEvent.UserId}/{modEvent.AutoModType}");
            }
        }

        await _autoModDatabase.SavePunishmentsEvent(modEvent);

        _eventHandler.AutoModEventRegisteredEvent.Invoke(modEvent, modConfig, guildConfig, channel, author);

        return modEvent;
    }

    public async Task<List<AutoModEvent>> GetPagination(ulong guildId, int startPage = 1, int pageSize = 20) =>
        await _autoModDatabase.SelectAllPunishmentsEventsForGuild(guildId, startPage, pageSize);

    public async Task<List<AutoModEvent>> GetPaginationFilteredForUser(ulong guildId, ulong userId, int startPage = 1,
        int pageSize = 20) =>
        await _autoModDatabase.SelectAllPunishmentsEventsForSpecificUserOnGuild(guildId, userId, startPage,
            pageSize);

    public async Task<List<AutoModEvent>> GetAllEventsForUser(ulong userId) =>
        await _autoModDatabase.SelectAllPunishmentsEventsForSpecificUser(userId);

    public async Task<List<AutoModEvent>> GetAllEventsForUserSinceMinutes(ulong userId, int minutes) =>
        await _autoModDatabase.SelectAllPunishmentsEventsForSpecificUser(userId, minutes);

    public async Task<List<DbCount>> GetCounts(ulong guildId, DateTime since) =>
        await _autoModDatabase.GetPunishmentsCountGraph(guildId, since);

    public async Task<List<AutoModTypeSplit>> GetCountsByType(ulong guildId, DateTime since) =>
        await _autoModDatabase.GetPunishmentsSplitGraph(guildId, since);

    public async Task<List<AutoModEvent>> SearchInGuild(ulong guildId, string searchString)
    {
        var events = await _autoModDatabase.SelectAllPunishmentsEventsForGuild(guildId);
        List<AutoModEvent> filteredEvents = [];

        foreach (var c in events)
        {
            var entry = new AutoModEventExpanded(
                c,
                await _discordRest.FetchUserInfo(c.UserId, true)
            );

            if (searchString.Search(entry))
                filteredEvents.Add(c);
        }

        return filteredEvents;
    }
}

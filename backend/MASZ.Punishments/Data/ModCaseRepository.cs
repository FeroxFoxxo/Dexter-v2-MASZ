using System.Text;
using Discord;
using MASZ.Bot.Abstractions;
using MASZ.Bot.Data;
using MASZ.Bot.Dynamics;
using MASZ.Bot.Enums;
using MASZ.Bot.Exceptions;
using MASZ.Bot.Extensions;
using MASZ.Bot.Models;
using MASZ.Bot.Services;
using MASZ.Bot.Translators;
using MASZ.Bot.Views;
using MASZ.Punishments.Enums;
using MASZ.Punishments.Events;
using MASZ.Punishments.Exceptions;
using MASZ.Punishments.Extensions;
using MASZ.Punishments.Models;
using MASZ.Punishments.Services;
using MASZ.Punishments.Translators;
using MASZ.Punishments.Views;
using MASZ.Utilities.Dynamics;
using Microsoft.Extensions.Logging;

namespace MASZ.Punishments.Data;

public class ModCaseRepository : Repository,
	AddAdminStats, ImportGuildInfo, LoopCaches, CacheUsers, AddChart, AddGuildStats, AddQuickEntrySearch,
	AddNetworks, WhoIsResults
{
	private readonly DiscordRest _discordRest;
	private readonly PunishmentEventHandler _eventHandler;
	private readonly GuildConfigRepository _guildConfigRepository;
	private readonly ILogger<ModCaseRepository> _logger;
	private readonly PunishmentDatabase _punishmentDatabase;
	private readonly PunishmentHandler _punishmentHandler;
	private readonly SettingsRepository _settingsRepository;
	private readonly Translation _translator;

	public ModCaseRepository(DiscordRest discordRest, SettingsRepository settingsRepository,
		GuildConfigRepository guildConfigRepository, PunishmentDatabase punishmentDatabase,
		PunishmentEventHandler eventHandler, PunishmentHandler punishmentHandler,
		Translation translator, ILogger<ModCaseRepository> logger) : base(discordRest)
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
	}

	public async Task AddAdminStatistics(dynamic adminStats)
	{
		adminStats.modCases = await CountAllCases();
	}

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

	public async Task AddNetworkData(dynamic network, List<string> modGuilds, ulong userId)
	{
		network.modCases = (await GetCasesForUser(userId)).Where(x => modGuilds.Contains(x.GuildId.ToString()))
			.Select(x => new CaseView(x)).ToList();
	}

	public async Task AddQuickSearchResults(List<QuickSearchEntry> entries, ulong guildId, string search)
	{
		foreach (var item in await SearchCases(guildId, search))
			entries.Add(new QuickSearchEntry<CaseExpandedView>
			{
				Entry = new CaseExpandedView(
					item,
					await _discordRest.FetchUserInfo(item.ModId, CacheBehavior.OnlyCache),
					await _discordRest.FetchUserInfo(item.LastEditedByModId, CacheBehavior.OnlyCache),
					await _discordRest.FetchUserInfo(item.UserId, CacheBehavior.OnlyCache),
					new List<CommentExpandedView>(),
					null
				),
				CreatedAt = item.CreatedAt,
				QuickSearchEntryType = QuickSearchEntryType.ModCase
			});
	}

	public async Task CacheKnownUsers(List<ulong> handledUsers)
	{
		foreach (var modCase in await _punishmentDatabase.SelectLatestModCases(DateTime.UtcNow.AddYears(-3), 750))
		{
			if (!handledUsers.Contains(modCase.UserId))
			{
				await _discordRest.FetchUserInfo(modCase.UserId, CacheBehavior.IgnoreCache);
				handledUsers.Add(modCase.UserId);
			}

			if (!handledUsers.Contains(modCase.ModId))
			{
				await _discordRest.FetchUserInfo(modCase.ModId, CacheBehavior.IgnoreCache);
				handledUsers.Add(modCase.ModId);
			}

			if (handledUsers.Contains(modCase.LastEditedByModId))
				continue;

			await _discordRest.FetchUserInfo(modCase.LastEditedByModId, CacheBehavior.IgnoreCache);
			handledUsers.Add(modCase.LastEditedByModId);
		}
	}

	public async Task ImportGuildInfo(GuildConfig guildConfig)
	{
		_translator.SetLanguage(guildConfig);

		foreach (var modCase in (await _discordRest.GetGuildBans(guildConfig.GuildId, CacheBehavior.OnlyCache)).Select(ban => new ModCase
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
			         Labels = new[] { _translator.Get<PunishmentTranslator>().Imported() },
			         Discriminator = ban.User.Discriminator,
			         CreationType = CaseCreationType.Imported,
			         PunishmentType = PunishmentType.Ban,
			         PunishedUntil = null
		         }))
		{
			await ImportModCase(modCase);
		}
	}

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
				caseInfo.Append($"({config.ServiceBaseUrl}/guilds/{modCase.GuildId}/cases/{modCase.CaseId})\n");
			}

			if (cases.Count > 5)
				caseInfo.Append("[...]");

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
					activeInfo.Append($"({config.ServiceBaseUrl}/guilds/{modCase.GuildId}/cases/{modCase.CaseId})\n");
				}

				if (activeCases.Count > 5)
					activeInfo.Append("[...]");

				embed.AddField($"{translator.Get<PunishmentTranslator>().ActivePunishments()} [{activeCases.Count}]",
					activeInfo.ToString());
			}
		}
		else
		{
			embed.AddField($"{translator.Get<PunishmentTranslator>().Cases()} [0]",
				translator.Get<PunishmentTranslator>().NoCases());
		}
	}

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

			modCase.Labels = modCase.Labels != null ? modCase.Labels.Distinct().ToArray() : Array.Empty<string>();

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

	public async Task<List<LabelUsage>> GetLabelUsages(ulong guildId)
	{
		var labels = await _punishmentDatabase.GetAllLabels(guildId);

		var countMap = new Dictionary<string, int>();

		foreach (var label in labels)
			if (countMap.ContainsKey(label))
				countMap[label]++;
			else
				countMap[label] = 1;

		List<LabelUsage> result = new();
		foreach (var label in countMap.Keys)
			result.Add(new LabelUsage()
			{
				Label = label,
				Count = countMap[label]
			});

		return result.OrderByDescending(x => x.Count).ToList();
	}

	public async Task<ModCase> CreateModCase(ModCase modCase, bool handlePunishment, bool sendPublicNotification,
		bool sendDmNotification)
	{
		var currentReportedUser = await _discordRest.FetchUserInfo(modCase.UserId, CacheBehavior.IgnoreButCacheOnError);

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
			modCase.Discriminator = currentReportedUser.Discriminator;

			var currentReportedMember =
				await _discordRest.FetchUserInfo(modCase.GuildId, modCase.UserId, CacheBehavior.IgnoreButCacheOnError);

			if (currentReportedMember != null)
			{
				if (currentReportedMember.RoleIds.Any(x => guildConfig.ModRoles.Contains(x)) ||
				    currentReportedMember.RoleIds.Any(x => guildConfig.AdminRoles.Contains(x)))
				{
					_logger.LogInformation("Cannot create cases for team members.");

					throw new ProtectedModCaseSuspectException("Cannot create cases for team members.", modCase)
						.WithError(ApiError.ProtectedModCaseSuspectIsTeam);
				}

				modCase.Nickname = currentReportedMember.Nickname;
			}

			modCase.CaseId = await _punishmentDatabase.GetHighestCaseIdForGuild(modCase.GuildId) + 1;
			modCase.CreatedAt = DateTime.UtcNow;

			if (modCase.OccurredAt == default || modCase.OccurredAt == DateTime.MinValue)
				modCase.OccurredAt = modCase.CreatedAt;

			modCase.ModId = Identity.Id;
			modCase.LastEditedAt = modCase.CreatedAt;
			modCase.LastEditedByModId = Identity.Id;

			modCase.Labels = modCase.Labels != null ? modCase.Labels.Distinct().ToArray() : Array.Empty<string>();

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

			_eventHandler.ModCaseCreatedEvent.Invoke(modCase, Identity, sendPublicNotification, sendDmNotification);

			if (!handlePunishment || (!modCase.PunishmentActive && modCase.PunishmentType != PunishmentType.Kick))
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

	public async Task<ModCase> GetModCase(ulong guildId, int caseId)
	{
		var modCase = await _punishmentDatabase.SelectSpecificModCase(guildId, caseId);

		if (modCase == null)
			throw new ResourceNotFoundException($"Mod case with id {caseId} does not exist.");

		return modCase;
	}

	public async Task<ModCase> DeleteModCase(ulong guildId, int caseId, bool forceDelete = false,
		bool handlePunishment = true, bool announcePublic = true)
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

			_eventHandler.ModCaseDeletedEvent.Invoke(modCase, Identity, announcePublic, false);
		}
		else
		{
			modCase.MarkedToDeleteAt = DateTime.UtcNow.AddDays(7);
			modCase.DeletedByUserId = Identity.Id;
			modCase.PunishmentActive = false;

			_logger.LogInformation($"Marking mod case {guildId}/{caseId} as deleted.");
			await _punishmentDatabase.UpdateModCase(modCase);

			_eventHandler.ModCaseMarkedToBeDeletedEvent.Invoke(modCase, Identity, announcePublic, false);
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

	public async Task<ModCase> UpdateModCase(ModCase modCase, bool handlePunishment, bool sendPublicNotification)
	{
		var currentReportedUser = await _discordRest.FetchUserInfo(modCase.UserId, CacheBehavior.IgnoreButCacheOnError);

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
			modCase.Discriminator = currentReportedUser.Discriminator;

			var currentReportedMember =
				await _discordRest.FetchUserInfo(modCase.GuildId, modCase.UserId, CacheBehavior.IgnoreButCacheOnError);

			if (currentReportedMember != null)
			{
				if (currentReportedMember.RoleIds.Any(x => guildConfig.ModRoles.Contains(x)) ||
				    currentReportedMember.RoleIds.Any(x => guildConfig.AdminRoles.Contains(x)))
				{
					_logger.LogInformation("Cannot create cases for team members.");

					throw new ProtectedModCaseSuspectException("Cannot create cases for team members.", modCase)
						.WithError(ApiError.ProtectedModCaseSuspectIsTeam);
				}

				modCase.Nickname = currentReportedMember.Nickname;
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

			_eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity, sendPublicNotification, false);

			if (!handlePunishment || (!modCase.PunishmentActive && modCase.PunishmentType != PunishmentType.Kick))
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

	public async Task<List<ModCase>> GetCasePagination(ulong guildId, int startPage = 1, int pageSize = 20)
	{
		return await _punishmentDatabase.SelectAllModCasesForGuild(guildId, startPage, pageSize);
	}

	public async Task<List<ModCase>> GetCasePaginationFilteredForUser(ulong guildId, ulong userId, int startPage = 1,
		int pageSize = 20)
	{
		return await _punishmentDatabase.SelectAllModCasesForSpecificUserOnGuild(guildId, userId, startPage,
			pageSize);
	}

	public async Task<List<ModCase>> GetCasesForUser(ulong userId)
	{
		return await _punishmentDatabase.SelectAllModCasesForSpecificUser(userId);
	}

	public async Task<List<ModCase>> GetCasesForGuild(ulong guildId)
	{
		return await _punishmentDatabase.SelectAllModCasesForGuild(guildId);
	}

	public async Task<List<ModCase>> GetCasesForGuildAndUser(ulong guildId, ulong userId)
	{
		return await _punishmentDatabase.SelectAllModCasesForSpecificUserOnGuild(guildId, userId);
	}

	public async Task<int> CountAllCases()
	{
		return await _punishmentDatabase.CountAllModCases();
	}

	public async Task<int> CountAllCasesForGuild(ulong guildId)
	{
		return await _punishmentDatabase.CountAllModCasesForGuild(guildId);
	}

	public async Task<int> CountAllPunishmentsForGuild(ulong guildId)
	{
		return await _punishmentDatabase.CountAllActivePunishmentsForGuild(guildId);
	}

	public async Task<int> CountAllActiveMutesForGuild(ulong guildId)
	{
		return await _punishmentDatabase.CountAllActivePunishmentsForGuild(guildId, PunishmentType.Mute);
	}

	public async Task<int> CountAllActiveBansForGuild(ulong guildId)
	{
		return await _punishmentDatabase.CountAllActivePunishmentsForGuild(guildId, PunishmentType.Ban);
	}

	public async Task<List<ModCase>> SearchCases(ulong guildId, string searchString)
	{
		var modCases = await _punishmentDatabase.SelectAllModCasesForGuild(guildId);
		var filteredModCases = new List<ModCase>();

		foreach (var c in modCases)
		{
			var entry = new ModCaseTableEntry(
				c,
				await _discordRest.FetchUserInfo(c.ModId, CacheBehavior.OnlyCache),
				await _discordRest.FetchUserInfo(c.UserId, CacheBehavior.OnlyCache)
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

		_eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity, false, false);

		return modCase;
	}

	public async Task<ModCase> UnlockCaseComments(ulong guildId, int caseId)
	{
		var modCase = await GetModCase(guildId, caseId);
		modCase.AllowComments = true;
		modCase.LockedAt = null;
		modCase.LockedByUserId = 0;

		await _punishmentDatabase.UpdateModCase(modCase);

		_eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity, false, false);

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

	public async Task<List<DbCountView>> GetCounts(ulong guildId, DateTime since)
	{
		return await _punishmentDatabase.GetCaseCountGraph(guildId, since);
	}

	public async Task<List<DbCountView>> GetPunishmentCounts(ulong guildId, DateTime since)
	{
		return await _punishmentDatabase.GetPunishmentCountGraph(guildId, since);
	}

	public async Task<ModCase> ActivateModCase(ulong guildId, int caseId)
	{
		var modCase = await GetModCase(guildId, caseId);
		modCase.PunishmentActive = true;
		modCase.LastEditedAt = DateTime.UtcNow;
		modCase.LastEditedByModId = Identity.Id;

		await _punishmentDatabase.UpdateModCase(modCase);

		_eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity, false, false);

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

		_eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity, false, false);

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

			_eventHandler.ModCaseUpdatedEvent.Invoke(modCase, Identity, false, false);

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
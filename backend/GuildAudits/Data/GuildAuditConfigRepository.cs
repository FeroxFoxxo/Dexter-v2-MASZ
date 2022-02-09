using Bot.Abstractions;
using Bot.Dynamics;
using Bot.Enums;
using Bot.Exceptions;
using Bot.Extensions;
using Bot.Services;
using GuildAudits.Enums;
using GuildAudits.Events;
using GuildAudits.Exceptions;
using GuildAudits.Models;

namespace GuildAudits.Data;

public class GuildAuditConfigRepository : Repository, DeleteGuildData
{
	private readonly GuildAuditEventHandler _eventHandler;
	private readonly GuildAuditDatabase _guildAuditDatabase;

	public GuildAuditConfigRepository(DiscordRest discordRest, GuildAuditDatabase guildAuditDatabase,
		GuildAuditEventHandler eventHandler) : base(discordRest)
	{
		_guildAuditDatabase = guildAuditDatabase;
		_eventHandler = eventHandler;
	}

	public async Task DeleteGuildData(ulong guildId)
	{
		await _guildAuditDatabase.DeleteAllAuditLogConfigsForGuild(guildId);
	}

	public async Task<List<GuildAuditConfig>> GetConfigsByGuild(ulong guildId)
	{
		return await _guildAuditDatabase.SelectAllAuditLogConfigsForGuild(guildId);
	}

	public async Task<GuildAuditConfig> GetConfigsByGuildAndType(ulong guildId, GuildAuditEvent type)
	{
		var config = await _guildAuditDatabase.SelectAuditLogConfigForGuildAndType(guildId, type);

		if (config == null)
			throw new ResourceNotFoundException($"GuildAudit config {guildId}/{type} does not exist.");

		return config;
	}

	public async Task<GuildAuditConfig> UpdateConfig(GuildAuditConfig newValue)
	{
		if (!Enum.IsDefined(typeof(GuildAuditEvent), newValue.GuildAuditEvent))
			throw new InvalidAuditLogEventException();

		var action = RestAction.Updated;
		GuildAuditConfig auditLogConfig;

		try
		{
			auditLogConfig = await GetConfigsByGuildAndType(newValue.GuildId, newValue.GuildAuditEvent);
		}
		catch (ResourceNotFoundException)
		{
			auditLogConfig = new GuildAuditConfig();
			action = RestAction.Created;
		}

		auditLogConfig.GuildId = newValue.GuildId;
		auditLogConfig.GuildAuditEvent = newValue.GuildAuditEvent;
		auditLogConfig.ChannelId = newValue.ChannelId;
		auditLogConfig.PingRoles = newValue.PingRoles;

		await _guildAuditDatabase.PutAuditLogConfig(auditLogConfig);

		if (action == RestAction.Created)
			_eventHandler.GuildAuditConfigCreatedEvent.Invoke(auditLogConfig, Identity);
		else
			_eventHandler.GuildAuditUpdatedEvent.Invoke(auditLogConfig, Identity);

		return auditLogConfig;
	}

	public async Task<GuildAuditConfig> DeleteConfigForGuild(ulong guildId, GuildAuditEvent type)
	{
		var config = await GetConfigsByGuildAndType(guildId, type);

		await _guildAuditDatabase.DeleteSpecificAuditLogConfig(config);

		_eventHandler.GuildAuditConfigDeletedEvent.Invoke(config, Identity);

		return config;
	}
}
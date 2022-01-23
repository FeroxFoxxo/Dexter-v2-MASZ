﻿using MASZ.Bot.Abstractions;
using MASZ.Bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MASZ.Bot.Data;

public class BotDatabase : DataContext<BotDatabase>, DataContextCreate
{
	public BotDatabase(DbContextOptions<BotDatabase> options) : base(options)
	{
	}

	private DbSet<AppSettings> AppSettings { get; set; }

	public DbSet<GuildConfig> GuildConfigs { get; set; }

	private DbSet<ApiToken> ApiTokens { get; set; }

	public static void AddContextToServiceProvider(Action<DbContextOptionsBuilder> optionsAction,
		IServiceCollection serviceCollection)
	{
		serviceCollection.AddDbContext<BotDatabase>(optionsAction);
	}

	public async Task<AppSettings> GetAppSettings()
	{
		return await AppSettings.AsQueryable().OrderByDescending(x => x.ClientId).FirstOrDefaultAsync();
	}

	public async Task UpdateAppSetting(AppSettings appSettings)
	{
		AppSettings.Update(appSettings);
		await SaveChangesAsync();
	}

	public async Task AddAppSetting(AppSettings appSettings)
	{
		AppSettings.Add(appSettings);
		await SaveChangesAsync();
	}
	
	public async Task<GuildConfig> SelectSpecificGuildConfig(ulong guildId)
	{
		return await GuildConfigs.AsQueryable().FirstOrDefaultAsync(x => x.GuildId == guildId);
	}

	public async Task<List<GuildConfig>> SelectAllGuildConfigs()
	{
		return await GuildConfigs.AsQueryable().ToListAsync();
	}

	public async Task DeleteSpecificGuildConfig(GuildConfig guildConfig)
	{
		GuildConfigs.Remove(guildConfig);
		await SaveChangesAsync();
	}

	public async Task InternalUpdateGuildConfig(GuildConfig guildConfig)
	{
		GuildConfigs.Update(guildConfig);
		await SaveChangesAsync();
	}

	public async Task SaveGuildConfig(GuildConfig guildConfig)
	{
		await GuildConfigs.AddAsync(guildConfig);
		await SaveChangesAsync();
	}

	public async Task<int> CountAllGuildConfigs()
	{
		return await GuildConfigs.AsQueryable().CountAsync();
	}

	public async Task SaveToken(ApiToken token)
	{
		await ApiTokens.AddAsync(token);
		await SaveChangesAsync();
	}

	public async Task DeleteToken(ApiToken token)
	{
		ApiTokens.Remove(token);
		await SaveChangesAsync();
	}

	public async Task<int> CountAllApiTokens()
	{
		return await ApiTokens.AsQueryable().CountAsync();
	}

	public async Task<ApiToken> GetApiToken()
	{
		return await ApiTokens.AsQueryable().FirstOrDefaultAsync();
	}
}
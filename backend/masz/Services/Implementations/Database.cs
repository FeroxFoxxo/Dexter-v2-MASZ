﻿using masz.data;
using masz.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace masz.Services
{
    public class Database : IDatabase
    {
        private readonly ILogger<Database> logger;
        private readonly DataContext context;

        public Database() { }

        public Database(ILogger<Database> logger, DataContext context)
        {
            this.logger = logger;
            this.context = context;
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }

        public async Task<GuildConfig> SelectSpecificGuildConfig(string guildId)
        {
            return await context.GuildConfigs.AsQueryable().FirstOrDefaultAsync(x => x.GuildId == guildId);
        }

        public async Task<List<GuildConfig>> SelectAllGuildConfigs()
        {
            return await context.GuildConfigs.AsQueryable().ToListAsync();
        }

        public void DeleteSpecificGuildConfig(GuildConfig guildConfig)
        {
            context.GuildConfigs.Remove(guildConfig);
        }

        public void UpdateGuildConfig(GuildConfig guildConfig)
        {
            context.GuildConfigs.Update(guildConfig);
        }

        public async Task SaveGuildConfig(GuildConfig guildConfig)
        {
            await context.GuildConfigs.AddAsync(guildConfig);
        }

        public async Task<ModCase> SelectSpecificModCase(string guildId, string modCaseId)
        {
            return await context.ModCases.AsQueryable().FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id.ToString() == modCaseId);
        }

        public async Task<List<ModCase>> SelectAllModcasesForSpecificUserOnGuild(string guildId, string userId)
        {
            return await context.ModCases.AsQueryable().Where(x => x.GuildId == guildId && x.UserId == userId).ToListAsync();
        }

        public async Task<List<ModCase>> SelectAllModCasesForGuild(string guildId)
        {
            return await context.ModCases.AsQueryable().Where(x => x.GuildId == guildId).ToListAsync();
        }

        public void DeleteSpecificModCase(ModCase modCase)
        {
            context.ModCases.Remove(modCase);
        }

        public void UpdateModCase(ModCase modCase)
        {
            context.ModCases.Update(modCase);
        }

        public async Task SaveModCase(ModCase modCase)
        {
            await context.ModCases.AddAsync(modCase);
        }
    }
}
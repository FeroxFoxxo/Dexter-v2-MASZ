using Bot.Abstractions;
using Levels.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Levels.Data;

public class LevelsDatabase(DbContextOptions<LevelsDatabase> options) : DataContext<LevelsDatabase>(options), IDataContextCreate
{
    public DbSet<GuildUserLevel> GuildUserLevels { get; set; }
    public DbSet<GuildLevelConfig> GuildLevelConfigs { get; set; }
    public DbSet<UserRankcardConfig> UserRankcardConfigs { get; set; }

    public static void AddContextToServiceProvider(Action<DbContextOptionsBuilder> optionsAction,
        IServiceCollection serviceCollection) =>
        serviceCollection.AddDbContext<LevelsDatabase>(optionsAction);

    public override void OverrideModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<GuildLevelConfig>()
            .Property(e => e.Levels)
            .HasConversion(new DictionaryDataConverter<int, ulong[]>(),
                new DictionaryDataComparer<int, ulong[]>());

        modelBuilder
            .Entity<GuildLevelConfig>()
            .Property(e => e.LevelUpMessageOverrides)
            .HasConversion(new DictionaryDataConverter<int, string>(),
                new DictionaryDataComparer<int, string>());

        modelBuilder
            .Entity<GuildLevelConfig>()
            .Property(e => e.Experience)
            .HasConversion(new JsonDataConverter<ExperienceConfig>(),
                new JsonDataComparer<ExperienceConfig>());

        modelBuilder
            .Entity<GuildLevelConfig>()
            .Property(e => e.ExperienceOverrides)
            .HasConversion(new DictionaryDataConverter<ulong, ExperienceConfig>(),
                new DictionaryDataComparer<ulong, ExperienceConfig>());
    }

    private static bool CheckNullAndReport([NotNullWhen(false)] object o, string name)
    {
        if (o is null)
        {
            new LoggerFactory().CreateLogger<LevelsDatabase>()
                .LogError(new NullReferenceException(), $"{name} is null in LevelsDatabase.");
            return true;
        }

        return false;
    }

    /***************/
    /* USER LEVELS */
    /***************/

    public async Task RegisterGuildUserLevel(GuildUserLevel guildUserLevel)
    {
        if (CheckNullAndReport(GuildUserLevels, "GuildUserLevels"))
            return;
        await GuildUserLevels.AddAsync(guildUserLevel);
        await SaveChangesAsync();
    }

    public GuildUserLevel GetGuildUserLevel(ulong guildid, ulong userid) =>
        CheckNullAndReport(GuildUserLevels, "GuildUserLevels") ? null :
        GuildUserLevels.Find(guildid, userid);

    public GuildUserLevel[] GetGuildUserLevelByGuild(ulong guildid) =>
        CheckNullAndReport(GuildUserLevels, "GuildUserLevels")
            ? []
            : [.. GuildUserLevels.AsQueryable().Where(x => x.GuildId == guildid)];

    public async Task UpdateGuildUserLevel(GuildUserLevel guildUserLevel)
    {
        if (CheckNullAndReport(GuildUserLevels, "GuildUserLevels"))
            return;
        GuildUserLevels.Update(guildUserLevel);
        await SaveChangesAsync();
    }

    public async Task DeleteSpecificGuildUserLevel(GuildUserLevel guildUserLevel)
    {
        if (CheckNullAndReport(GuildUserLevels, "GuildUserLevels"))
            return;
        GuildUserLevels.Remove(guildUserLevel);
        await SaveChangesAsync();
    }

    public async Task<int> DeleteGuildUserLevelsByGuild(ulong guildid)
    {
        if (CheckNullAndReport(GuildUserLevels, "GuildUserLevels"))
            return 0;
        var range = GuildUserLevels.AsQueryable().Where(x => x.GuildId == guildid).ToArray();
        GuildUserLevels.RemoveRange(range);
        await SaveChangesAsync();
        return range.Length;
    }

    /********************/
    /* RANKCARD CONFIGS */
    /********************/

    public async Task RegisterUserRankcardConfig(UserRankcardConfig rankcardConfig)
    {
        if (CheckNullAndReport(UserRankcardConfigs, "UserRankcardConfigs"))
            return;
        await UserRankcardConfigs.AddAsync(rankcardConfig);
        await SaveChangesAsync();
    }

    public UserRankcardConfig GetUserRankcardConfig(ulong userid) =>
        CheckNullAndReport(UserRankcardConfigs, "UserRankcardConfigs") ? null : UserRankcardConfigs.Find(userid);

    public async Task UpdateUserRankcardConfig(UserRankcardConfig rankcardConfig)
    {
        if (CheckNullAndReport(UserRankcardConfigs, "UserRankcardConfigs"))
            return;
        UserRankcardConfigs.Update(rankcardConfig);
        await SaveChangesAsync();
    }

    public async Task DeleteUserRankcardConfig(UserRankcardConfig rankcardConfig)
    {
        if (CheckNullAndReport(UserRankcardConfigs, "UserRankcardConfigs"))
            return;
        UserRankcardConfigs.Remove(rankcardConfig);
        await SaveChangesAsync();
    }

    /*****************/
    /* GUILD CONFIGS */
    /*****************/

    public async Task RegisterGuildLevelConfig(GuildLevelConfig guildLevelConfig)
    {
        if (CheckNullAndReport(GuildLevelConfigs, "GuildLevelsConfigs"))
            return;
        await GuildLevelConfigs.AddAsync(guildLevelConfig);
        await SaveChangesAsync();
    }

    public GuildLevelConfig GetGuildLevelConfig(ulong guildid) =>
        CheckNullAndReport(GuildLevelConfigs, "GuildLevelsConfigs") ? null : GuildLevelConfigs.Find(guildid);

    public GuildLevelConfig[] GetAllGuildLevelConfigs() => CheckNullAndReport(GuildLevelConfigs, "GuildLevelsConfigs")
        ? []
        : [.. GuildLevelConfigs];

    public async Task UpdateGuildLevelConfig()
    {
        if (CheckNullAndReport(GuildLevelConfigs, "GuildLevelsConfigs"))
            return;

        await SaveChangesAsync();
    }

    public async Task DeleteGuildLevelConfig(GuildLevelConfig guildLevelConfig)
    {
        if (CheckNullAndReport(GuildLevelConfigs, "UserRankcardConfigs"))
            return;
        GuildLevelConfigs.Remove(guildLevelConfig);
        await SaveChangesAsync();
    }
}

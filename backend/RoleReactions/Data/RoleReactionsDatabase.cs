using Bot.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoleReactions.Models;

namespace RoleReactions.Data;

public class RoleReactionsDatabase(DbContextOptions<RoleReactionsDatabase> options) :
    DataContext<RoleReactionsDatabase>(options), IDataContextCreate
{
    public DbSet<RoleMenu> RoleReactionsMenu { get; set; }
    public DbSet<UserRoles> UserRoles { get; set; }

    public static void AddContextToServiceProvider(Action<DbContextOptionsBuilder> optionsAction,
        IServiceCollection serviceCollection) =>
        serviceCollection.AddDbContext<RoleReactionsDatabase>(optionsAction);

    public override void OverrideModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<RoleMenu>()
            .Property(e => e.RoleToEmote)
            .HasConversion(new DictionaryDataConverter<ulong, string>(),
                new DictionaryDataComparer<ulong, string>());

        modelBuilder
            .Entity<RoleMenu>()
            .Property(e => e.RoleToPrerequesite)
            .HasConversion(new DictionaryDataConverter<ulong, ulong>(),
                new DictionaryDataComparer<ulong, ulong>());

        modelBuilder
            .Entity<UserRoles>()
            .Property(e => e.RoleIds)
            .HasConversion(new ListDataConverter<ulong>(),
                new ListDataComparer<ulong>());
    }
}

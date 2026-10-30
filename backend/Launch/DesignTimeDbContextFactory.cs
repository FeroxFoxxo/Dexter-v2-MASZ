using AutoMods.Data;
using Bot.Data;
using Greeting.Data;
using GuildAudits.Data;
using Invites.Data;
using JoinLeave.Data;
using Levels.Data;
using Messaging.Data;
using MOTDs.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PrivateVcs.Data;
using Punishments.Data;
using RoleReactions.Data;
using UserMaps.Data;
using UserNotes.Data;

namespace Launch;

public sealed class DesignTimeDbContextFactory :
    IDesignTimeDbContextFactory<AutoModDatabase>,
    IDesignTimeDbContextFactory<BotDatabase>,
    IDesignTimeDbContextFactory<GreeterDatabase>,
    IDesignTimeDbContextFactory<GuildAuditDatabase>,
    IDesignTimeDbContextFactory<InviteDatabase>,
    IDesignTimeDbContextFactory<JoinLeaveDatabase>,
    IDesignTimeDbContextFactory<LevelsDatabase>,
    IDesignTimeDbContextFactory<MessagingDatabase>,
    IDesignTimeDbContextFactory<MotdDatabase>,
    IDesignTimeDbContextFactory<PrivateVcDatabase>,
    IDesignTimeDbContextFactory<PunishmentDatabase>,
    IDesignTimeDbContextFactory<RoleReactionsDatabase>,
    IDesignTimeDbContextFactory<UserMapDatabase>,
    IDesignTimeDbContextFactory<UserNoteDatabase>
{
    private static DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        builder.UseMySQL("Server=localhost;Port=3306;Database=dexter_design;Uid=root;Pwd=;");
        return builder.Options;
    }

    AutoModDatabase IDesignTimeDbContextFactory<AutoModDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<AutoModDatabase>());

    BotDatabase IDesignTimeDbContextFactory<BotDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<BotDatabase>());

    GreeterDatabase IDesignTimeDbContextFactory<GreeterDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<GreeterDatabase>());

    GuildAuditDatabase IDesignTimeDbContextFactory<GuildAuditDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<GuildAuditDatabase>());

    InviteDatabase IDesignTimeDbContextFactory<InviteDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<InviteDatabase>());

    JoinLeaveDatabase IDesignTimeDbContextFactory<JoinLeaveDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<JoinLeaveDatabase>());

    LevelsDatabase IDesignTimeDbContextFactory<LevelsDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<LevelsDatabase>());

    MessagingDatabase IDesignTimeDbContextFactory<MessagingDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<MessagingDatabase>());

    MotdDatabase IDesignTimeDbContextFactory<MotdDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<MotdDatabase>());

    PrivateVcDatabase IDesignTimeDbContextFactory<PrivateVcDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<PrivateVcDatabase>());

    PunishmentDatabase IDesignTimeDbContextFactory<PunishmentDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<PunishmentDatabase>());

    RoleReactionsDatabase IDesignTimeDbContextFactory<RoleReactionsDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<RoleReactionsDatabase>());

    UserMapDatabase IDesignTimeDbContextFactory<UserMapDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<UserMapDatabase>());

    UserNoteDatabase IDesignTimeDbContextFactory<UserNoteDatabase>.CreateDbContext(string[] args)
        => new(CreateOptions<UserNoteDatabase>());
}

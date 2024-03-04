using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Extensions;
using Bot.Models;
using Bot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Punishments.Enums;
using Punishments.Extensions;
using Punishments.Models;

namespace Punishments.Events;

public class PunishmentEventAnnouncer(DiscordRest discordRest, PunishmentEventHandler eventHandler,
    ILogger<PunishmentEventAnnouncer> logger, IServiceProvider serviceProvider,
    DiscordSocketClient client) : IEvent
{
    private readonly DiscordSocketClient _client = client;
    private readonly DiscordRest _discordRest = discordRest;
    private readonly PunishmentEventHandler _eventHandler = eventHandler;
    private readonly ILogger<PunishmentEventAnnouncer> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public void RegisterEvents()
    {
        _eventHandler.OnModCaseCreated += AnnounceModCaseCreated;

        _eventHandler.OnModCaseUpdated += async (a, b) => await AnnounceModCase(a, b, RestAction.Updated);

        _eventHandler.OnModCaseDeleted += async (a, b) => await AnnounceModCase(a, b, RestAction.Deleted);

        _eventHandler.OnModCaseMarkedToBeDeleted += async (a, b) => await AnnounceModCase(a, b, RestAction.Deleted);

        _eventHandler.OnModCaseCommentCreated += async (a, b) => await AnnounceComment(a, b, RestAction.Created);

        _eventHandler.OnModCaseCommentUpdated += async (a, b) => await AnnounceComment(a, b, RestAction.Updated);

        _eventHandler.OnModCaseCommentDeleted += async (a, b) => await AnnounceComment(a, b, RestAction.Deleted);

        _eventHandler.OnFileUploaded += async (a, b, c) => await AnnounceFile(a, b, c, RestAction.Created);

        _eventHandler.OnFileDeleted += async (a, b, c) => await AnnounceFile(a, b, c, RestAction.Deleted);
    }

    private async Task AnnounceModCaseCreated(ModCase modCase, IUser actor, AnnouncementResult result)
    {
        using var scope = _serviceProvider.CreateScope();

        _logger.LogInformation($"Announcing mod-case {modCase.Id} in guild {modCase.GuildId}.");

        await _discordRest.FetchUserInfo(modCase.UserId, false);

        var guildConfig = await scope.ServiceProvider.GetRequiredService<GuildConfigRepository>()
            .GetGuildConfig(modCase.GuildId);

        var settings = await scope.ServiceProvider.GetRequiredService<SettingsRepository>()
            .GetAppSettings();

        _logger.LogInformation(
            $"Sending webhook for mod-case {modCase.GuildId}/{modCase.CaseId} to {guildConfig.StaffLogs}.");

        try
        {
            var (embed, _) =
                await modCase.CreateNewModCaseEmbed(guildConfig, settings, result, _discordRest, scope.ServiceProvider);

            await _client.SendEmbed(guildConfig.GuildId, guildConfig.StaffLogs, embed);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                $"Error while announcing mod-case {modCase.GuildId}/{modCase.CaseId} to {guildConfig.StaffLogs}.");
        }
    }

    private async Task AnnounceModCase(ModCase modCase, IUser actor, RestAction action)
    {
        using var scope = _serviceProvider.CreateScope();

        _logger.LogInformation($"Announcing mod-case {modCase.Id} in guild {modCase.GuildId}.");

        await _discordRest.FetchUserInfo(modCase.UserId, false);

        var guildConfig = await scope.ServiceProvider.GetRequiredService<GuildConfigRepository>()
            .GetGuildConfig(modCase.GuildId);

        _logger.LogInformation(
            $"Sending webhook for mod-case {modCase.GuildId}/{modCase.CaseId} to {guildConfig.StaffLogs}.");

        try
        {
            var embed = await modCase.CreateModCaseEmbed(action, _discordRest, scope.ServiceProvider);

            await _client.SendEmbed(guildConfig.GuildId, guildConfig.StaffLogs, embed);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                $"Error while announcing mod-case {modCase.GuildId}/{modCase.CaseId} to {guildConfig.StaffLogs}.");
        }
    }

    private async Task AnnounceComment(ModCaseComment comment, IUser actor, RestAction action)
    {
        using var scope = _serviceProvider.CreateScope();

        _logger.LogInformation(
            $"Announcing comment {comment.Id} in case {comment.ModCase.GuildId}/{comment.ModCase.CaseId}.");

        var guildConfig = await scope.ServiceProvider.GetRequiredService<GuildConfigRepository>()
            .GetGuildConfig(comment.ModCase.GuildId);

        _logger.LogInformation(
            $"Sending webhook for comment {comment.ModCase.GuildId}/{comment.ModCase.CaseId}/{comment.Id} to {guildConfig.StaffLogs}.");

        try
        {
            var embed = await comment.CreateCommentEmbed(action, actor, scope.ServiceProvider);

            await _client.SendEmbed(guildConfig.GuildId, guildConfig.StaffAnnouncements, embed);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                $"Error while announcing comment {comment.ModCase.GuildId}/{comment.ModCase.CaseId}/{comment.Id} to {guildConfig.StaffLogs}.");
        }
    }

    private async Task AnnounceFile(UploadedFile file, ModCase modCase, IUser actor, RestAction action)
    {
        using var scope = _serviceProvider.CreateScope();

        _logger.LogInformation($"Announcing file {modCase.GuildId}/{modCase.CaseId}/{file.Name}.");

        var guildConfig = await scope.ServiceProvider.GetRequiredService<GuildConfigRepository>()
            .GetGuildConfig(modCase.GuildId);

        _logger.LogInformation(
            $"Sending webhook for file {modCase.GuildId}/{modCase.CaseId}/{file.Name} to {guildConfig.StaffLogs}.");

        try
        {
            var embed = await file.CreateFileEmbed(modCase, action, actor, scope.ServiceProvider);

            await _client.SendEmbed(guildConfig.GuildId, guildConfig.StaffLogs, embed);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                $"Error while announcing file {modCase.GuildId}/{modCase.CaseId}/{file.Name} to {guildConfig.StaffLogs}.");
        }
    }
}

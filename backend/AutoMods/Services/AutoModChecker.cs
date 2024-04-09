using AutoMods.Data;
using AutoMods.Enums;
using AutoMods.MessageChecks;
using AutoMods.Models;
using Bot.Abstractions;
using Bot.Data;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoMods.Services;

public class AutoModChecker(DiscordSocketClient client, ILogger<AutoModChecker> logger, IServiceProvider services) : IEvent
{
    private readonly DiscordSocketClient _client = client;
    private readonly ILogger<AutoModChecker> _logger = logger;
    private readonly IServiceProvider _services = services;

    public void RegisterEvents()
    {
        _client.MessageReceived += MessageCreatedHandler;
        _client.MessageUpdated += MessageUpdatedHandler;
    }

    private async Task MessageCreatedHandler(SocketMessage message)
    {
        if (message.Channel is ITextChannel channel)
        {
            if (message.Type is not MessageType.Default and not MessageType.Reply)
                return;

            if (message.Author.IsBot)
                return;

            if (channel.Guild == null)
                return;

            await HandleAutoMod(message);
        }
    }

    private async Task MessageUpdatedHandler(Cacheable<IMessage, ulong> oldMsg, SocketMessage newMsg,
        ISocketMessageChannel channel)
    {
        if (channel is ITextChannel txtChannel)
        {
            if (newMsg.Type is not MessageType.Default and not MessageType.Reply)
                return;

            if (newMsg.Author.IsBot)
                return;

            if (txtChannel.Guild == null)
                return;

            await HandleAutoMod(newMsg, true);
        }
    }

    public async Task HandleAutoMod(IMessage message, bool onEdit = false)
    {
        if (message.Type is not MessageType.Default and not MessageType.Reply)
            return;

        if (message.Author.IsBot)
            return;

        if ((message.Channel as ITextChannel)?.Guild == null)
            return;

        using var scope = _services.CreateScope();

        if (!onEdit)
            if (await CheckAutoMod(
                    AutoModType.TooManyMessages,
                    message,
                    SpamCheck.Check,
                    scope
                ))
                return;

        if (await CheckAutoMod(
                AutoModType.InvitePosted,
                message,
                InviteChecker.Check,
                scope
            )) return;

        if (await CheckAutoMod(
                AutoModType.TooManyEmotes,
                message,
                EmoteCheck.Check,
                scope
            )) return;

        if (await CheckAutoMod(
                AutoModType.TooManyMentions,
                message,
                MentionCheck.Check,
                scope
            )) return;

        if (await CheckAutoMod(
                AutoModType.TooManyAttachments,
                message,
                AttachmentCheck.Check,
                scope
            )) return;

        if (await CheckAutoMod(
                AutoModType.TooManyEmbeds,
                message,
                EmbedCheck.Check,
                scope
            )) return;

        if (await CheckAutoMod(
                AutoModType.CustomWordFilter,
                message,
                CustomWordCheck.Check,
                scope
            )) return;

        if (await CheckAutoMod(
                AutoModType.TooManyDuplicatedCharacters,
                message,
                DuplicatedCharacterCheck.Check,
                scope
            )) return;

        await CheckAutoMod(
            AutoModType.TooManyLinks,
            message,
            LinkCheck.Check,
            scope
        );
    }

    private async Task<bool> CheckAutoMod(AutoModType autoModType, IMessage message,
        Func<IMessage, AutoModConfig, DiscordSocketClient, Task<bool>> predicate, IServiceScope scope)
    {
        var autoModConfig = await GetAutomodConfig(autoModType, message, scope);

        return autoModConfig != null &&
            await predicate(message, autoModConfig, _client) &&
            await FinaliseAutoMod(autoModConfig, message, scope);
    }

    private async Task<bool> CheckAutoMod(AutoModType autoModType, IMessage message,
        Func<IMessage, AutoModConfig, DiscordSocketClient, bool> predicate, IServiceScope scope)
    {
        var autoModConfig = await GetAutomodConfig(autoModType, message, scope);

        return autoModConfig != null
            && predicate(message, autoModConfig, _client) &&
            await FinaliseAutoMod(autoModConfig, message, scope);
    }

    private async Task CheckAutoMod(AutoModType autoModType, IMessage message,
        Func<IMessage, AutoModConfig, IServiceScope, Task<bool>> predicate, IServiceScope scope)
    {
        var autoModConfig = await GetAutomodConfig(autoModType, message, scope);

        if (autoModConfig == null) return;

        if (!await predicate(message, autoModConfig, scope)) return;

        await FinaliseAutoMod(autoModConfig, message, scope);
    }

    private async Task<bool> FinaliseAutoMod(AutoModConfig autoModConfig, IMessage message, IServiceScope scope)
    {
        if (await IsProtectedByFilter(message, autoModConfig, scope)) return false;

        var modType = autoModConfig.AutoModType;

        if (modType == AutoModType.TooManyLinks && autoModConfig.Limit == 0)
            modType = AutoModType.NoLinksAllowed;

        var channel = (ITextChannel)message.Channel;

        _logger.LogInformation(
            $"U: {message.Author.Id} | C: {channel.Id} | G: {channel.Guild.Id} triggered {modType}.");

        await ExecutePunishment(message, modType, autoModConfig.AutoModAction, scope);

        if (modType != AutoModType.TooManyAutomods)
            await CheckAutoMod(AutoModType.TooManyAutomods, message, CheckMultipleEvents, scope);

        return true;
    }

    private static async Task<AutoModConfig> GetAutomodConfig(AutoModType autoModType, IMessage message, IServiceScope scope)
    {
        var guild = ((ITextChannel)message.Channel).Guild;

        return (await scope.ServiceProvider.GetRequiredService<AutoModConfigRepository>()
                .GetConfigsByGuild(guild.Id))
            .FirstOrDefault(x => x.AutoModType == autoModType);
    }

    private static async Task<bool> IsProtectedByFilter(IMessage message, AutoModConfig autoModConfig,
        IServiceScope scope)
    {
        var config = await scope.ServiceProvider.GetRequiredService<SettingsRepository>().GetAppSettings();

        if (config.SiteAdmins.Contains(message.Author.Id))
            return true;

        var guild = ((ITextChannel)message.Channel).Guild;

        var user = await guild.GetUserAsync(message.Author.Id);

        if (user == null)
            return false;

        if (user.Guild.OwnerId == user.Id)
            return true;

        var guildConfig = await scope.ServiceProvider.GetRequiredService<GuildConfigRepository>()
            .GetGuildConfig(guild.Id);

        return user.RoleIds.Any(x =>
                   guildConfig.ModRoles.Contains(x) ||
                   guildConfig.AdminRoles.Contains(x) ||
                   autoModConfig.IgnoreRoles.Contains(x)
               ) ||
               (message.Channel is ITextChannel { CategoryId: { } } textChannel
                   ? autoModConfig.IgnoreChannels.Contains(message.Channel.Id) ||
                     autoModConfig.IgnoreChannels.Contains(textChannel.CategoryId.Value)
                   : autoModConfig.IgnoreChannels.Contains(((ITextChannel)message.Channel)!.Id));
    }

    private static async Task<bool> CheckMultipleEvents(IMessage message, AutoModConfig config, IServiceScope scope)
    {
        if (config.Limit == null)
            return false;

        if (config.TimeLimitMinutes == null)
            return false;

        var existing = await scope.ServiceProvider.GetRequiredService<AutoModEventRepository>()
            .GetAllEventsForUserSinceMinutes(message.Author.Id, config.TimeLimitMinutes.Value);

        return existing.Where(x => x.AutoModType != AutoModType.TooManyAutomods).Count() > config.Limit.Value;
    }

    private async Task ExecutePunishment(IMessage message, AutoModType type, AutoModAction action, IServiceScope scope)
    {
        AutoModEvent modEvent = new()
        {
            GuildId = ((ITextChannel)message.Channel).Guild.Id,
            AutoModType = type,
            AutoModAction = action,
            UserId = message.Author.Id,
            MessageId = message.Id,
            MessageContent = message.Content
        };

        await scope.ServiceProvider.GetRequiredService<AutoModEventRepository>()
            .RegisterEvent(modEvent, (ITextChannel)message.Channel, message.Author);

        if (modEvent.AutoModAction is AutoModAction.ContentDeleted or AutoModAction.ContentDeletedAndCaseCreated)
            try
            {
                RequestOptions requestOptions = new()
                {
                    RetryMode = RetryMode.RetryRatelimit
                };
                await message.DeleteAsync(requestOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting message {message.Id}.");
            }
    }
}

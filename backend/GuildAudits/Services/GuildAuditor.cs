using Bot.Abstractions;
using Bot.Data;
using Bot.Exceptions;
using Bot.Extensions;
using Bot.Services;
using Bot.Translators;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using GuildAudits.Data;
using GuildAudits.Enums;
using GuildAudits.Translators;
using Humanizer;
using Invites.Events;
using Invites.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace GuildAudits.Services;

public class GuildAuditor(DiscordSocketClient client, IServiceProvider serviceProvider,
    InviteEventHandler eventHandler) : IEvent
{
    private static readonly string Check = "\u2705";
    private static readonly string XCheck = "\u274C";

    public void RegisterEvents()
    {
        client.UserBanned += HandleBanAdded;
        client.UserUnbanned += HandleBanRemoved;
        client.InviteCreated += HandleInviteCreated;
        client.UserJoined += HandleUserJoined;
        client.UserLeft += HandleUserRemoved;
        client.MessageReceived += HandleMessageSent;
        client.MessageDeleted += HandleMessageDeleted;
        client.MessageUpdated += HandleMessageUpdated;
        client.ThreadCreated += HandleThreadCreated;
        client.UserUpdated += HandleUsernameUpdated;
        client.GuildMemberUpdated += HandleGuildUserUpdated;
        client.UserVoiceStateUpdated += HandleVoiceStateUpdated;
        client.ReactionAdded += HandleReactionAdded;
        client.ReactionRemoved += HandleReactionRemoved;

        eventHandler.OnInviteDeleted += HandleInviteDeleted;
    }

    public async Task SendEmbed(EmbedBuilder embed, ulong guildId, GuildAuditLogEvent eventType)
    {
        using var scope = serviceProvider.CreateScope();

        var guildConfigRepository = scope.ServiceProvider.GetRequiredService<GuildConfigRepository>();

        embed.WithColor(eventType switch
        {
            GuildAuditLogEvent.MessageSent => Color.Green,
            GuildAuditLogEvent.MessageUpdated => Color.Orange,
            GuildAuditLogEvent.MessageDeleted => Color.Red,
            GuildAuditLogEvent.UsernameUpdated => Color.Orange,
            GuildAuditLogEvent.AvatarUpdated => Color.Orange,
            GuildAuditLogEvent.NicknameUpdated => Color.Orange,
            GuildAuditLogEvent.UserRolesUpdated => Color.Orange,
            GuildAuditLogEvent.UserJoined => Color.Green,
            GuildAuditLogEvent.UserRemoved => Color.Red,
            GuildAuditLogEvent.BanAdded => Color.Red,
            GuildAuditLogEvent.BanRemoved => Color.Green,
            GuildAuditLogEvent.InviteCreated => Color.Green,
            GuildAuditLogEvent.InviteDeleted => Color.Red,
            GuildAuditLogEvent.ThreadCreated => Color.Green,
            GuildAuditLogEvent.VoiceJoined => Color.Green,
            GuildAuditLogEvent.VoiceLeft => Color.Red,
            GuildAuditLogEvent.VoiceMoved => Color.Orange,
            GuildAuditLogEvent.ReactionAdded => Color.Green,
            GuildAuditLogEvent.ReactionRemoved => Color.Red,
            _ => throw new NotImplementedException()
        })
            .WithCurrentTimestamp();

        try
        {
            var guildConfig = await guildConfigRepository.GetGuildConfig(guildId);

            if (guildConfig == null)
                return;

            var auditLogRepository = scope.ServiceProvider.GetRequiredService<GuildAuditConfigRepository>();

            var auditLogConfig = await auditLogRepository.GetConfigsByGuildAndType(guildId, eventType);

            if (auditLogConfig == null)
                return;

            if (embed.Footer == null)
                embed.WithFooter(auditLogConfig.GuildAuditLogEvent.Humanize());
            else
                embed.WithFooter(embed.Footer.Text + $" | {auditLogConfig.GuildAuditLogEvent.Humanize()}");

            StringBuilder rolePings = new();

            foreach (var role in auditLogConfig.PingRoles)
                rolePings.Append($"<@&{role}> ");

            if (await client.GetChannelAsync(auditLogConfig.ChannelId) is ITextChannel channel)
                await channel.SendMessageAsync(rolePings.ToString(), embed: embed.Build());
        }
        catch (ResourceNotFoundException)
        {
        }
    }

    public async Task<bool> CheckForIgnoredRoles(ulong guildId, GuildAuditLogEvent eventType, List<ulong> roles)
    {
        using var scope = serviceProvider.CreateScope();

        var auditLogRepository = scope.ServiceProvider.GetRequiredService<GuildAuditConfigRepository>();

        try
        {
            var auditLogConfig = await auditLogRepository.GetConfigsByGuildAndType(guildId, eventType);
            return auditLogConfig != null && (auditLogConfig.IgnoreRoles?.Any(roles.Contains) ?? false);
        }
        catch (ResourceNotFoundException)
        {
        }

        return false;
    }

    public async Task<bool> CheckForIgnoredRoles(ulong guildId, GuildAuditLogEvent eventType,
        IReadOnlyCollection<SocketRole> roles) =>
        await CheckForIgnoredRoles(guildId, eventType, roles.Select(role => role.Id).ToList());

    public async Task<bool> CheckForIgnoredChannel(ulong guildId, GuildAuditLogEvent eventType, ulong channelId)
    {
        using var scope = serviceProvider.CreateScope();

        var auditLogRepository = scope.ServiceProvider.GetRequiredService<GuildAuditConfigRepository>();

        try
        {
            var auditLogConfig = await auditLogRepository.GetConfigsByGuildAndType(guildId, eventType);
            return auditLogConfig != null && (auditLogConfig.IgnoreChannels?.Contains(channelId) ?? false);
        }
        catch (ResourceNotFoundException)
        {
        }

        return false;
    }

    private static async Task<Translation> TryGetTranslation(IServiceScope scope, ulong guildId)
    {
        var translator = scope.ServiceProvider.GetRequiredService<Translation>();

        try
        {
            await translator.SetLanguage(guildId);
            return translator;
        }
        catch (UnregisteredGuildException)
        {
            return null;
        }
    }

    public async Task HandleGuildUserUpdated(Cacheable<SocketGuildUser, ulong> oldU, SocketGuildUser newU)
    {
        var oldUser = await oldU.GetOrDownloadAsync();

        if (oldUser is null || newU is null)
            return;

        if (await CheckForIgnoredRoles(newU.Guild.Id, GuildAuditLogEvent.UserRolesUpdated, newU.Roles))
            return;

        if (oldUser.Nickname != newU.Nickname)
            await HandleNicknameUpdated(oldUser, newU, newU.Guild.Id);

        if (oldUser.AvatarId != newU.AvatarId)
            await HandleAvatarUpdated(oldUser, newU, newU.Guild.Id);

        if (!Equals(oldUser.Roles, newU.Roles))
            await HandleUserRolesUpdated(newU, oldUser.Roles, newU.Roles, newU.Guild.Id);
    }

    public async Task HandleAvatarUpdated(IGuildUser oldU, IGuildUser newU, ulong guildId)
    {
        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, guildId);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {newU.Username} - {newU.Mention}");
        description.AppendLine($"> **{translator.Get<BotTranslator>().Id()}:** `{newU.Id}`");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().AvatarUpdated())
            .WithDescription(description.ToString())
            .WithImageUrl(newU.GetAvatarOrDefaultUrl())
            .AddField(
                translator.Get<GuildAuditTranslator>().Old(),
                oldU.GetAvatarOrDefaultUrl(),
                true
            ).AddField(
                translator.Get<GuildAuditTranslator>().New(),
                newU.GetAvatarOrDefaultUrl(),
                true
            )
            .WithAuthor(newU);

        await SendEmbed(embed, guildId, GuildAuditLogEvent.AvatarUpdated);
    }

    public async Task HandleNicknameUpdated(IGuildUser oldU, IGuildUser newU, ulong guildId)
    {
        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, guildId);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {newU.Username} - {newU.Mention}");
        description.AppendLine($"> **{translator.Get<BotTranslator>().Id()}:** `{newU.Id}`");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().NicknameUpdated())
            .WithDescription(description.ToString())
            .AddField(
                translator.Get<GuildAuditTranslator>().Old(),
                string.IsNullOrEmpty(oldU.Nickname)
                    ? $"`{translator.Get<GuildAuditTranslator>().Empty()}`"
                    : oldU.Nickname,
                true
            )
            .AddField(
                translator.Get<GuildAuditTranslator>().New(),
                string.IsNullOrEmpty(newU.Nickname)
                    ? $"`{translator.Get<GuildAuditTranslator>().Empty()}`"
                    : newU.Nickname,
                true
            )
            .WithAuthor(newU);

        await SendEmbed(embed, guildId, GuildAuditLogEvent.NicknameUpdated);
    }

    public async Task HandleUserRolesUpdated(IGuildUser user, IReadOnlyCollection<SocketRole> roleOld,
        IReadOnlyCollection<SocketRole> roleNew, ulong guildId)
    {
        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, guildId);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {user.Username} - {user.Mention}");
        description.AppendLine($"> **{translator.Get<BotTranslator>().Id()}:** `{user.Id}`");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().RolesUpdated())
            .WithDescription(description.ToString())
            .WithAuthor(user);

        var addedRoles = roleNew.Except(roleOld).ToList();
        var removedRoles = roleOld.Except(roleNew).ToList();

        if (removedRoles.Count > 0)
            embed.AddField(
                translator.Get<GuildAuditTranslator>().Removed(),
                string.Join(" ", removedRoles.Select(x => x.Mention)),
                true
            );

        if (addedRoles.Count > 0)
            embed.AddField(
                translator.Get<GuildAuditTranslator>().Added(),
                string.Join(" ", addedRoles.Select(x => x.Mention)),
                true
            );

        if (addedRoles.Count + removedRoles.Count > 0)
            await SendEmbed(embed, guildId, GuildAuditLogEvent.UserRolesUpdated);
    }

    public async Task HandleUsernameUpdated(SocketUser oldU, SocketUser newU)
    {
        if (oldU.Username != newU.Username)
        {
            using var scope = serviceProvider.CreateScope();

            foreach (var guild in newU.MutualGuilds)
            {
                var translator = await TryGetTranslation(scope, guild.Id);

                if (translator == null)
                    return;

                StringBuilder description = new();
                description.AppendLine(
                    $"> **{translator.Get<BotTranslator>().User()}:** {newU.Username} - {newU.Mention}");
                description.AppendLine($"> **{translator.Get<BotTranslator>().Id()}:** `{newU.Id}`");

                var embed = new EmbedBuilder()
                    .WithTitle(translator.Get<GuildAuditTranslator>().UsernameUpdated())
                    .WithDescription(description.ToString())
                    .WithAuthor(newU);

                embed.AddField(
                    translator.Get<GuildAuditTranslator>().Old(),
                    oldU.Username,
                    true
                );
                embed.AddField(
                    translator.Get<GuildAuditTranslator>().New(),
                    newU.Username,
                    true
                );

                await SendEmbed(embed, guild.Id, GuildAuditLogEvent.UsernameUpdated);
            }
        }
    }

    private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        using var scope = serviceProvider.CreateScope();

        IGuildChannel guildChannel;
        try
        {
            guildChannel = (IGuildChannel)await client.GetChannelAsync(channel.Id);
            if (guildChannel == null)
                return;
        }
        catch (HttpException)
        {
            return;
        }

        if (await CheckForIgnoredChannel(guildChannel.GuildId, GuildAuditLogEvent.ReactionRemoved, guildChannel.Id))
            return;

        if (!reaction.User.IsSpecified)
            return;

        var translator = await TryGetTranslation(scope, guildChannel.GuildId);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {reaction.User.Value.Username} - {reaction.User.Value.Mention}");
        if (message.HasValue)
            description.AppendLine(
                $"> **{translator.Get<BotTranslator>().Message()}:** [{message.Id}]({message.Value.GetJumpUrl()})");
        else
            description.AppendLine($"> **{translator.Get<BotTranslator>().Message()}:** {message.Id}");
        description.AppendLine($"> **{translator.Get<GuildAuditTranslator>().Emote()}:** {reaction.Emote}");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().ReactionRemoved())
            .WithDescription(description.ToString())
            .WithAuthor(reaction.User.Value)
            .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {reaction.User.Value.Id}");

        await SendEmbed(embed, guildChannel.GuildId, GuildAuditLogEvent.ReactionRemoved);
    }

    private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        using var scope = serviceProvider.CreateScope();

        IGuildChannel guildChannel;
        try
        {
            guildChannel = (IGuildChannel)await client.GetChannelAsync(channel.Id);
            if (guildChannel == null)
                return;
        }
        catch (HttpException)
        {
            return;
        }

        if (await CheckForIgnoredChannel(guildChannel.GuildId, GuildAuditLogEvent.ReactionRemoved, guildChannel.Id))
            return;

        if (!reaction.User.IsSpecified)
            return;

        var translator = await TryGetTranslation(scope, guildChannel.GuildId);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {reaction.User.Value.Username} - {reaction.User.Value.Mention}");
        if (message.HasValue)
            description.AppendLine(
                $"> **{translator.Get<BotTranslator>().Message()}:** [{message.Id}]({message.Value.GetJumpUrl()})");
        else
            description.AppendLine($"> **{translator.Get<BotTranslator>().Message()}:** {message.Id}");
        description.AppendLine($"> **{translator.Get<GuildAuditTranslator>().Emote()}:** {reaction.Emote}");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().ReactionAdded())
            .WithDescription(description.ToString())
            .WithAuthor(reaction.User.Value)
            .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {reaction.User.Value.Id}");

        await SendEmbed(embed, guildChannel.GuildId, GuildAuditLogEvent.ReactionAdded);
    }

    private async Task HandleVoiceStateUpdated(SocketUser user, SocketVoiceState voiceStateBefore,
        SocketVoiceState voiceStateAfter)
    {
        using var scope = serviceProvider.CreateScope();

        if (voiceStateBefore.VoiceChannel == voiceStateAfter.VoiceChannel)
            return;

        GuildAuditLogEvent eventType;
        IVoiceChannel beforeChannel = null;
        IVoiceChannel afterChannel = null;
        ulong guildId;
        if (voiceStateBefore.VoiceChannel == null)
        {
            eventType = GuildAuditLogEvent.VoiceJoined;
            afterChannel = voiceStateAfter.VoiceChannel;
            guildId = afterChannel.GuildId;
        }
        else if (voiceStateAfter.VoiceChannel == null)
        {
            eventType = GuildAuditLogEvent.VoiceLeft;
            beforeChannel = voiceStateBefore.VoiceChannel;
            guildId = beforeChannel.GuildId;
        }
        else
        {
            eventType = GuildAuditLogEvent.VoiceMoved;
            beforeChannel = voiceStateBefore.VoiceChannel;
            afterChannel = voiceStateAfter.VoiceChannel;
            if (beforeChannel.GuildId != afterChannel.GuildId)
                return;
            guildId = beforeChannel.GuildId;
        }

        if (guildId == 0)
            return;

        var translator = await TryGetTranslation(scope, guildId);

        if (translator == null)
            return;

        var title = eventType switch
        {
            GuildAuditLogEvent.VoiceJoined => translator.Get<GuildAuditTranslator>().VoiceJoined(),
            GuildAuditLogEvent.VoiceLeft => translator.Get<GuildAuditTranslator>().VoiceLeft(),
            _ => translator.Get<GuildAuditTranslator>().MovedVoiceChannel()
        };
        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {user.Username} - {user.Mention}");

        if (eventType == GuildAuditLogEvent.VoiceJoined)
        {
            if (await CheckForIgnoredChannel(guildId, GuildAuditLogEvent.VoiceJoined, afterChannel.Id))
                return;
            description.AppendLine(
                $"> **{translator.Get<BotTranslator>().Channel()}:** {afterChannel.Name} - {afterChannel.Mention}");
        }
        else if (eventType == GuildAuditLogEvent.VoiceLeft)
        {
            if (await CheckForIgnoredChannel(guildId, GuildAuditLogEvent.VoiceLeft, beforeChannel.Id))
                return;
            description.AppendLine(
                $"> **{translator.Get<BotTranslator>().Channel()}:** {beforeChannel.Name} - {beforeChannel.Mention}");
        }
        else
        {
            if (await CheckForIgnoredChannel(guildId, GuildAuditLogEvent.VoiceMoved, beforeChannel.Id))
                return;
            if (await CheckForIgnoredChannel(guildId, GuildAuditLogEvent.VoiceMoved, afterChannel.Id))
                return;
            description.AppendLine(
                $"> **{translator.Get<GuildAuditTranslator>().ChannelBefore()}:** {beforeChannel.Name} - {beforeChannel.Mention}");
            description.AppendLine(
                $"> **{translator.Get<GuildAuditTranslator>().ChannelAfter()}:** {afterChannel.Name} - {afterChannel.Mention}");
        }

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description.ToString())
            .WithAuthor(user)
            .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {user.Id}");

        await SendEmbed(embed, guildId, eventType);
    }

    public async Task HandleBanAdded(SocketUser user, SocketGuild guild)
    {
        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, guild.Id);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {user.Username} - {user.Mention}");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().UserBanned())
            .WithDescription(description.ToString())
            .WithAuthor(user)
            .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {user.Id}");

        await SendEmbed(embed, guild.Id, GuildAuditLogEvent.BanAdded);
    }

    public async Task HandleBanRemoved(SocketUser user, SocketGuild guild)
    {
        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, guild.Id);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {user.Username} - {user.Mention}");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().UserUnbanned())
            .WithDescription(description.ToString())
            .WithAuthor(user)
            .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {user.Id}");

        await SendEmbed(embed, guild.Id, GuildAuditLogEvent.BanRemoved);
    }

    public async Task HandleInviteCreated(SocketInvite invite)
    {
        if (!invite.GuildId.HasValue)
            return;

        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, invite.GuildId.Value);

        if (translator == null)
            return;

        EmbedBuilder embed = new();

        StringBuilder description = new();
        description.AppendLine($"> **{translator.Get<GuildAuditTranslator>().Url()}:** {invite}");

        if (invite.Inviter != null)
        {
            description.AppendLine(
                $"> **{translator.Get<BotTranslator>().User()}:** {invite.Inviter.Username} - {invite.Inviter.Mention}");
            embed.WithAuthor(invite.Inviter)
                .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {invite.Inviter.Id}");
        }

        if (invite.Channel is ITextChannel tChannel)
            description.AppendLine(
                $"> **{translator.Get<GuildAuditTranslator>().TargetChannel()}:** {tChannel.Name} - {tChannel.Mention}");

        if (await CheckForIgnoredChannel(invite.GuildId.Value, GuildAuditLogEvent.InviteCreated, invite.ChannelId))
            return;

        embed.WithTitle(translator.Get<GuildAuditTranslator>().InviteCreated())
            .WithDescription(description.ToString());

        if (invite.MaxUses != 0)
            embed.AddField(translator.Get<GuildAuditTranslator>().MaxUses(), $"`{invite.MaxUses}`", true);

        if (invite.CreatedAt != default && invite.MaxAge != default)
            embed.AddField(translator.Get<GuildAuditTranslator>().ExpirationDate(),
                invite.CreatedAt.AddSeconds(invite.MaxAge).DateTime.ToDiscordTs(), true);

        await SendEmbed(embed, invite.GuildId.Value, GuildAuditLogEvent.InviteCreated);
    }

    public async Task HandleInviteDeleted(SocketGuildChannel channel, TrackedInvite invite)
    {
        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, channel.Guild.Id);

        if (translator == null)
            return;

        EmbedBuilder embed = new();

        StringBuilder description = new();
        description.AppendLine($"> **{translator.Get<GuildAuditTranslator>().Url()}:** {invite}");

        var inviter = channel.Guild.GetUser(invite.CreatorId);

        if (inviter != null)
        {
            description.AppendLine(
                $"> **{translator.Get<BotTranslator>().User()}:** {inviter.Username} - {inviter.Mention}");
            embed.WithAuthor(inviter)
                .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {inviter.Id}");
        }

        if (channel is ITextChannel tChannel)
            description.AppendLine(
                $"> **{translator.Get<GuildAuditTranslator>().TargetChannel()}:** {tChannel.Name} - {tChannel.Mention}");
        if (await CheckForIgnoredChannel(invite.GuildId, GuildAuditLogEvent.InviteCreated, channel.Id))
            return;

        embed.WithTitle(translator.Get<GuildAuditTranslator>().InviteDeleted())
            .WithDescription(description.ToString());

        await SendEmbed(embed, channel.Guild.Id, GuildAuditLogEvent.InviteDeleted);
    }

    public async Task HandleUserJoined(SocketGuildUser user)
    {
        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, user.Guild.Id);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {user.Username} - {user.Mention}");
        description.AppendLine($"> **{translator.Get<BotTranslator>().Id()}:** `{user.Id}`");
        description.AppendLine(
            $"> **{translator.Get<GuildAuditTranslator>().Registered()}:** {user.CreatedAt.DateTime.ToDiscordTs()}");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().UserJoined())
            .WithDescription(description.ToString())
            .WithAuthor(user)
            .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {user.Id}");

        await SendEmbed(embed, user.Guild.Id, GuildAuditLogEvent.UserJoined);
    }

    public async Task HandleUserRemoved(SocketGuild guild, SocketUser user)
    {
        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, guild.Id);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine(
            $"> **{translator.Get<BotTranslator>().User()}:** {user.Username} - {user.Mention}");
        description.AppendLine($"> **{translator.Get<BotTranslator>().Id()}:** `{user.Id}`");
        description.AppendLine(
            $"> **{translator.Get<GuildAuditTranslator>().Registered()}:** {user.CreatedAt.DateTime.ToDiscordTs()}");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().UserRemoved())
            .WithDescription(description.ToString())
            .WithAuthor(user)
            .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {user.Id}");

        await SendEmbed(embed, guild.Id, GuildAuditLogEvent.UserRemoved);
    }

    public async Task HandleMessageDeleted(Cacheable<IMessage, ulong> messageCached,
        Cacheable<IMessageChannel, ulong> channel)
    {
        var message = await messageCached.GetOrDownloadAsync();

        if (message?.Channel is ITextChannel textChannel)
        {
            if (await CheckForIgnoredChannel(textChannel.GuildId, GuildAuditLogEvent.MessageDeleted, textChannel.Id))
                return;

            if (message.Author is IGuildUser msgAuthor)
                if (await CheckForIgnoredRoles(textChannel.GuildId, GuildAuditLogEvent.MessageDeleted,
                        msgAuthor.RoleIds.ToList()))
                    return;

            using var scope = serviceProvider.CreateScope();

            var translator = await TryGetTranslation(scope, textChannel.GuildId);

            if (translator == null)
                return;

            EmbedBuilder embed = new();

            StringBuilder description = new();
            description.AppendLine(
                $"> **{translator.Get<BotTranslator>().Channel()}:** {message.Channel.Name} - {textChannel.Mention}");
            description.AppendLine(
                $"> **{translator.Get<BotTranslator>().Id()}:** [{message.Id}]({message.GetJumpUrl()})");

            if (message.Author != null)
            {
                description.AppendLine(
                    $"> **{translator.Get<BotTranslator>().Author()}:** {message.Author.Username} - {message.Author.Mention}");
                embed.WithAuthor(message.Author)
                    .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {message.Author.Id}");
            }

            if (message.CreatedAt != default)
                description.AppendLine(
                    $"> **{translator.Get<GuildAuditTranslator>().Created()}:** {message.CreatedAt.DateTime.ToDiscordTs()}");

            embed.WithTitle(translator.Get<GuildAuditTranslator>().MessageDeleted())
                .WithDescription(description.ToString());

            if (!string.IsNullOrEmpty(message.Content))
            {
                if (message.Content.Length > 1000)
                    for (var i = 0; i < 4; i++)
                    {
                        if (message.Content.Length > i * 1000)
                            embed.AddField(
                                $"{translator.Get<GuildAuditTranslator>().Content()} [{i + 1}]",
                                new string(message.Content.Skip(i * 1000).Take(1000).ToArray())
                            );
                    }
                else
                    embed.AddField(translator.Get<GuildAuditTranslator>().Content(), message.Content);
            }

            if (message.Attachments.Count > 0)
            {
                StringBuilder attachmentInfo = new();

                var counter = 1;

                foreach (var attachment in message.Attachments.Take(5))
                {
                    attachmentInfo.AppendLine(
                        $"- [{counter}. {translator.Get<BotTranslator>().Attachment()}]({attachment.Url})");
                    counter++;
                }

                if (message.Attachments.Count > 5)
                    attachmentInfo.AppendLine(translator.Get<BotTranslator>().AndXMore(message.Attachments.Count - 5));

                var attachInfo = attachmentInfo.ToString();

                embed.AddField(translator.Get<BotTranslator>().Attachments(),
                    attachInfo.Length > 1000 ? attachInfo.Take(1000) : attachInfo);
            }

            await SendEmbed(embed, textChannel.GuildId, GuildAuditLogEvent.MessageDeleted);
        }
    }

    public async Task HandleMessageSent(IMessage message)
    {
        if (!message.Author.IsBot && !message.Author.IsWebhook)
            if (message.Channel is ITextChannel textChannel)
            {
                if (await CheckForIgnoredChannel(textChannel.GuildId, GuildAuditLogEvent.MessageSent, textChannel.Id))
                    return;

                if (message.Author is IGuildUser msgAuthor)
                    if (await CheckForIgnoredRoles(textChannel.GuildId, GuildAuditLogEvent.MessageSent,
                            msgAuthor.RoleIds.ToList()))
                        return;

                using var scope = serviceProvider.CreateScope();

                var translator = await TryGetTranslation(scope, textChannel.GuildId);

                if (translator == null)
                    return;

                StringBuilder description = new();
                description.AppendLine(
                    $"> **{translator.Get<BotTranslator>().Channel()}:** {textChannel.Name} - {textChannel.Mention}");
                description.AppendLine(
                    $"> **{translator.Get<BotTranslator>().Id()}:** [{message.Id}]({message.GetJumpUrl()})");
                description.AppendLine(
                    $"> **{translator.Get<BotTranslator>().Author()}:** {message.Author.Username} - {message.Author.Mention}");

                var embed = new EmbedBuilder()
                    .WithTitle(translator.Get<GuildAuditTranslator>().MessageSent())
                    .WithDescription(description.ToString())
                    .WithAuthor(message.Author)
                    .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {message.Author.Id}");

                if (!string.IsNullOrEmpty(message.Content))
                {
                    if (message.Content.Length > 1000)
                        for (var i = 0; i < 4; i++)
                        {
                            if (message.Content.Length > i * 1000)
                                embed.AddField(
                                    $"{translator.Get<GuildAuditTranslator>().Content()} [{i + 1}]",
                                    new string(message.Content.Skip(i * 1000).Take(1000).ToArray())
                                );
                        }
                    else
                        embed.AddField(translator.Get<GuildAuditTranslator>().Content(), message.Content);
                }

                if (message.Attachments.Count > 0)
                {
                    StringBuilder attachmentInfo = new();

                    var counter = 1;

                    foreach (var attachment in message.Attachments.Take(5))
                    {
                        attachmentInfo.AppendLine(
                            $"- [{counter}. {translator.Get<BotTranslator>().Attachment()}]({attachment.Url})");
                        counter++;
                    }

                    if (message.Attachments.Count > 5)
                        attachmentInfo.AppendLine(translator.Get<BotTranslator>()
                            .AndXMore(message.Attachments.Count - 5));

                    var attachInfo = attachmentInfo.ToString();

                    embed.AddField(translator.Get<BotTranslator>().Attachments(),
                        attachInfo.Length > 1000 ? attachInfo.Take(1000) : attachInfo);
                }

                await SendEmbed(embed, textChannel.GuildId, GuildAuditLogEvent.MessageSent);
            }
    }

    public async Task HandleMessageUpdated(Cacheable<IMessage, ulong> messageBefore, SocketMessage messageAfter,
        ISocketMessageChannel channel)
    {
        if (messageAfter.Author.Id == 0)
            return;

        if (!messageAfter.Author.IsBot && !messageAfter.Author.IsWebhook)
            if (channel is ITextChannel textChannel)
            {
                if (await CheckForIgnoredChannel(textChannel.GuildId, GuildAuditLogEvent.MessageUpdated,
                        textChannel.Id))
                    return;

                if (messageAfter.Author is IGuildUser msgAuthor)
                    if (await CheckForIgnoredRoles(textChannel.GuildId, GuildAuditLogEvent.MessageUpdated,
                            msgAuthor.RoleIds.ToList()))
                        return;

                using var scope = serviceProvider.CreateScope();

                var translator = await TryGetTranslation(scope, textChannel.GuildId);

                if (translator == null)
                    return;

                StringBuilder description = new();
                description.AppendLine(
                    $"> **{translator.Get<BotTranslator>().Channel()}:** {textChannel.Name} - {textChannel.Mention}");
                description.AppendLine(
                    $"> **{translator.Get<BotTranslator>().Id()}:** [{messageAfter.Id}]({messageAfter.GetJumpUrl()})");
                description.AppendLine(
                    $"> **{translator.Get<BotTranslator>().Author()}:** {messageAfter.Author.Username} - {messageAfter.Author.Mention}");
                description.AppendLine(
                    $"> **{translator.Get<GuildAuditTranslator>().Created()}:** {messageAfter.CreatedAt.DateTime.ToDiscordTs()}");

                var embed = new EmbedBuilder()
                    .WithTitle(translator.Get<GuildAuditTranslator>().MessageUpdated())
                    .WithDescription(description.ToString())
                    .WithAuthor(messageAfter.Author)
                    .WithFooter($"{translator.Get<BotTranslator>().UserId()}: {messageAfter.Author.Id}")
                    .AddField(translator.Get<GuildAuditTranslator>().Pinned(), messageAfter.IsPinned ? Check : XCheck);

                var before = await messageBefore.GetOrDownloadAsync();

                if (before == null)
                    embed.AddField(translator.Get<GuildAuditTranslator>().Before(),
                        translator.Get<GuildAuditTranslator>().InformationNotCached());
                else if (string.Equals(before.Content, messageAfter.Content) &&
                         before.Embeds.Count != messageAfter.Embeds.Count)
                    return;
                else if (!string.Equals(before.Content, messageAfter.Content))
                    if (!string.IsNullOrEmpty(before.Content))
                        embed.AddField(translator.Get<GuildAuditTranslator>().Before(), before.Content.Truncate(1000));

                if (!string.IsNullOrEmpty(messageAfter.Content))
                {
                    if (messageAfter.Content.Length > 1000)
                        for (var i = 0; i < 4; i++)
                        {
                            if (messageAfter.Content.Length > i * 1000)
                                embed.AddField(
                                    $"{translator.Get<GuildAuditTranslator>().New()} [{i + 1}]",
                                    new string(messageAfter.Content.Skip(i * 1000).Take(1000).ToArray())
                                );
                        }
                    else
                        embed.AddField(translator.Get<GuildAuditTranslator>().New(), messageAfter.Content);
                }

                if (messageAfter.Attachments.Count > 0)
                {
                    StringBuilder attachmentInfo = new();

                    var counter = 1;

                    foreach (IAttachment attachment in messageAfter.Attachments.Take(5))
                    {
                        attachmentInfo.AppendLine(
                            $"- [{counter}. {translator.Get<BotTranslator>().Attachment()}]({attachment.Url})");
                        counter++;
                    }

                    if (messageAfter.Attachments.Count > 5)
                        attachmentInfo.AppendLine(translator.Get<BotTranslator>()
                            .AndXMore(messageAfter.Attachments.Count - 5));

                    var attachInfo = attachmentInfo.ToString();

                    embed.AddField(translator.Get<BotTranslator>().Attachments(),
                        attachInfo.Length > 1000 ? attachInfo.Take(1000) : attachInfo);
                }

                await SendEmbed(embed, textChannel.GuildId, GuildAuditLogEvent.MessageUpdated);
            }
    }

    public async Task HandleThreadCreated(SocketThreadChannel thread)
    {
        if (await CheckForIgnoredChannel(thread.Guild.Id, GuildAuditLogEvent.ThreadCreated, thread.ParentChannel.Id))
            return;

        using var scope = serviceProvider.CreateScope();

        var translator = await TryGetTranslation(scope, thread.Guild.Id);

        if (translator == null)
            return;

        StringBuilder description = new();
        description.AppendLine($"> **{translator.Get<BotTranslator>().Channel()}:** {thread.Name} - {thread.Mention}");
        description.AppendLine(
            $"> **{translator.Get<GuildAuditTranslator>().Parent()}:** {thread.ParentChannel.Name} - <#{thread.ParentChannel.Id}>");
        description.AppendLine($"> **{translator.Get<GuildAuditTranslator>().Creator()}:** <@{thread.Owner.Mention}>");

        var embed = new EmbedBuilder()
            .WithTitle(translator.Get<GuildAuditTranslator>().ThreadCreated())
            .WithDescription(description.ToString())
            .WithFooter($"{translator.Get<BotTranslator>().ChannelId()}: {thread.Id}");

        await SendEmbed(embed, thread.Guild.Id, GuildAuditLogEvent.ThreadCreated);
    }
}

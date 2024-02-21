using AutoMods.Data;
using AutoMods.Models;
using Bot.Abstractions;
using Bot.Data;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrivateVcs.Data;
using PrivateVcs.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace PrivateVcs.Services;

public class VcChecker(DiscordSocketClient client, IServiceProvider serviceProvider, ILogger<VcChecker> logger) : IEvent
{
    private readonly DiscordSocketClient _client = client;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<VcChecker> _logger = logger;

    // Channel Id, User Id
    private readonly ConcurrentDictionary<ulong, ulong> _privateVcCreators = new();

    public void RegisterEvents()
    {
        _client.ChannelUpdated += ChannelUpdated;
        _client.ChannelDestroyed += ChannelDestroyed;

        _client.Ready += async () =>
        {
            foreach (var guild in _client.Guilds)
                await CheckRemoveVCs(guild);
        };

        _client.UserVoiceStateUpdated += async (_, voiceChannel, _) => {
            if (voiceChannel.VoiceChannel is not null)
                await CheckRemoveVCs(voiceChannel.VoiceChannel.Guild);
        };
    }

    public async Task CheckRemoveVCs(SocketGuild guild)
    {
        _logger.LogInformation("Removing VCs for guild: '{GuildName}'", guild.Name);

        using var scope = _serviceProvider.CreateScope();

        var config = await scope.ServiceProvider.GetRequiredService<PrivateVcConfigRepository>()
            .SelectPrivateVcConfig(guild.Id);

        if (config is null)
            return;

        var categoryChannel = guild.GetCategoryChannel(config.PrivateCategoryId);

        if (categoryChannel == null)
            return;

        _logger.LogInformation("Found private vc category for guild: '{GuildName}'", guild.Name);

        var voiceChannels = categoryChannel.Guild.VoiceChannels
            .Where(check => check.CategoryId == config.PrivateCategoryId && check.Name != config.WaitingVcName);

        var voiceLobbyExists = false;

        foreach (var voiceChannel in voiceChannels)
        {
            var userCount = voiceChannel.ConnectedUsers.Count;

            _logger.LogInformation("Checking private VC '{ChannelName}' with user count: '{UserCount}'", voiceChannel.Name, userCount);

            if (userCount <= 0)
            {
                _logger.LogInformation("Deleting private VC: '{ChannelName}'", voiceChannel.Name);
                await voiceChannel.DeleteAsync();
            }
            else
            {
                voiceLobbyExists = true;
            }
        }

        if (!voiceLobbyExists && config.DeleteWaitingRoom)
        {
            var waitingLobby = categoryChannel.Guild.VoiceChannels
                .FirstOrDefault(check => check.Name == config.WaitingVcName);

            _logger.LogInformation("Deleting welcome lobby for: '{GuildName}'", guild.Name);

            if (waitingLobby != null)
                await waitingLobby.DeleteAsync();
        }
    }

    private Task ChannelDestroyed(SocketChannel arg)
    {
        if (_privateVcCreators.ContainsKey(arg.Id))
            if (_privateVcCreators.TryGetValue(arg.Id, out var value))
                _privateVcCreators.TryRemove(new KeyValuePair<ulong, ulong>(arg.Id, value));

        return Task.CompletedTask;
    }

    public void AddNewPrivateVc(ulong vcId, ulong creatorId) => _privateVcCreators.TryAdd(vcId, creatorId);

    public static bool IsNameAgainstFilter(string name, PrivateVcConfig privateVcConfig, AutoModConfig autoModConfig)
    {
        if (autoModConfig != null)
            foreach (var word in autoModConfig.CustomWordFilter.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                try
                {
                    if (Regex.IsMatch(name, word, RegexOptions.IgnoreCase))
                        return true;
                }
                catch (RegexParseException)
                {
                }
            }

        if (privateVcConfig != null)
            foreach (var word in privateVcConfig.ChannelFilterRegex.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                try
                {
                    if (Regex.IsMatch(name, word, RegexOptions.IgnoreCase))
                        return true;
                }
                catch (RegexParseException)
                {
                }
            }

        return false;
    }

    private async Task ChannelUpdated(SocketChannel _, SocketChannel newChannel)
    {
        if (newChannel is not SocketVoiceChannel voiceChannel)
            return;

        var guild = voiceChannel.Guild;
        
        using var scope = _serviceProvider.CreateScope();

        var config = await scope.ServiceProvider.GetRequiredService<PrivateVcConfigRepository>()
            .SelectPrivateVcConfig(guild.Id);

        var guildConfig = await scope.ServiceProvider.GetRequiredService<GuildConfigRepository>()
            .GetGuildConfig(guild.Id);

        var announcementChannel = guild.GetTextChannel(guildConfig.StaffAnnouncements);

        if (config is null)
            return;

        if (!voiceChannel.CategoryId.HasValue)
            return;

        if (voiceChannel.CategoryId != config.PrivateCategoryId)
            return;

        if (!_privateVcCreators.TryGetValue(voiceChannel.Id, out var userId))
            return;

        var creator = guild.GetUser(userId);

        var autoModConfigs = await scope.ServiceProvider.GetRequiredService<AutoModConfigRepository>()
            .GetConfigsByGuild(guild.Id);

        var autoMod = autoModConfigs.FirstOrDefault(a => a.AutoModType == AutoMods.Enums.AutoModType.CustomWordFilter);

        if (IsNameAgainstFilter(voiceChannel.Name, config, autoMod))
        {
            await announcementChannel.SendMessageAsync(embed:
                new EmbedBuilder()
                    .WithTitle("Punishable Private Vc Name")
                    .WithDescription(
                        $"{creator.Mention} tried to change their private vc name to something against the server's rules. " +
                        "The channel has since been deleted.")
                    .AddField("User", creator.Mention)
                    .AddField("Channel Name", voiceChannel.Name)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build()
            );

            var dm = await creator.CreateDMChannelAsync();

            await dm.SendMessageAsync(
                $"You are not allowed to change your private VC to the name: '{voiceChannel.Name}', " +
                "as it is against the server's terms of service. " +
                "The voice channel has since been deleted. " +
                "You may be contacted by moderators resulting from this."
            );

            await voiceChannel.DeleteAsync();
            return;
        }

        foreach (var authorized in voiceChannel.PermissionOverwrites
                     .Where(a => a.Permissions.MentionEveryone == PermValue.Allow)
                )
        {
            switch (authorized.TargetType)
            {
                case PermissionTarget.Role:
                {
                    var role = guild.GetRole(authorized.TargetId);
                    if (role.Permissions.MentionEveryone)
                        continue;
                    break;
                }
                case PermissionTarget.User:
                {
                    var user = guild.GetUser(authorized.TargetId);
                    if (user.GuildPermissions.MentionEveryone)
                        continue;
                    break;
                }
                default:
                    throw new InvalidDataException($"Unknown target type for private VCs of: {authorized.TargetType}");
            }

            IMentionable mentionable = authorized.TargetType switch
            {
                PermissionTarget.Role => guild.GetRole(authorized.TargetId),
                PermissionTarget.User => guild.GetUser(authorized.TargetId),
                _ => throw new InvalidDataException($"Unknown target type for private VCs of: {authorized.TargetType}")
            };

            await announcementChannel.SendMessageAsync(embed:
                new EmbedBuilder()
                    .WithTitle("Punishable Private Vc Permission")
                    .WithDescription(
                        $"{creator.Mention} tried to change their private vc to alloW everyone mention perms for the following role. " +
                        "The channel has since been deleted.")
                    .AddField("Unauthorized Member/Role", mentionable.Mention)
                    .AddField("Channel Name", voiceChannel.Name)
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp()
                    .Build()
            );

            var dm = await creator.CreateDMChannelAsync();

            await dm.SendMessageAsync(
                "You are not allowed to change your private VC to allow mentions to everyone, " +
                "as it is against the server's terms of service. " +
                "The voice channel has since been deleted. " +
                "You may be contacted by moderators resulting from this."
            );

            await voiceChannel.DeleteAsync();
            return;
        }
    }
}

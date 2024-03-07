using Bot.Abstractions;
using Bot.Enums;
using Bot.Events;
using Bot.Services;
using Discord;
using Discord.Net;
using Messaging.Data;
using Messaging.Enums;
using Messaging.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using Timer = System.Timers.Timer;

namespace Messaging.Services;

public class ScheduledMessages(ILogger<ScheduledMessages> logger, IServiceProvider serviceProvider,
    DiscordRest discordRest, BotEventHandler eventHandler) : IEvent
{
    private readonly DiscordRest _discordRest = discordRest;
    private readonly BotEventHandler _eventHandler = eventHandler;
    private readonly ILogger<ScheduledMessages> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public void RegisterEvents() => _eventHandler.OnBotLaunched += StartScheduledTimers;

    private async Task StartScheduledTimers()
    {
        Timer minuteEventTimer = new(TimeSpan.FromMinutes(1).TotalMilliseconds)
        {
            AutoReset = true,
            Enabled = true
        };

        minuteEventTimer.Elapsed += async (s, e) => await CheckDueScheduledMessages();

        await Task.Run(minuteEventTimer.Start);
    }

    public async Task CheckDueScheduledMessages()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<MessagingRepository>();
        var messages = await repo.GetDueMessages();

        foreach (var message in messages)
        {
            _logger.LogInformation(
                $"Handling scheduled message {message.Id} for {message.GuildId}/{message.ChannelId} by {message.CreatorId}/{message.LastEditedById}.");

            var guild = _discordRest.FetchGuildInfo(message.GuildId, CacheBehavior.OnlyCache);

            if (guild == null)
            {
                _logger.LogInformation($"Failed scheduled message {message.Id}. Reason unknown.");
                await repo.SetMessageAsFailed(message.Id, ScheduledMessageFailureReason.Unknown);
                continue;
            }

            var channel = await guild.GetTextChannelAsync(message.ChannelId);

            if (channel == null)
            {
                _logger.LogInformation($"Failed scheduled message {message.Id}. Reason channel not found.");
                await repo.SetMessageAsFailed(message.Id, ScheduledMessageFailureReason.ChannelNotFound);
                continue;
            }

            try
            {
                await channel.SendMessageAsync(message.Content.SanitizeMentions(), allowedMentions: AllowedMentions.None);
                await repo.SetMessageAsSent(message.Id);
                _logger.LogInformation(
                    $"Sent scheduled message {message.Id} for {message.GuildId}/{message.ChannelId} by {message.CreatorId}/{message.LastEditedById}.");
            }
            catch (HttpException e)
            {
                if (e.HttpCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    _logger.LogInformation($"Failed scheduled message {message.Id}. Reason insufficient permission.");
                    await repo.SetMessageAsFailed(message.Id, ScheduledMessageFailureReason.InsufficientPermission);
                }
                else
                {
                    _logger.LogInformation($"Failed scheduled message {message.Id}. Reason unknown");
                    await repo.SetMessageAsFailed(message.Id, ScheduledMessageFailureReason.Unknown);
                }
            }
            catch (Exception)
            {
                _logger.LogInformation($"Failed scheduled message {message.Id}. Reason unknown");
                await repo.SetMessageAsFailed(message.Id, ScheduledMessageFailureReason.Unknown);
            }
        }
    }
}

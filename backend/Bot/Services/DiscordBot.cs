using Bot.Abstractions;
using Bot.Data;
using Bot.Enums;
using Bot.Events;
using Bot.Exceptions;
using Bot.Extensions;
using Bot.Models;
using Bot.Translators;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot.Services;

public class DiscordBot(ILogger<DiscordBot> logger, DiscordSocketClient client, InteractionService interactions,
    IServiceProvider serviceProvider, BotEventHandler eventHandler, CachedServices cachedServices) : IHostedService, IEvent
{
    private readonly CachedServices _cachedServices = cachedServices;
    private readonly DiscordSocketClient _client = client;
    private readonly BotEventHandler _eventHandler = eventHandler;
    private readonly InteractionService _interactions = interactions;
    private readonly ILogger<DiscordBot> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private bool _firstReady = true;
    private bool _isRunning = false;
    private DateTime? _lastDisconnect = DateTime.UtcNow;

    public void RegisterEvents()
    {
        _client.JoinedGuild += GuildCreatedHandler;
        _client.GuildMemberUpdated += GuildUserUpdatedHandler;
        _client.UserLeft += GuildUserRemoved;
        _client.UserBanned += GuildBanAdded;
        _client.UserUnbanned += GuildBanRemoved;
        _client.ThreadCreated += ThreadCreatedHandler;

        _client.Connected += Connected;
        _client.Disconnected += Disconnected;
        _client.Ready += ReadyHandler;

        _client.InteractionCreated += HandleInteraction;

        _interactions.SlashCommandExecuted += CmdErrorHandler;

        var clientLogger = _serviceProvider.GetRequiredService<ILogger<DiscordSocketClient>>();

        _client.Log += logLevel => Log(logLevel, clientLogger);

        _client.JoinedGuild += JoinGuild;

        var interactionsLogger = _serviceProvider.GetRequiredService<ILogger<InteractionService>>();

        _interactions.Log += logLevel => Log(logLevel, interactionsLogger);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            foreach (var assembly in _cachedServices.Dependents)
                await _interactions.AddModulesAsync(assembly, scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Modules could not initialize!");
            return;
        }

        var settingsRepository = scope.ServiceProvider.GetRequiredService<SettingsRepository>();

        var config = await settingsRepository.GetAppSettings();

        await _client.LoginAsync(TokenType.Bot, config.DiscordBotToken);
        await _client.StartAsync();
        await _client.SetGameAsync(config.ServiceDomain, type: ActivityType.Watching);
    }

    public async Task StopAsync(CancellationToken cancellationToken) => await _client.LogoutAsync();

    private async Task HandleInteraction(SocketInteraction arg)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, arg);

            await _interactions.ExecuteCommandAsync(ctx, _serviceProvider);
        }
        catch (Exception)
        {
            _logger.LogInformation($"Unable to execute {arg.Type} in channel {arg.Channel}");

            if (arg.Type is InteractionType.ApplicationCommand)
                await arg.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
        }
    }

    public bool IsRunning() => _isRunning;

    public DateTime? GetLastDisconnectTime() => _lastDisconnect;

    public int GetLatency() => _client.Latency;

    private Task Connected()
    {
        _logger.LogCritical("Client connected.");
        _isRunning = true;

        return Task.CompletedTask;
    }

    private Task Disconnected(Exception _)
    {
        _logger.LogCritical("Client disconnected.");
        _isRunning = false;
        _lastDisconnect = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    private async Task ReadyHandler()
    {
        _logger.LogInformation("Client ready.");
        _isRunning = true;

        try
        {
            await _client.BulkOverwriteGlobalApplicationCommandsAsync([]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while overwriting global application commands.");
        }

        foreach (var guild in _client.Guilds)
        {
            try
            {
                await JoinGuild(guild);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Something went wrong while handling guild join for {guild.Id}.");
            }
        }

        if (_firstReady)
        {
            _firstReady = false;

            _eventHandler.BotLaunchedEvent.Invoke();
        }
    }

    private static Task Log(LogMessage logMessage, ILogger logger)
    {
        var level = logMessage.Severity switch
        {
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Debug => LogLevel.Debug,
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Warning => LogLevel.Warning,
            _ => throw new NotImplementedException()
        };

        if (logMessage.Exception is null)
            logger.Log(level, logMessage.Message);
        else
            logger.LogError(logMessage.Exception, logMessage.Message);

        return Task.CompletedTask;
    }

    private async Task JoinGuild(SocketGuild guild)
    {
        await _interactions.RegisterCommandsToGuildAsync(
            guild.Id
        );

        _logger.LogInformation($"Initialized guild commands for guild {guild.Name}.");
    }

    private Task GuildBanRemoved(SocketUser user, SocketGuild guild)
    {
        using var scope = _serviceProvider.CreateScope();

        // Refresh ban cachedServices
        var discordRest = scope.ServiceProvider.GetRequiredService<DiscordRest>();
        discordRest.RemoveFromCache(CacheKey.GuildBan(guild.Id, user.Id));

        return Task.CompletedTask;
    }

    private async Task GuildBanAdded(SocketUser user, SocketGuild guild)
    {
        using var scope = _serviceProvider.CreateScope();

        // Refresh ban cachedServices
        var discordRest = scope.ServiceProvider.GetRequiredService<DiscordRest>();
        await discordRest.GetGuildUserBan(guild.Id, user.Id, CacheBehavior.IgnoreCache);
        discordRest.RemoveFromCache(CacheKey.GuildUser(guild.Id, user.Id));

        // Refresh identity memberships
        var identityManager = scope.ServiceProvider.GetRequiredService<IdentityManager>();
        foreach (var identity in identityManager.GetCurrentIdentities()
                     .Where(identity => identity.GetCurrentUser().Id == user.Id))
            identity.RemoveGuildMembership(guild.Id);
    }

    private Task GuildUserRemoved(SocketGuild guild, SocketUser usr)
    {
        using var scope = _serviceProvider.CreateScope();

        // Refresh identity memberships
        var identityManager = scope.ServiceProvider.GetRequiredService<IdentityManager>();
        foreach (var identity in identityManager.GetCurrentIdentities()
                     .Where(identity => identity.GetCurrentUser().Id == usr.Id))
            identity.RemoveGuildMembership(guild.Id);

        return Task.CompletedTask;
    }

    private Task GuildUserUpdatedHandler(Cacheable<SocketGuildUser, ulong> oldUsrCached, SocketGuildUser newUsr)
    {
        using var scope = _serviceProvider.CreateScope();

        // Refresh identity memberships
        var identityManager = scope.ServiceProvider.GetRequiredService<IdentityManager>();
        foreach (var identity in identityManager.GetCurrentIdentities()
                     .Where(identity => identity.GetCurrentUser().Id == newUsr.Id))
            identity.UpdateGuildMembership(newUsr);

        // Refresh user cachedServices
        var discordRest = scope.ServiceProvider.GetRequiredService<DiscordRest>();
        discordRest.AddOrUpdateCache(CacheKey.GuildUser(newUsr.Id, newUsr.Id), new CacheApiResponse(newUsr));

        return Task.CompletedTask;
    }

    private static async Task ThreadCreatedHandler(SocketThreadChannel channel) => await channel.JoinAsync();

    private Task GuildCreatedHandler(SocketGuild guild)
    {
        _logger.LogInformation($"Joined guild '{guild.Name}' with ID: '{guild.Id}'");
        return Task.CompletedTask;
    }

    private async Task CmdErrorHandler(SlashCommandInfo info, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            using var scope = _serviceProvider.CreateScope();
            var translation = scope.ServiceProvider.GetRequiredService<Translation>();

            if (context.Guild != null)
                try
                {
                    await translation.SetLanguage(context.Guild.Id);
                }
                catch (UnregisteredGuildException)
                {
                    translation.SetLanguage(Language.En);
                }

            switch (result)
            {
                case ExecuteResult eResult:
                    {
                        if (eResult.Exception is ApiException exception)
                        {
                            var errorCode = "#" + ((int)exception.Error).ToString("D4");

                            await SendError(info, translation, context,
                                $"{translation.Get<BotEnumTranslator>().Enum(exception.Error)}: {exception.Message}",
                                errorCode);
                        }
                        else
                        {
                            await SendError(info, translation, context, result.ErrorReason, result.Error.Value.ToString());

                            _logger.LogError(
                            $"Command '{info.Name}' invoked by '{context.User.Username}' failed: " +
                            eResult.Exception.Message + "\n" + eResult.Exception.StackTrace);
                        }

                        _eventHandler.CommandErroredEvent.Invoke(eResult.Exception);
                        break;
                    }
                case PreconditionResult preResult:
                    await SendError(info, translation, context, preResult.ErrorReason, "PRECON");
                    break;
                default:
                    await SendError(info, translation, context, result.ErrorReason, result.Error.Value.ToString());

                    _logger.LogError(
                        $"Command '{info.Name}' ({result.GetType()}) invoked by '{context.User.Username}' failed due to {result.Error}: {result.ErrorReason}.");

                    _eventHandler.CommandErroredEvent.Invoke(
                        new Exception($"{result.ErrorReason}\nResult Type: {result.GetType()}"));
                    break;
            }
        }
    }

    private async Task SendError(ICommandInfo info, Translation translation, IInteractionContext context,
        string errorReason, string code)
    {
        _logger.LogError(
            $"Command '{info.Name}' invoked by '{context.User.Username}' failed: {errorReason}");

        var builder = new EmbedBuilder()
            .WithTitle(translation.Get<BotTranslator>().SomethingWentWrong())
            .WithColor(Color.Red)
            .WithDescription(errorReason)
            .WithCurrentTimestamp()
            .WithFooter($"{translation.Get<BotTranslator>().Code()} {code}");

        try
        {
            await context.Interaction.DeferAsync(true);
        }
        catch (Exception)
        {
        }

        try
        {
            try
            {
                await context.Interaction.ModifyOriginalResponseAsync(msg => msg.Embed = builder.Build());
            }
            catch (Exception)
            {
                await context.Interaction.FollowupAsync(embed: builder.Build());
            }
        }
        catch (Exception)
        {
            await context.Channel.SendMessageAsync(embed: builder.Build());
        }
    }
}

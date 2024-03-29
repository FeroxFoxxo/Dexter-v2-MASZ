using Bot.Abstractions;
using Bot.Data;
using Bot.Events;
using Bot.Extensions;
using Bot.Models;
using Discord;
using Discord.Net;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Bot.Services;

public class AuditLogger(DiscordSocketClient client, ILogger<AuditLogger> logger,
    IServiceProvider serviceProvider, BotEventHandler eventHandler) : IHostedService, IEvent
{
    private readonly DiscordSocketClient _client = client;
    private readonly StringBuilder _currentMessage = new();
    private readonly BotEventHandler _eventHandler = eventHandler;
    private readonly ILogger<AuditLogger> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private List<Module> _modules = [];

    public void RegisterEvents()
    {
        _client.Connected += OnBotReady;
        _client.Disconnected += OnDisconnect;
        _client.Log += async l => await OnLog(l.Exception);
        _eventHandler.OnCommandErroredEvent += OnLog;
    }

    public async Task StartAsync(CancellationToken _)
    {
        var config = await GetConfig();

        await QueueLog("======= STARTUP =======");
        await QueueLog("`Dexter` started!");
        await QueueLog("System time: " + DateTime.Now);
        await QueueLog("System time (UTC): " + DateTime.UtcNow);
        await QueueLog($"Language: `{config.DefaultLanguage}`");
        await QueueLog($"URL: `{config.GetServiceUrl()}`");
        await QueueLog($"Domain: `{config.ServiceDomain}`");
        await QueueLog($"Client ID: `{config.ClientId}`");

        await QueueLog(config.CorsEnabled ? "CORS support: ⚠ `ENABLED`" : "CORS support: `DISABLED`");

        await QueueLog("======= /STARTUP ========", true);
    }

    public async Task StopAsync(CancellationToken _) => await QueueLog("======= LOGOUT ========", true);

    public async Task QueueLog(string message, bool shouldExecute = false, bool shouldAppendTime = true)
    {
        if (shouldAppendTime)
            message = DateTime.UtcNow.ToDiscordTs() + " " + message[..Math.Min(message.Length, 1950)];

        if (_currentMessage.Length + message.Length <= 1998)
        {
            _currentMessage.AppendLine(message);

            if (shouldExecute)
                await ExecuteWebhook();
        }
        else
        {
            await ExecuteWebhook();
            _currentMessage.AppendLine(message);
        }
    }

    public async Task<AppSettings> GetConfig()
    {
        using var scope = _serviceProvider.CreateScope();

        var settingsRepository = scope.ServiceProvider.GetRequiredService<SettingsRepository>();

        return await settingsRepository.GetAppSettings();
    }

    private async Task ExecuteWebhook()
    {
        var config = await GetConfig();

        if (string.IsNullOrEmpty(config.AuditLogWebhookUrl))
            return;

        StringBuilder msg;

        lock (_currentMessage)
        {
            if (_currentMessage.Length <= 0)
                return;

            msg = new StringBuilder(_currentMessage.Length);
            msg.Append(_currentMessage);
            _currentMessage.Clear();
        }

        _logger.LogInformation("Executing audit log webhook.");

        try
        {
            if (!string.IsNullOrEmpty(msg.ToString()))
                await new DiscordWebhookClient(config.AuditLogWebhookUrl).SendMessageAsync(msg.ToString(),
                    allowedMentions: AllowedMentions.None);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing audit log webhook. ");
        }
    }

    public bool GetExceptionMessage(Exception e, out string description)
    {
        description = string.Empty;

        if (e == null)
            return false;

        if (e is GatewayReconnectException ||
            _modules.Any(x => e.GetType().Namespace.Contains(x.GetType().Namespace)) || e is HttpException)
            return false;

        description = Format.Sanitize(e.ToString());

        switch (e.InnerException)
        {
            case WebSocketException when e.Message.Contains("WebSocket connection was closed"):
                return false;
            case ExternalException ee:
                description = $"Error Code: {ee.ErrorCode}\n" + description;
                break;
            case WebSocketClosedException:
                return false;
            case HttpRequestException:
                return false;
        }

        description = description[..Math.Min(1000, description.Length)];
        return true;
    }

    private async Task OnLog(Exception e)
    {
        if (!GetExceptionMessage(e, out var description))
            return;

        await QueueLog("======= ERROR ENCOUNTERED =======", true);

        foreach (var ex in e.ToString().Replace("```", "").ChunksUpTo(1900))
            await QueueLog($"```\n{ex}\n```", true, false);

        await QueueLog("=================================", true);

        var config = await GetConfig();

        var embed = new EmbedBuilder()
            .WithTitle("Error Encountered")
            .WithDescription(description)
            .WithCurrentTimestamp()
            .WithColor(Color.Red)
            .WithFooter("View logs for more information");

        foreach (var admin in config.SiteAdmins)
        {
            var user = await _client.GetUserAsync(admin);
            var dmChannel = await user.CreateDMChannelAsync();

            embed.WithAuthor(user);

            await dmChannel.SendMessageAsync(embed: embed.Build());
        }
    }

    private async Task OnDisconnect(Exception _) => await QueueLog("Bot **disconnected** from discord sockets.", true);

    private async Task OnBotReady() =>
        await QueueLog($"Bot **connected** to `{_client.Guilds.Count} guild(s)` with `{_client.Latency}ms` latency.",
            true);

    public void SetModules(List<Module> modules) => _modules = modules;
}

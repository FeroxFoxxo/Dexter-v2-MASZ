using Bot.Abstractions;
using Bot.Models;
using Bot.Services;
using Fergun.Interactive;
using Lavalink4NET.Artwork;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Lavalink4NET.Integrations.SponsorBlock.Extensions;
using Lavalink4NET.Lyrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Music;

public class MusicModule : Module
{
    public const int Port = 2333;
    public const string Pass = "youshallnotpass";

    public override string[] Contributors { get; } = ["Swyreee", "Ferox"];

    public override void AddServices(IServiceCollection services, CachedServices cachedServices,
        AppSettings appSettings)
    {
        var host = GetMyIp().ToString();

        services
            .AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) })
            .AddSingleton<InteractiveService>()

            .AddSingleton<LyricsOptions>()
            .AddSingleton<LyricsService>()

            .AddSingleton<ArtworkService>()

            .ConfigureInactivityTracking(options =>
            {
                options.DefaultPollInterval = TimeSpan.FromMinutes(5);
                options.DefaultTimeout = TimeSpan.FromMinutes(5);
                options.UseDefaultTrackers = true;
            })
            .Configure<IdleInactivityTrackerOptions>(config => config.Timeout = TimeSpan.FromSeconds(10))
            .AddInactivityTracking()
            .AddSingleton<InactivityTrackingService>()

            .ConfigureLavalink(config =>
            {
                config.BaseAddress = new Uri($"http://{host}:{Port}");
                config.WebSocketUri = new Uri($"ws://{host}:{Port}/v4/websocket");
                config.ReadyTimeout = TimeSpan.FromSeconds(10);
                config.Passphrase = Pass;
            })
            .AddLavalink();
    }

    public override void ConfigureModules(List<Module> modules, WebApplication app) => app.UseSponsorBlock();

    public static IPAddress GetMyIp()
    {
        var services = new List<string>()
        {
            "https://ipv4.icanhazip.com",
            "https://api.ipify.org",
            "https://ipinfo.io/ip",
            "https://checkip.amazonaws.com",
            "https://wtfismyip.com/text",
            "http://icanhazip.com"
        };

        using var webclient = new HttpClient();

        foreach (var service in services)
            try
            {
                return IPAddress.Parse(webclient.GetStringAsync(service).Result);
            }
            catch { }

        return null;
    }
}

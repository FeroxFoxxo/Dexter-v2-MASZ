﻿using AspNetCoreRateLimit;
using Bot.Abstractions;
using Bot.Loggers;
using Bot.Middleware;
using Bot.Models;
using Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot;

public class BotModule : WebModule
{
    public override string[] Contributors { get; } = { "Zaanposni", "Ferox" };

    public override void AddLogging(ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddProvider(new LoggerProvider());
    }

    public override string[] AddAuthorizationPolicy() => new[] { "Cookies" };

    public override void AddPreServices(IServiceCollection services, CachedServices cachedServices,
        Action<DbContextOptionsBuilder> dbOption)
    {
        foreach (var type in cachedServices.GetClassTypes<IDataContextInitialize>())
        {
            type.GetMethod("AddContextToServiceProvider")
                .Invoke(null, new object[] { dbOption, services });
        }

        foreach (var type in cachedServices.GetClassTypes<Repository>())
            services.AddScoped(type);

        services.AddScoped<Translation>();

        foreach (var type in cachedServices.GetClassTypes<Translator>())
            services.AddScoped(type);

        services.AddSingleton(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Verbose,
            LogGatewayIntentWarnings = true,
            UseInteractionSnowflakeDate = false,
            AlwaysDownloadUsers = true,
            UseSystemClock = false,
            ConnectionTimeout = 24 * 60 * 60 * 1000,
            FormatUsersInBidirectionalUnicode = false,
            MessageCacheSize = 10240,
            GatewayIntents = (GatewayIntents)((int)GatewayIntents.AllUnprivileged | (int)GatewayIntents.GuildMembers |
                                              (int)GatewayIntents.GuildMessages | (int)GatewayIntents.DirectMessages |
                                              (1 << 15))
        });

        services.AddSingleton<DiscordSocketClient>();

        services.AddSingleton(new InteractionServiceConfig
        {
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Verbose
        });

        services.AddSingleton<InteractionService>();
    }

    public override void AddServices(IServiceCollection services, CachedServices cachedServices, AppSettings settings)
    {
        foreach (var type in cachedServices.GetClassTypes<INternalEventHandler>())
            services.AddSingleton(type);

        foreach (var type in cachedServices.GetClassTypes<IEvent>())
            services.AddSingleton(type);

        services.AddHostedService(s => s.GetRequiredService<DiscordBot>());
        services.AddHostedService(s => s.GetRequiredService<AuditLogger>());
        services.AddHostedService(s => s.GetRequiredService<DiscordRest>());

        services.AddSingleton<FilesHandler>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie("Cookies", options =>
            {
                options.LoginPath = "/api/v1/login";
                options.LogoutPath = "/api/v1/logout";
                options.ExpireTimeSpan = new TimeSpan(7, 0, 0, 0);
                options.Cookie.MaxAge = new TimeSpan(7, 0, 0, 0);
                options.Cookie.Name = "dexter_access_token";
                options.Cookie.HttpOnly = false;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.Headers["Location"] = context.RedirectUri;
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            })
            .AddDiscord(options =>
            {
                options.ClientId = settings.ClientId.ToString();
                options.ClientSecret = settings.ClientSecret;
                options.Scope.Add("guilds");
                options.Scope.Add("identify");
                options.SaveTokens = true;
                options.Prompt = "none";
                options.AccessDeniedPath = "/oauthfailed";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.HttpOnly = false;
            });

        if (settings.CorsEnabled)
            services.AddCors(o => o.AddPolicy("AngularDevCors", builder =>
            {
                builder.WithOrigins("http://127.0.0.1:4200")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }));

        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    public override void ConfigureServices(ConfigurationManager configuration, IServiceCollection services)
    {
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
    }

    public override void PostBuild(IServiceProvider services, CachedServices cachedServices)
    {
        foreach (var initEvent in cachedServices.GetInitializedClasses<IEvent>(services))
            initEvent.RegisterEvents();
    }

    public override void ConfigureModules(List<Module> modules, WebApplication app) =>
        app.Services.GetRequiredService<AuditLogger>().SetModules(modules);

    public override void PostWebBuild(WebApplication app, AppSettings settings)
    {
        if (settings.CorsEnabled)
            app.UseCors("AngularDevCors");

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                options.RoutePrefix = string.Empty;
            });
        }

        app.UseIpRateLimiting();

        app.UseMiddleware<HeaderMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ApiExceptionHandlingMiddleware>();
    }
}

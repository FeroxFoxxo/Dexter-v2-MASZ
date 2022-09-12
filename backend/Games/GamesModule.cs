using Bot.Abstractions;
using Bot.Models;
using Bot.Services;
using Games.Data;
using Games.Middleware;
using Games.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Games;

public class GamesModule : WebModule
{
	public override string[] Contributors { get; } = { "EnderFloof" };

	public override void AddServices(IServiceCollection services, CachedServices cachedServices, AppSettings appSettings)
	{
		services.AddSignalR()
			.AddJsonProtocol(opt =>
			{
				opt.PayloadSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
			});
		services.AddSingleton<GamesHub>();
		services.AddSingleton<GamesCache>();
	}

	public override void PostWebBuild(WebApplication application, AppSettings appSettings)
	{

	}
}
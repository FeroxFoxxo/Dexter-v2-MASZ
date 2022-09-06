using Bot.Abstractions;
using Bot.Models;
using Bot.Services;
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
		services.AddSignalR();
		services.AddSingleton<GamesHub>();
	}

	public override void PostWebBuild(WebApplication application, AppSettings appSettings)
	{
		application.UseEndpoints(endpoints => {
			endpoints.MapControllers();
			endpoints.MapHub<GamesHub>("/games");
		});
	}
}
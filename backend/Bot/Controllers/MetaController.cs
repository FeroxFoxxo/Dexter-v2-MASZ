using Bot.Abstractions;
using Bot.Data;
using Bot.Models;
using Bot.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bot.Controllers;

[Route("api/v1/meta")]
public class MetaController : BaseController
{
	private readonly DiscordRest _discordRest;
	private readonly SettingsRepository _settingsRepository;

	public MetaController(DiscordRest discordRest, SettingsRepository settingsRepository)
	{
		_discordRest = discordRest;
		_settingsRepository = settingsRepository;
	}

	[HttpGet("user")]
	public IActionResult GetBotUser()
	{
		return Ok(DiscordUser.FromUser(_discordRest.GetCurrentBotInfo()));
	}

	[HttpGet("embed")]
	public async Task<IActionResult> GetOEmbedInfo()
	{
		var appSettings = await _settingsRepository.GetAppSettings();

		return new ContentResult()
		{
			Content = appSettings.GetEmbedData(appSettings.GetServiceUrl()),
			ContentType = "text/html"
		};
	}

	[HttpGet("application")]
	public async Task<IActionResult> GetApplication()
	{
		return Ok(DiscordApplication.GetDiscordApplication(await _discordRest.GetCurrentApplicationInfo()));
	}
}
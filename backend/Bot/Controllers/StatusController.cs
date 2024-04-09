using Bot.Abstractions;
using Bot.Data;
using Bot.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Bot.Controllers;

[Route("api/v1")]
public class StatusController(SettingsRepository settingsRepository) : BaseController
{
    private readonly SettingsRepository _settingsRepository = settingsRepository;

    [HttpGet("status")]
    [HttpGet("health")]
    [HttpGet("healthcheck")]
    [HttpGet("ping")]
    public async Task<IActionResult> Status()
    {
        var config = await _settingsRepository.GetAppSettings();

        return !HttpContext.Request.Headers.TryGetValue("Accept", out var value)
            ? Ok("OK")
            : value.ToString().Search("application/json")
            ? Ok(new
            {
                status = "OK",
                lang = config.DefaultLanguage,
                name = config.GetServiceUrl(),
                server_time = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                server_time_utc = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
            })
            : (IActionResult)Ok("OK");
    }
}

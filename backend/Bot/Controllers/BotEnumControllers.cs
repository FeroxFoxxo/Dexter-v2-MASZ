using Bot.Abstractions;
using Bot.Enums;
using Bot.Services;
using Bot.Translators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bot.Controllers;

[Route("api/v1/enums")]
public class BotEnumControllers(Translation translator, ILogger<EnumController<BotEnumTranslator>> logger) : EnumController<BotEnumTranslator>(translator, logger)
{
    [HttpGet("apierror")]
    public async Task<IActionResult> ApiError([FromQuery] Language? language = null) =>
        await TranslateEnum<ApiError>(language);

    [HttpGet("editstatus")]
    public async Task<IActionResult> ViewEditStatus([FromQuery] Language? language = null) =>
        await TranslateEnum<EditStatus>(language);

    [HttpGet("language")]
    public async Task<IActionResult> LanguageEnum([FromQuery] Language? language = null) =>
        await TranslateEnum<Language>(language);

    [HttpGet("viewpermission")]
    public async Task<IActionResult> ViewPermissions([FromQuery] Language? language = null) =>
        await TranslateEnum<ViewPermission>(language);
}

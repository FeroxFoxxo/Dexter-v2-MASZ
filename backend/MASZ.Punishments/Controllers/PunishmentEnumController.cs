using MASZ.Bot.DTOs;
using MASZ.Bot.Enums;
using MASZ.Bot.Services;
using MASZ.Punishments.Enums;
using MASZ.Punishments.Translators;
using Microsoft.AspNetCore.Mvc;

namespace MASZ.Punishments.Controllers;

[ApiController]
[Route("api/v1/enums/")]
public class PunishmentEnumController : ControllerBase
{
	private readonly Translation _translator;

	public PunishmentEnumController(Translation translator)
	{
		_translator = translator;
	}

	[HttpGet("punishment")]
	public IActionResult Punishment([FromQuery] Language? language = null)
	{
		_translator.SetLanguage(language);

		var enums = new List<EnumDto>();

		foreach (var enumValue in Enum.GetValues<PunishmentType>())
			EnumDto.Create((int)enumValue, _translator.Get<PunishmentEnumTranslator>().Enum(enumValue));

		return Ok(enums);
	}

	[HttpGet("casecreationtype")]
	public IActionResult CreationType([FromQuery] Language? language = null)
	{
		_translator.SetLanguage(language);

		var enums = new List<EnumDto>();

		foreach (var enumValue in Enum.GetValues<CaseCreationType>())
			EnumDto.Create((int)enumValue, _translator.Get<PunishmentEnumTranslator>().Enum(enumValue));

		return Ok(enums);
	}

	[HttpGet("lockedcommentstatus")]
	public IActionResult ViewLockedCommentStatus([FromQuery] Language? language = null)
	{
		_translator.SetLanguage(language);

		var enums = new List<EnumDto>();

		foreach (var enumValue in Enum.GetValues<LockedCommentStatus>())
			EnumDto.Create((int)enumValue, _translator.Get<PunishmentEnumTranslator>().Enum(enumValue));

		return Ok(enums);
	}

	[HttpGet("markedtodeletestatus")]
	public IActionResult ViewMarkedToDeleteStatus([FromQuery] Language? language = null)
	{
		_translator.SetLanguage(language);

		var enums = new List<EnumDto>();

		foreach (var enumValue in Enum.GetValues<MarkedToDeleteStatus>())
			EnumDto.Create((int)enumValue, _translator.Get<PunishmentEnumTranslator>().Enum(enumValue));

		return Ok(enums);
	}

	[HttpGet("punishmentactivestatus")]
	public IActionResult ViewPunishmentActiveStatus([FromQuery] Language? language = null)
	{
		_translator.SetLanguage(language);

		var enums = new List<EnumDto>();

		foreach (var enumValue in Enum.GetValues<PunishmentActiveStatus>())
			EnumDto.Create((int)enumValue, _translator.Get<PunishmentEnumTranslator>().Enum(enumValue));

		return Ok(enums);
	}
}
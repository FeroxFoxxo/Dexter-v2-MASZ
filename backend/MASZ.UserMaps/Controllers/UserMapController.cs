using MASZ.Bot.Abstractions;
using MASZ.Bot.Enums;
using MASZ.Bot.Exceptions;
using MASZ.Bot.Services;
using MASZ.UserMaps.Data;
using MASZ.UserMaps.DTOs;
using MASZ.UserMaps.Views;
using Microsoft.AspNetCore.Mvc;

namespace MASZ.UserMaps.Controllers;

[Route("api/v1/guilds/{guildId}/usermap")]
public class UserMapController : AuthenticatedController
{
	private readonly UserMapRepository _userMapRepo;

	public UserMapController(IdentityManager identityManager, UserMapRepository userMapRepo) :
		base(identityManager, userMapRepo)
	{
		_userMapRepo = userMapRepo;
	}

	[HttpGet]
	public async Task<IActionResult> GetUserMap([FromRoute] ulong guildId)
	{
		var identity = await SetupAuthentication();

		await identity.RequirePermission(DiscordPermission.Moderator, guildId);

		var userMaps = await _userMapRepo.GetUserMapsByGuild(guildId);

		return Ok(userMaps.Select(x => new UserMapView(x)));
	}

	[HttpGet("{userId}")]
	public async Task<IActionResult> GetUserMaps([FromRoute] ulong guildId, [FromRoute] ulong userId)
	{
		var identity = await SetupAuthentication();

		await identity.RequirePermission(DiscordPermission.Moderator, guildId);

		var userMaps = await _userMapRepo.GetUserMapsByGuildAndUser(guildId, userId);

		return Ok(userMaps.Select(x => new UserMapView(x)));
	}

	[HttpPost]
	public async Task<IActionResult> CreateUserMap([FromRoute] ulong guildId, [FromBody] UserMapForCreateDto userMapDto)
	{
		var identity = await SetupAuthentication();

		await identity.RequirePermission(DiscordPermission.Moderator, guildId);

		try
		{
			await _userMapRepo.GetUserMap(guildId, userMapDto.UserA, userMapDto.UserB);
			throw new ResourceAlreadyExists();
		}
		catch (ResourceNotFoundException)
		{
		}

		var userMap =
			await _userMapRepo.CreateOrUpdateUserMap(guildId, userMapDto.UserA, userMapDto.UserB, userMapDto.Reason);

		return StatusCode(201, new UserMapView(userMap));
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> UpdateUserMap([FromRoute] ulong guildId, [FromRoute] int id,
		[FromBody] UserMapForUpdateDto userMapDto)
	{
		var identity = await SetupAuthentication();

		await identity.RequirePermission(DiscordPermission.Moderator, guildId);

		var userMap = await _userMapRepo.GetUserMap(id);

		if (userMap.GuildId != guildId)
			throw new ResourceNotFoundException();

		var result = await _userMapRepo.CreateOrUpdateUserMap(guildId, userMap.UserA, userMap.UserB, userMapDto.Reason);

		return Ok(new UserMapView(result));
	}


	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteUserMap([FromRoute] ulong guildId, [FromRoute] int id)
	{
		var identity = await SetupAuthentication();

		await identity.RequirePermission(DiscordPermission.Moderator, guildId);

		var userMap = await _userMapRepo.GetUserMap(id);

		if (userMap.GuildId != guildId)
			throw new ResourceNotFoundException();

		await _userMapRepo.DeleteUserMap(id);

		return Ok();
	}
}
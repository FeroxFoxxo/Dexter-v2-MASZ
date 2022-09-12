using Bot.Models;
using Discord;

namespace UserMaps.Models;

public class UserMapExpanded
{
	public UserMapExpanded(UserMap userMaps, IUser userA, IUser userB, IUser moderator)
	{
		UserMap = userMaps;
		UserA = DiscordUser.FromUser(userA);
		UserB = DiscordUser.FromUser(userB);
		Moderator = DiscordUser.FromUser(moderator);
	}

	public UserMap UserMap { get; set; }
	public DiscordUser UserA { get; set; }
	public DiscordUser UserB { get; set; }
	public DiscordUser Moderator { get; set; }
}
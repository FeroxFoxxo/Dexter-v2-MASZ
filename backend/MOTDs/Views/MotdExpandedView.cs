using Discord;
using Bot.Models;
using MOTDs.Models;

namespace MOTDs.Views;

public class MotdExpandedView
{
	public MotdExpandedView(GuildMotd punishmentsEvent, IUser creator)
	{
		Motd = new MotdView(punishmentsEvent);
		Creator = new DiscordUser(creator);
	}

	public MotdView Motd { get; set; }
	public DiscordUser Creator { get; set; }
}
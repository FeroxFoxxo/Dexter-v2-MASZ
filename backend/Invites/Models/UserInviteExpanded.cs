using Bot.Models;
using Discord;

namespace Invites.Models;

public class UserInviteExpanded
{
	public UserInviteExpanded(UserInvite userInvite, IUser invitedUser, IUser invitedBy)
	{
		UserInvite = userInvite;
		InvitedUser = DiscordUser.FromUser(invitedUser);
		InvitedBy = DiscordUser.FromUser(invitedBy);
	}

	public UserInvite UserInvite { get; set; }
	public DiscordUser InvitedUser { get; set; }
	public DiscordUser InvitedBy { get; set; }
}
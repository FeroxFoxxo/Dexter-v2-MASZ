using Discord;
using MASZ.Bot.Models;
using MASZ.UserNotes.Models;

namespace MASZ.Punishments.Models;

public class CaseExpanded
{
	public CaseExpanded(ModCase modCase, IUser moderator, IUser lastModerator, IUser suspect,
		List<ModCaseCommentExpanded> comments, UserNoteExpanded userNoteView)
	{
		ModCase = modCase;
		Moderator = new DiscordUser(moderator);
		LastModerator = new DiscordUser(lastModerator);
		Suspect = new DiscordUser(suspect);
		Comments = comments;
		UserNote = userNoteView;

		if (modCase.PunishedUntil == null) return;

		if (!(modCase.PunishedUntil > modCase.CreatedAt)) return;

		if (modCase.PunishedUntil < DateTime.UtcNow)
		{
			PunishmentProgress = 100;
		}
		else
		{
			var totalPunished = (modCase.PunishedUntil.Value - modCase.CreatedAt).TotalSeconds;
			var alreadyPunished = (DateTime.UtcNow - modCase.CreatedAt).TotalSeconds;

			PunishmentProgress = alreadyPunished / totalPunished * 100;
		}
	}

	public ModCase ModCase { get; set; }
	public DiscordUser Moderator { get; set; }
	public DiscordUser LastModerator { get; set; }
	public DiscordUser Suspect { get; set; }
	public DiscordUser LockedBy { get; set; }
	public DiscordUser DeletedBy { get; set; }
	public List<ModCaseCommentExpanded> Comments { get; set; }
	public UserNoteExpanded UserNote { get; set; }
	public double? PunishmentProgress { get; set; }

	public void RemoveModeratorInfo()
	{
		Moderator = null;
		LastModerator = null;
		LockedBy = null;
		DeletedBy = null;
		ModCase.RemoveModeratorInfo();

		foreach (var comment in Comments)
			comment.RemoveModeratorInfo(ModCase.UserId);
	}
}
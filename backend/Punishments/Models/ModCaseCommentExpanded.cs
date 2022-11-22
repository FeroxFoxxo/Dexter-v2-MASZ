using Bot.Models;
using Discord;

namespace Punishments.Models;

public class ModCaseCommentExpanded
{
    public ModCaseComment Comment { get; set; }
    public DiscordUser Commenter { get; set; }

    public ModCaseCommentExpanded(ModCaseComment comment, IUser commenter)
    {
        Comment = comment;
        Commenter = DiscordUser.GetDiscordUser(commenter);
    }

    public void RemoveModeratorInfo(ulong suspectId)
    {
        if (Comment.UserId == suspectId) return;

        Comment.UserId = default;
        Commenter = null;
    }
}

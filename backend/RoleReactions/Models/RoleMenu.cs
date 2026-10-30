using Microsoft.EntityFrameworkCore;

namespace RoleReactions.Models;

[PrimaryKey(nameof(GuildId), nameof(ChannelId), nameof(Id))]
public class RoleMenu
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public ulong MessageId { get; set; }
    public int MaximumRoles { get; set; }
    public Dictionary<ulong, ulong> RoleToPrerequesite { get; set; }
    public Dictionary<ulong, string> RoleToEmote { get; set; }
}

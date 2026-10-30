using Microsoft.EntityFrameworkCore;

namespace RoleReactions.Models;

[PrimaryKey(nameof(GuildId), nameof(ChannelId), nameof(Id), nameof(UserId))]
public class UserRoles
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int Id { get; set; }
    public ulong UserId { get; set; }
    public List<ulong> RoleIds { get; set; }
}

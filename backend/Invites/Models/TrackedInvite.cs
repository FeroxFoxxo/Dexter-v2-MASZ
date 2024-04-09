using Discord;

namespace Invites.Models;

public class TrackedInvite
{
    public TrackedInvite(IInviteMetadata invite, ulong guildId)
    {
        GuildId = guildId;
        Code = invite.Code;

        CreatorId = invite.Inviter is not null ? invite.Inviter.Id : 0;

        CreatedAt = invite.CreatedAt.GetValueOrDefault().UtcDateTime;
        Uses = invite.Uses.GetValueOrDefault();
        MaxUses = invite.MaxUses;

        try
        {
            TargetChannelId = invite.ChannelId;
        }
        catch (NullReferenceException)
        {
            TargetChannelId = 0;
        }

        ExpiresAt = invite.CreatedAt.GetValueOrDefault().UtcDateTime.AddSeconds(invite.MaxAge.GetValueOrDefault());
    }

    public TrackedInvite(ulong guildId, string vanityUrl, int uses)
    {
        GuildId = guildId;
        Code = vanityUrl;
        Uses = uses;
        CreatorId = 0;
        CreatedAt = null;
        MaxUses = null;
        TargetChannelId = 0;
        ExpiresAt = null;
    }

    public string Code { get; set; }
    public ulong CreatorId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int Uses { get; set; }
    public int? MaxUses { get; set; }
    public ulong TargetChannelId { get; set; }
    public ulong GuildId { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool IsExpired() => ExpiresAt != null && ExpiresAt < DateTime.UtcNow;

    public bool HasNewUses(int currentUses) => currentUses != Uses;
}

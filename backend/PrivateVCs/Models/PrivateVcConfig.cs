using PrivateVcs.DTOs;
using System.ComponentModel.DataAnnotations;

namespace PrivateVcs.Models;

public class PrivateVcConfig
{
    [Key] public ulong GuildId { get; set; }
    public string WaitingVcName { get; set; }
    public ulong PrivateCategoryId { get; set; }
    public ulong[] AllowedRoles { get; set; }
    public ulong[] CreatorRoles { get; set; }
    public string ChannelFilterRegex { get; set; }
    public bool DeleteWaitingRoom { get; set; }

    public PrivateVcConfig()
    {
    }

    public PrivateVcConfig(PrivateVcConfigDto dto, ulong guildId)
    {
        GuildId = guildId;
        WaitingVcName = dto.WaitingVcName;
        PrivateCategoryId = dto.PrivateCategoryId;
        AllowedRoles = dto.AllowedRoles;
        CreatorRoles = dto.CreatorRoles;
        ChannelFilterRegex = dto.ChannelFilterRegex;
        DeleteWaitingRoom = dto.DeleteWaitingRoom;
    }
}

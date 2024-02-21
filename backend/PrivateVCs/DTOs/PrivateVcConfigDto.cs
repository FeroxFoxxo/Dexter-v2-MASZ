namespace PrivateVcs.DTOs;

public class PrivateVcConfigDto
{
    public string WaitingVcName { get; set; }
    public ulong PrivateCategoryId { get; set; }
    public ulong[] AllowedRoles { get; set; }
    public ulong[] CreatorRoles { get; set; }
    public string ChannelFilterRegex { get; set; }
    public bool DeleteWaitingRoom { get; set; }
}

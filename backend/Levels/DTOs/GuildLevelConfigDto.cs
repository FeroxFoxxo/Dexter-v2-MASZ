namespace Levels.DTOs;

public class GuildLevelConfigDto
{
    public ulong Id { get; set; }

    public float[] Coefficients { get; set; }
    public int XpInterval { get; set; }
    public int MinimumTextXpGiven { get; set; }
    public int MaximumTextXpGiven { get; set; }
    public int MinimumVoiceXpGiven { get; set; }
    public int MaximumVoiceXpGiven { get; set; }
    public string LevelUpTemplate { get; set; }
    public ulong VoiceLevelUpChannel { get; set; }
    public ulong TextLevelUpChannel { get; set; }

    public ulong[] DisabledXpChannels { get; set; }
    public bool HandleRoles { get; set; }
    public bool SendTextLevelUps { get; set; }
    public bool SendVoiceLevelUps { get; set; }
    public bool VoiceXpCountMutedMembers { get; set; }
    public int VoiceXpRequiredMembers { get; set; }

    public ulong NicknameDisabledRole { get; set; }
    public ulong NicknameDisabledReplacement { get; set; }

    public Dictionary<int, ulong[]> Levels { get; set; }
    public Dictionary<int, string> LevelUpMessageOverrides { get; set; } 
}

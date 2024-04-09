using Discord;
using System.ComponentModel.DataAnnotations;

namespace Levels.Models;

public class GuildLevelConfig
{
    [Key] public ulong Id { get; set; }

    public ExperienceConfig Experience { get; set; }

    public float[] Coefficients { get; set; }
    public int XpInterval { get; set; }

    public string LevelUpTemplate { get; set; }

    public ulong VoiceLevelUpChannel { get; set; }
    public ulong TextLevelUpChannel { get; set; }

    public ulong[] DisabledXpChannels { get; set; }
    public bool HandleRoles { get; set; } = false;

    public bool SendTextLevelUps { get; set; }
    public bool SendVoiceLevelUps { get; set; }

    public bool VoiceXpCountMutedMembers { get; set; }
    public int VoiceXpRequiredMembers { get; set; }

    public ulong NicknameDisabledRole { get; set; }
    public ulong NicknameDisabledReplacement { get; set; }

    public Dictionary<int, ulong[]> Levels { get; set; }
    public Dictionary<int, string> LevelUpMessageOverrides { get; set; }
    public Dictionary<ulong, ExperienceConfig> ExperienceOverrides { get; set; }

    public GuildLevelConfig()
    {
    }

    public GuildLevelConfig(ulong guildId)
    {
        Id = guildId;

        Experience = new ExperienceConfig()
        {
            MaximumTextXpGiven = 0,
            MaximumVoiceXpGiven = 0,
            MinimumTextXpGiven = 0,
            MinimumVoiceXpGiven = 0
        };

        Coefficients = [0f, 75.83333f, 22.5f, 1.66667f];
        XpInterval = 60;
        LevelUpTemplate = "{USER} leveled up to level {LEVEL}!";

        DisabledXpChannels = [];

        HandleRoles = false;
        SendVoiceLevelUps = true;
        SendTextLevelUps = true;
        VoiceXpCountMutedMembers = true;
        VoiceXpRequiredMembers = 3;

        Levels = [];
        LevelUpMessageOverrides = [];
        ExperienceOverrides = [];
    }

    public ExperienceConfig GetExperienceConfig(IChannel channel)
    {
        Experience ??= new ExperienceConfig()
        {
            MaximumTextXpGiven = 0,
            MaximumVoiceXpGiven = 0,
            MinimumTextXpGiven = 0,
            MinimumVoiceXpGiven = 0
        };

        ExperienceOverrides ??= [];

        return ExperienceOverrides.TryGetValue(channel.Id, out var channelOverride) ? channelOverride : Experience;
    }
}

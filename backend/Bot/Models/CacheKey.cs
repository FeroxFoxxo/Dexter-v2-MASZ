namespace Bot.Models;

public class CacheKey
{
    private readonly string _key;

    private CacheKey(string key) => _key = key;

    public string GetValue() => _key;

    public static CacheKey User(ulong userId) => new($"u:{userId}");

    public static CacheKey Guild(ulong guildId) => new($"g:{guildId}");

    public static CacheKey GuildBans(ulong guildId) => new($"g:{guildId}:b");

    public static CacheKey GuildBan(ulong guildId, ulong userId) => new($"g:{guildId}:b:{userId}");

    public static CacheKey GuildUsers(ulong guildId) => new($"g:{guildId}:m");

    public static CacheKey GuildUser(ulong guildId, ulong userId) => new($"g:{guildId}:m:{userId}");

    public static CacheKey GuildChannels(ulong guildId) => new($"g:{guildId}:c");

    public static CacheKey DmChannel(ulong userId) => new($"c:{userId}");

    public static CacheKey TokenUser(string token) => new($"t:{token}");

    public static CacheKey TokenUserGuilds(string token) => new($"t:{token}:g");
}

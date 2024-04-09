using AutoMods.Models;
using Discord;
using Discord.WebSocket;

namespace AutoMods.MessageChecks;

public static class EmbedCheck
{
    public static bool Check(IMessage message, AutoModConfig config, DiscordSocketClient _) => config.Limit != null && message.Embeds != null && message.Embeds.Count > config.Limit;
}

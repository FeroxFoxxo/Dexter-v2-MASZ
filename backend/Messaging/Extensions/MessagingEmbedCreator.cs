using Bot.Enums;
using Bot.Extensions;
using Bot.Services;
using Bot.Translators;
using Discord;
using Messaging.Translators;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace Messaging.Extensions;

public static partial class MessagingEmbedCreator
{
    public static async Task<EmbedBuilder> CreateMessageSentEmbed(this IMessage message,
        ITextChannel channel, IUser user, IServiceProvider provider)
    {
        var translation = provider.GetRequiredService<Translation>();

        await translation.SetLanguage(channel.GuildId);

        var embed = await EmbedCreator.CreateActionEmbed(RestAction.Created, provider);

        embed.WithTitle(translation.Get<MessagingTranslator>().MessageSent())
            .WithAuthor(user)
            .WithDescription(translation.Get<MessagingTranslator>().SaySent(user, channel))
            .AddField(
                translation.Get<BotTranslator>().Channel(),
                channel.Mention,
                true
            ).AddField(
                translation.Get<BotTranslator>().Message(),
                message,
                true
            ).AddField(
                translation.Get<BotTranslator>().MessageUrl(), message.GetJumpUrl())
            .WithFooter($"{translation.Get<BotTranslator>().GuildId()}: {channel.GuildId}");

        return embed;
    }

    public static string StripMentions(this string message)
    {
        const string rolePinged = "**PLEASE DO NOT PING USERS VIA DEXTER**";

        var regex = RoleRegex();

        return
            regex.Replace(message, rolePinged)
            .Replace("@everyone", rolePinged)
            .Replace("@here", rolePinged);
    }

    [GeneratedRegex("<@&[0-9]+>")]
    private static partial Regex RoleRegex();
}

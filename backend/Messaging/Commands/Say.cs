using Bot.Abstractions;
using Bot.Attributes;
using Bot.Enums;
using Bot.Extensions;
using Bot.Translators;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Messaging.Extensions;
using Messaging.Translators;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Messaging.Commands;

public class Say : Command<Say>
{
    public IServiceProvider ServiceProvider { get; set; }
    public DiscordSocketClient Client { get; set; }

    public override async Task BeforeCommandExecute() => await DeferAsync(true);

    [Require(RequireCheck.GuildModerator)]
    [SlashCommand("say", "Let the bot send a message.")]
    public async Task SayCommand(
        [Summary("message", "message content the bot shall write")]
        string message,
        [Summary("channel", "channel to write the message in, defaults to current")]
        ITextChannel channel = null)
    {
        if (channel is null)
            if (Context.Channel is ITextChannel txtChannel)
            {
                channel = txtChannel;
            }
            else
            {
                await RespondInteraction(Translator.Get<BotTranslator>().OnlyTextChannel());
                return;
            }

        try
        {
            var createdMessage = await channel.SendMessageAsync(message.StripMentions());

            await RespondInteraction(Translator.Get<MessagingTranslator>().MessageSent());

            try
            {
                var embed = await createdMessage.CreateMessageSentEmbed(channel, Context.User, ServiceProvider);

                await Client.SendEmbed(GuildConfig.GuildId, GuildConfig.StaffLogs, embed);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Something went wrong while sending the internal notification for the say command by {Context.User.Id} in {Context.Guild.Id}/{Context.Channel.Id}.");
            }
        }
        catch (HttpException e)
        {
            if (e.HttpCode == HttpStatusCode.Unauthorized)
                await RespondInteraction(Translator.Get<BotTranslator>().CannotViewOrDeleteInChannel());
        }
        catch (Exception e)
        {
            Logger.LogError(e, $"Error while writing message in channel {channel.Id}");
            await RespondInteraction(Translator.Get<MessagingTranslator>().FailedToSend());
        }
    }
}

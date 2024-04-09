using Bot.Abstractions;
using Bot.Attributes;
using Bot.Enums;
using Bot.Translators;
using Discord;
using Discord.Interactions;
using Discord.Net;
using System.Net;
using Utilities.Enums;
using Utilities.Translators;

namespace Utilities.Commands;

public class Cleanup : Command<Cleanup>
{
    public override async Task BeforeCommandExecute() =>
        await Context.Interaction.DeferAsync(!GuildConfig.StaffChannels.Contains(Context.Channel.Id));

    [Require(RequireCheck.GuildModerator)]
    [SlashCommand("cleanup", "Cleanup specific data from the server and/or channel.")]
    public async Task CleanupCommand(
        [Summary("mode", "The data you wish to clean up.")]
        CleanupMode cleanupMode,
        [Summary("user", "The user to filter the cleanup to.")]
        IUser filterUser = null,
        [Summary("channel", "The channel to delete in, defaults to current.")]
        ITextChannel channel = null,
        [Summary("count", "The amount of messages to delete, defaults to 100.")]
        long count = 100)
    {
        if (cleanupMode == CleanupMode.Messages && filterUser == null)
        {
            await RespondInteraction("I can't clean up all these messages! Please provide a filter...");
            return;
        }

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

        if (count > 1000)
            count = 1000;

        var func = cleanupMode switch
        {
            CleanupMode.Bots => IsFromBot,
            CleanupMode.Attachments => HasAttachment,
            _ => new Func<IMessage, bool>(_ => true)
        };

        int deleted;

        try
        {
            deleted = await IterateAndDeleteChannels(channel, (int)count, func, Context.User, filterUser);
        }
        catch (HttpException ex)
        {
            if (ex.HttpCode == HttpStatusCode.Forbidden)
                await RespondInteraction(Translator.Get<BotTranslator>().CannotViewOrDeleteInChannel());
            else if (ex.HttpCode == HttpStatusCode.Forbidden)
                await RespondInteraction(Translator.Get<BotTranslator>().CannotFindChannel());

            return;
        }

        await RespondInteraction(Translator.Get<UtilityTranslator>().DeletedMessages(deleted, channel));
    }

    private static bool HasAttachment(IMessage m) => m.Attachments.Count > 0;

    private static bool IsFromBot(IMessage m) => m.Author.IsBot;

    private static async Task<int> IterateAndDeleteChannels(ITextChannel channel, int limit,
        Func<IMessage, bool> predicate, IUser currentActor, IUser filterUser = null)
    {
        var latestMessage = await channel.GetMessagesAsync(1).FirstOrDefaultAsync();

        if (latestMessage?.FirstOrDefault() is null)
            return 0;

        var lastId = latestMessage.First().Id;
        var deleted = 0;

        while (limit > 0)
        {
            var messages = channel.GetMessagesAsync(lastId, Direction.Before, Math.Min(limit, 100));
            var toDelete = new List<IMessage>();

            if (await messages.CountAsync() == 0)
                break;
            foreach (var message in await messages.FlattenAsync())
            {
                lastId = message.Id;
                limit--;

                if (filterUser != null && message.Author.Id != filterUser.Id)
                    continue;

                if (!predicate(message))
                    continue;

                deleted++;
                if (message.CreatedAt.UtcDateTime.AddDays(14) > DateTime.UtcNow)
                    toDelete.Add(message);
                else
                    await message.DeleteAsync();
            }

            switch (toDelete.Count)
            {
                case >= 2:
                    {
                        RequestOptions options = new()
                        {
                            AuditLogReason =
                                $"Bulkdelete by {currentActor.Username} ({currentActor.Id})."
                        };

                        await channel.DeleteMessagesAsync(toDelete, options);
                        toDelete.Clear();
                        break;
                    }
                case > 0:
                    await toDelete.First().DeleteAsync();
                    toDelete.Clear();
                    break;
            }
        }

        return deleted;
    }
}

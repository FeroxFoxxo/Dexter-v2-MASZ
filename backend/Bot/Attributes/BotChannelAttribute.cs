using Bot.Data;
using Bot.Exceptions;
using Bot.Services;
using Bot.Translators;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Attributes;

public class BotChannelAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context,
        ICommandInfo commandInfo, IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var guildConfig = await scope.ServiceProvider
            .GetService<GuildConfigRepository>()
            ?.GetGuildConfig(context.Guild.Id)!;

        var translator = scope.ServiceProvider.GetService<Translation>() ?? throw new ResourceNotFoundException();

        translator.SetLanguage(guildConfig);

        var channels = await context.Guild.GetTextChannelsAsync();

        return !guildConfig.BotChannels.Contains(context.Channel.Id)
            ? PreconditionResult.FromError(
                new UnauthorizedException($"{
                    translator.Get<BotTranslator>().OnlyBotChannel()
                    } {
                    string.Join(", ", channels.Where(c => guildConfig.BotChannels.Contains(c.Id)).Select(x => x.Mention)
                    )}.")
                )
            : PreconditionResult.FromSuccess();
    }
}

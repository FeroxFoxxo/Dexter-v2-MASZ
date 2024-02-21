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

        var roles = context.Guild.Roles.Where(r => guildConfig.BotChannels.Contains(r.Id)).Select(x => x.Mention);

        return !guildConfig.BotChannels.Contains(context.Channel.Id)
            ? PreconditionResult.FromError(
                new UnauthorizedException($"{translator.Get<BotTranslator>().OnlyBotChannel()} {string.Join(", ", roles)}."))
            : PreconditionResult.FromSuccess();
    }
}

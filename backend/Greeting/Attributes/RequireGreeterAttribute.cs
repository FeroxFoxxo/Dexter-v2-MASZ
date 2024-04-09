using Bot.Exceptions;
using Discord;
using Discord.Interactions;
using Greeting.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Greeting.Attributes;

internal class RequireGreeterAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context,
        ICommandInfo commandInfo, IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var greetConfig = await scope.ServiceProvider
            .GetService<GreeterDatabase>()
            .GreeterConfigs.FindAsync(context.Guild.Id);

        if (greetConfig == null)
            return PreconditionResult.FromError("This guild has not been set up to use the greeting module!");

        var gUser = await context.Guild.GetUserAsync(context.User.Id);

        var gRoles = greetConfig.AllowedGreetRoles.Select(c => context.Guild.Roles.FirstOrDefault(c2 => c2.Id == c))
            .Where(c => c != null);
        var roleNames = string.Join(", ", gRoles.Select(c => c.Name));

        var gChannelTotal = await context.Guild.GetChannelsAsync();
        var gChannels = greetConfig.AllowedGreetChannels.Select(c => gChannelTotal.FirstOrDefault(c2 => c2.Id == c))
            .Where(c => c != null);
        var channelNames = string.Join(", ", gChannels.Select(c => c.Name));

        return !greetConfig.AllowedGreetChannels.Contains(context.Channel.Id)
            ? PreconditionResult.FromError(
                new UnauthorizedException($"This command can only be used in the `{channelNames}` channel(s)!"))
            : !greetConfig.AllowedGreetRoles.Any(r => gUser.RoleIds.Contains(r))
            ? PreconditionResult.FromError(
                new UnauthorizedException($"This command can only be run by users with the `{roleNames}` role(s)!"))
            : PreconditionResult.FromSuccess();
    }
}

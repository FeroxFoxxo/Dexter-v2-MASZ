﻿using Discord.Interactions;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using RoleReactions.Data;

namespace RoleReactions.Abstractions;

public class MenuHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var value = autocompleteInteraction.Data.Current.Value as string;

        var database = services.GetRequiredService<RoleReactionsDatabase>();

        var menus = database.RoleReactionsMenu.Where(
            x => x.GuildId == context.Guild.Id &&
            x.ChannelId == context.Channel.Id 
        );

        var selectedMenus = menus.Where(x => x.Name.Contains(value))
            .Select(x => new AutocompleteResult(x.Name, x.Id))
            .ToArray();

        return Task.FromResult(AutocompletionResult.FromSuccess(selectedMenus.Take(25)));
    }
}

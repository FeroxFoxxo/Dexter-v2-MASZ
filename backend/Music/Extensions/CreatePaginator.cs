using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace Music.Extensions;

public static class CreatePaginator
{
    public static async Task SendPaginator(this InteractiveService interactive, IEnumerable<PageBuilder> pages,
        IInteractionContext context)
    {
        var pageBuilders = pages as PageBuilder[] ?? pages.ToArray();

        switch (pageBuilders.Length)
        {
            case > 1:
                {
                    Paginator paginator = new StaticPaginatorBuilder()
                        .WithPages(pageBuilders)
                        .WithDefaultEmotes()
                        .AddUser(context.User)
                        .WithFooter(PaginatorFooter.PageNumber)
                        .WithActionOnCancellation(ActionOnStop.DeleteInput)
                        .WithActionOnTimeout(ActionOnStop.DeleteInput)
                        .Build();

                    await interactive.SendPaginatorAsync(paginator, context.Interaction,
                        responseType: InteractionResponseType.DeferredChannelMessageWithSource);
                    break;
                }
            case 1:
                await context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "";
                    x.Embed = pageBuilders.First()?.Build().Embed;
                });
                break;
        }
    }
}

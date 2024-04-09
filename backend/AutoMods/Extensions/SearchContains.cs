using AutoMods.Models;
using Bot.Extensions;

namespace AutoMods.Extensions;

public static class SearchContains
{
    public static bool Search(this string search, AutoModEventExpanded obj) =>
        obj != null
&& (search.Search(obj.AutoModEvent) ||
              search.Search(obj.Suspect));

    public static bool Search(this string search, AutoModEvent obj) =>
        obj != null
&& (search.Search(obj.AutoModAction.ToString()) ||
              search.Search(obj.AutoModType.ToString()) ||
              search.Search(obj.CreatedAt) ||
              search.Search(obj.Username) ||
              search.Search(obj.Nickname) ||
              search.Search(obj.UserId) ||
              search.Search(obj.MessageContent) ||
              search.Search(obj.MessageId));
}

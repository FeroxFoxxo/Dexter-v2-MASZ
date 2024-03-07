using System.Text.RegularExpressions;

namespace Messaging.Extensions;

public static partial class MentionSanitization
{
    public static string SanitizeMentions(this string input)
    {
        input = HereMention().Replace(input, $"@{ZWSP}here");
        input = EveryoneMention().Replace(input, $"@{ZWSP}everyone");
        input = RoleMention().Replace(input, $"<@{ZWSP}&");
        return input;
    }

    [GeneratedRegex(@"@everyone", RegexOptions.IgnoreCase, "en-NZ")]
    private static partial Regex EveryoneMention();

    [GeneratedRegex(@"@here", RegexOptions.IgnoreCase, "en-NZ")]
    private static partial Regex HereMention();

    [GeneratedRegex(@"<@&")]
    private static partial Regex RoleMention();

    public const char ZWSP = '​';
}

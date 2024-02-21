using Bot.Enums;

namespace Bot.Abstractions;

public abstract class Translator(Language preferredLanguage = Language.En)
{
    public Language PreferredLanguage { get; set; } = preferredLanguage;
}

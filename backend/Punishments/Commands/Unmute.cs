using Bot.Abstractions;
using Bot.Attributes;
using Bot.Data;
using Bot.Enums;
using Bot.Extensions;
using Bot.Translators;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Humanizer;
using Punishments.Data;
using Punishments.Enums;
using Punishments.Translators;
using System.Globalization;
using System.Text;

namespace Punishments.Commands;

public class Unmute : Command<Unmute>
{
    public ModCaseRepository ModCaseRepository { get; set; }
    public SettingsRepository SettingsRepository { get; set; }

    [Require(RequireCheck.GuildModerator, RequireCheck.GuildStrictModeMute)]
    [SlashCommand("unmute", "unmute a user by deactivating all his mute mod-cases.")]
    public async Task UnmuteCommand([Summary("user", "User to unmute")] IUser user)
    {
        ModCaseRepository.AsUser(Identity);

        var modCases = (await ModCaseRepository.GetCasesForGuildAndUser(Context.Guild.Id, user.Id))
            .Where(x => x.PunishmentActive && x.PunishmentType == PunishmentType.Mute).ToList();

        if (modCases.Count == 0)
        {
            await RespondInteraction(Translator.Get<PunishmentTranslator>().NoActiveModCases());
            return;
        }

        StringBuilder interactionString = new();
        interactionString.AppendLine(Translator.Get<PunishmentTranslator>().FoundCasesForUnmute(modCases.Count));

        var config = await SettingsRepository.GetAppSettings();

        foreach (var modCase in modCases.Take(5))
        {
            var truncate = 50;

            if (modCase.PunishedUntil != null)
                truncate = 30;

            interactionString.Append($"- [#{modCase.CaseId} - {modCase.Title.Truncate(truncate)}]");
            interactionString.Append($"({config.GetServiceUrl()}/guilds/{modCase.GuildId}/cases/{modCase.CaseId})");

            if (modCase.PunishedUntil != null)
                interactionString.Append(
                    $" {Translator.Get<BotTranslator>().Until()} {modCase.PunishedUntil.Value.ToDiscordTs()}");

            interactionString.AppendLine();
        }

        if (modCases.Count > 5)
            interactionString.AppendLine(Translator.Get<BotTranslator>().AndXMore(modCases.Count - 5));

        var embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithDescription(interactionString.ToString())
            .WithColor(Color.Orange);

        embed.AddField(Translator.Get<PunishmentTranslator>().Result(),
            Translator.Get<PunishmentTranslator>().WaitingForApproval());

        var buttons = new ComponentBuilder()
            .WithButton(Translator.Get<PunishmentTranslator>().DeleteMutes(), $"unmute-delete:{user.Id}")
            .WithButton(Translator.Get<PunishmentTranslator>().DeactivateMutes(), $"unmute-deactivate:{user.Id}",
                ButtonStyle.Secondary)
            .WithButton(Translator.Get<PunishmentTranslator>().Cancel(), "unmute-cancel", ButtonStyle.Danger);

        await RespondInteraction(string.Empty, embed, buttons);
    }

    [ComponentInteraction("unmute-delete:*")]
    public async Task DeleteMuteConfirmation(string userId)
    {
        ModCaseRepository.AsUser(Identity);

        var modCases = (await ModCaseRepository.GetCasesForGuildAndUser(Context.Guild.Id, Convert.ToUInt64(userId)))
            .Where(x => x.PunishmentActive && x.PunishmentType == PunishmentType.Mute).ToList();

        foreach (var modCase in modCases)
            await ModCaseRepository.DeleteModCase(modCase.GuildId, modCase.CaseId);

        if (Context.Interaction is SocketMessageComponent castInteraction)
        {
            var embed = castInteraction.Message.Embeds.FirstOrDefault().ToEmbedBuilder()
                .WithColor(new Color(Convert.ToUInt32(int.Parse("7289da", NumberStyles.HexNumber))));

            embed.Fields =
            [
                new EmbedFieldBuilder().WithName(Translator.Get<PunishmentTranslator>().Result())
                    .WithValue(Translator.Get<PunishmentTranslator>().MutesDeleted())
            ];

            await RespondInteraction(string.Empty, embed);
        }
    }

    [ComponentInteraction("unmute-deactivate:*")]
    public async Task DeactivateMute(string userId)
    {
        ModCaseRepository.AsUser(Identity);

        var modCases = (await ModCaseRepository.GetCasesForGuildAndUser(Context.Guild.Id, Convert.ToUInt64(userId)))
            .Where(x => x.PunishmentActive && x.PunishmentType == PunishmentType.Mute).ToList();

        await ModCaseRepository.DeactivateModCase([.. modCases]);

        if (Context.Interaction is SocketMessageComponent castInteraction)
        {
            var embed = castInteraction.Message.Embeds.FirstOrDefault().ToEmbedBuilder().WithColor(Color.Green);

            embed.Fields =
            [
                new EmbedFieldBuilder().WithName(Translator.Get<PunishmentTranslator>().Result())
                    .WithValue(Translator.Get<PunishmentTranslator>().MutesDeactivated())
            ];

            await RespondInteraction(string.Empty, embed);
        }
    }

    [ComponentInteraction("unmute-cancel")]
    public async Task UnmuteCancel()
    {
        if (Context.Interaction is SocketMessageComponent castInteraction)
        {
            var embed = castInteraction.Message.Embeds.FirstOrDefault().ToEmbedBuilder().WithColor(Color.Red);

            embed.Fields =
            [
                new EmbedFieldBuilder().WithName(Translator.Get<PunishmentTranslator>().Result())
                    .WithValue(Translator.Get<PunishmentTranslator>().Canceled())
            ];

            await RespondInteraction(string.Empty, embed);
        }
    }
}

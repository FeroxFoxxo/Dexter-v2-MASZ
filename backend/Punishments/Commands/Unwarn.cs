using Bot.Abstractions;
using Bot.Attributes;
using Bot.Data;
using Bot.Enums;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Punishments.Data;
using Punishments.Enums;
using Punishments.Translators;
using System.Globalization;

namespace Punishments.Commands;

public class Unwarn : Command<Unwarn>
{
    public ModCaseRepository ModCaseRepository { get; set; }
    public SettingsRepository SettingsRepository { get; set; }

    [Require(RequireCheck.GuildModerator)]
    [SlashCommand("unwarn", "unwarn a user by removing their last warning.")]
    public async Task UnwarnCommand([Summary("user", "User to unwarn")] IUser user)
    {
        ModCaseRepository.AsUser(Identity);

        var modCases = (await ModCaseRepository.GetCasesForGuildAndUser(Context.Guild.Id, user.Id))
            .Where(x => x.PunishmentActive && x.PunishmentType == PunishmentType.Warn).ToList();

        if (modCases.Count == 0)
        {
            await RespondInteraction(Translator.Get<PunishmentTranslator>().NoActiveModCases());
            return;
        }

        var mCase = modCases.FirstOrDefault();

        var embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithDescription($"Found last warning of `{mCase.Title}` for {mCase.Username}. Do you want to delete the warning?")
            .WithColor(Color.Orange);

        embed.AddField(Translator.Get<PunishmentTranslator>().Result(),
            Translator.Get<PunishmentTranslator>().WaitingForApproval());

        var buttons = new ComponentBuilder()
            .WithButton("Delete Warn", $"unwarn-delete:{user.Id}")
            .WithButton(Translator.Get<PunishmentTranslator>().Cancel(), "unwarn-cancel", ButtonStyle.Danger);

        await RespondInteraction(string.Empty, embed, buttons);
    }

    [ComponentInteraction("unwarn-delete:*")]
    public async Task DeleteWarnConfirmation(string userId)
    {
        ModCaseRepository.AsUser(Identity);

        var modCases = (await ModCaseRepository.GetCasesForGuildAndUser(Context.Guild.Id, Convert.ToUInt64(userId)))
            .Where(x => x.PunishmentActive && x.PunishmentType == PunishmentType.Warn).ToList();

        var fMod = modCases.First();

        await ModCaseRepository.DeleteModCase(fMod.GuildId, fMod.CaseId);

        if (Context.Interaction is SocketMessageComponent castInteraction)
        {
            var embed = castInteraction.Message.Embeds.FirstOrDefault().ToEmbedBuilder()
                .WithColor(new Color(Convert.ToUInt32(int.Parse("7289da", NumberStyles.HexNumber))));

            embed.Fields =
            [
                new EmbedFieldBuilder().WithName(Translator.Get<PunishmentTranslator>().Result())
                    .WithValue($"Warning `{fMod.Title}` for {fMod.Username} deleted.")
            ];

            await RespondInteraction(string.Empty, embed);
        }
    }

    [ComponentInteraction("unwarn-cancel")]
    public async Task UnwarnCancel()
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
